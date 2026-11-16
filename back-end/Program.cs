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
        FROM services s";
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

app.Run();
