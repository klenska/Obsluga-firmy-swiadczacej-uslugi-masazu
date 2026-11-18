using Npgsql;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

app.MapGet("/", () => "Serwer Masu Masu SPA działa.");

// testowy endpoint do pobierania informacji o ofercie

app.MapGet("/api/services", async () =>
{
    // nawiązanie połączenia z bazą danych
    await using var connection = new NpgsqlConnection(connectionString);
    await connection.OpenAsync();

    // zapytanie sql
    var sql = @"
        SELECT 
            s.id, 
            s.name, 
            s.duration_minutes, 
            s.price_gr, 
            s.icon, 
            s.description,
            COALESCE((SELECT array_agg(employee_id) FROM service_employees WHERE service_id = s.id), '{}') as therapists,
            COALESCE((SELECT array_agg(room_id) FROM service_rooms WHERE service_id = s.id), '{}') as rooms
        FROM services s
        ORDER BY s.id";
    await using var command = new NpgsqlCommand(sql, connection);
    await using var reader = await command.ExecuteReaderAsync();

    var servicesList = new List<object>();

    // czytanie wyników
    while (await reader.ReadAsync())
    {
        var priceGr = reader.GetInt32(3);
        servicesList.Add(new
        {
            id = reader.GetInt32(0),
            name = reader.GetString(1),
            duration = reader.GetInt32(2),
            price = priceGr / 100m, 
            icon = reader.IsDBNull(4) ? "🌿" : reader.GetString(4),
            description = reader.IsDBNull(5) ? "" : reader.GetString(5),
            therapists = reader.GetFieldValue<int[]>(6),
            rooms = reader.GetFieldValue<int[]>(7)
        });
    }

    return Results.Ok(servicesList);
});

//testowy endpoint do dodawania nowej usługi

app.MapPost("/api/services", async (ServiceRequest req) =>
{
    var validationError = ValidateService(req);
    if (validationError is not null)
        return Results.BadRequest(new { error = validationError });
    
    await using var connection = new NpgsqlConnection(connectionString);
    await connection.OpenAsync();

    await using var transaction = await connection.BeginTransactionAsync();

    try
    {
        // sql
        var insertServiceSql = @"
            INSERT INTO services (name, duration_minutes, price_gr, icon, description)
            VALUES (@name, @duration, @priceGr, @icon, @description)
            RETURNING id";

        int newServiceId;
        await using (var cmd = new NpgsqlCommand(insertServiceSql, connection, transaction))
        {
            // nazwa - obowiązkowe
            cmd.Parameters.AddWithValue("name", req.Name);
            // czas trwania usługi w minutach - obowiązkowe
            cmd.Parameters.AddWithValue("duration", req.Duration);
            // cena - obowiązkowe (zamiana złotówek na grosze)
            cmd.Parameters.AddWithValue("priceGr", (int)Math.Round(req.Price * 100));
            // ikona - opcjonalne
            cmd.Parameters.AddWithValue("icon", string.IsNullOrWhiteSpace(req.Icon) ? "🌿" : req.Icon);
            // opis bez limitu długości - opcjonalne
            cmd.Parameters.AddWithValue("description", req.Description ?? "");

            // zwrócenie id
            newServiceId = (int)(await cmd.ExecuteScalarAsync())!;
        }

        // zapis przypisanych masażystów
        foreach (var employeeId in req.Therapists.Distinct())
        {
            var sqlEmp = "INSERT INTO service_employees (service_id, employee_id) VALUES (@sId, @eId)";
            await using var cmd = new NpgsqlCommand(sqlEmp, connection, transaction);
            cmd.Parameters.AddWithValue("sId", newServiceId);
            cmd.Parameters.AddWithValue("eId", employeeId);
            await cmd.ExecuteNonQueryAsync();
        }

        // zapis przypisanych gabinetów
        foreach (var roomId in req.Rooms.Distinct())
        {
            var sqlRoom = "INSERT INTO service_rooms (service_id, room_id) VALUES (@sId, @rId)";
            await using var cmd = new NpgsqlCommand(sqlRoom, connection, transaction);
            cmd.Parameters.AddWithValue("sId", newServiceId);
            cmd.Parameters.AddWithValue("rId", roomId);
            await cmd.ExecuteNonQueryAsync();
        }

        // zatwierdzenie transakcji
        await transaction.CommitAsync();

        // zwrócenie statusu 201 Created i id nowej usługi
        return Results.Created($"/api/services/{newServiceId}", new { id = newServiceId });
    }
    catch (PostgresException ex) when (ex.SqlState == "23503")
    {
        await transaction.RollbackAsync();
        return Results.BadRequest(new { error = "Wybrany masażysta lub gabinet nie istnieje." });
    }
    catch (Exception ex)
    {
        await transaction.RollbackAsync();
        Console.WriteLine(ex);
        return Results.Problem("Wystąpił nieoczekiwany błąd serwera.");
    }
});

// testowy endpoint do edycji usługi

app.MapPut("/api/services/{id:int}", async (int id, ServiceRequest req) =>
{
    var validationError = ValidateService(req);
    if (validationError is not null)
        return Results.BadRequest(new { error = validationError });
    
    await using var connection = new NpgsqlConnection(connectionString);
    await connection.OpenAsync();

    await using var transaction = await connection.BeginTransactionAsync();

    try
    {
        // sql
        var updateSql = @"
            UPDATE services
            SET name = @name,
                duration_minutes = @duration,
                price_gr = @priceGr,
                icon = @icon,
                description = @description
            WHERE id = @id";

        await using (var cmd = new NpgsqlCommand(updateSql, connection, transaction))
        {
            cmd.Parameters.AddWithValue("id", id);
            cmd.Parameters.AddWithValue("name", req.Name);
            cmd.Parameters.AddWithValue("duration", req.Duration);
            cmd.Parameters.AddWithValue("priceGr", (int)Math.Round(req.Price * 100));
            cmd.Parameters.AddWithValue("icon", string.IsNullOrWhiteSpace(req.Icon) ? "🌿" : req.Icon);
            cmd.Parameters.AddWithValue("description", req.Description ?? "");

            // sprawdzenie, czy taka usługa w ogóle istniała w bazie
            var rowsAffected = await cmd.ExecuteNonQueryAsync();
            if (rowsAffected == 0)
            {
                await transaction.RollbackAsync();
                return Results.NotFound(new { error = $"Usługa o ID {id} nie istnieje." });
            }
        }

        // czyszczenie starych powiązań z pracownikami i gabinetami, żeby dodać nowe od zera
        await using (var cmd = new NpgsqlCommand("DELETE FROM service_employees WHERE service_id = @sId", connection, transaction))
        {
            cmd.Parameters.AddWithValue("sId", id);
            await cmd.ExecuteNonQueryAsync();
        }

        await using (var cmd = new NpgsqlCommand("DELETE FROM service_rooms WHERE service_id = @sId", connection, transaction))
        {
            cmd.Parameters.AddWithValue("sId", id);
            await cmd.ExecuteNonQueryAsync();
        }

        foreach (var employeeId in req.Therapists.Distinct())
        {
            var sqlEmp = "INSERT INTO service_employees (service_id, employee_id) VALUES (@sId, @eId)";
            await using var cmd = new NpgsqlCommand(sqlEmp, connection, transaction);
            cmd.Parameters.AddWithValue("sId", id);
            cmd.Parameters.AddWithValue("eId", employeeId);
            await cmd.ExecuteNonQueryAsync();
        }

        foreach (var roomId in req.Rooms.Distinct())
        {
            var sqlRoom = "INSERT INTO service_rooms (service_id, room_id) VALUES (@sId, @rId)";
            await using var cmd = new NpgsqlCommand(sqlRoom, connection, transaction);
            cmd.Parameters.AddWithValue("sId", id);
            cmd.Parameters.AddWithValue("rId", roomId);
            await cmd.ExecuteNonQueryAsync();
        }

        await transaction.CommitAsync();
        return Results.Ok(new { id });
    }
    catch (PostgresException ex) when (ex.SqlState == "23503")
    {
        await transaction.RollbackAsync();
        return Results.BadRequest(new { error = "Wybrany masażysta lub gabinet nie istnieje." });
    }
    catch (Exception ex)
    {
        await transaction.RollbackAsync();
        Console.WriteLine(ex);
        return Results.Problem("Wystąpił nieoczekiwany błąd serwera.");
    }
});


app.Run();

// pomocnicza funkcja walidująca (do POST i PUT)
static string? ValidateService(ServiceRequest req)
{
    if (string.IsNullOrWhiteSpace(req.Name))
        return "Nazwa usługi jest wymagana.";
    if (req.Name.Length > 100)
        return "Nazwa usługi może mieć maksymalnie 100 znaków.";
    if (req.Price < 0)
        return "Cena nie może być ujemna.";
    if (req.Duration <= 0)
        return "Czas trwania musi być większy od zera.";
    if (req.Therapists is null || req.Therapists.Length == 0)
        return "Wybierz przynajmniej jednego masażystę.";
    if (req.Rooms is null || req.Rooms.Length == 0)
        return "Wybierz przynajmniej jeden gabinet.";

    return null;
}

record ServiceRequest(
    string Name,
    int Duration,
    decimal Price,
    string? Icon,
    string? Description,
    int[] Therapists,
    int[] Rooms
);
