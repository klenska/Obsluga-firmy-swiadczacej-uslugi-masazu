-- GŁÓWNE TABELE (Zasoby)

CREATE TABLE services (
    id SERIAL PRIMARY KEY,
    name VARCHAR(100) NOT NULL,
    duration_minutes INT NOT NULL,
    price_gr INT NOT NULL,
    icon VARCHAR(20),
    description TEXT
);

CREATE TABLE rooms (
    id SERIAL PRIMARY KEY,
    name VARCHAR(100) NOT NULL,
    description VARCHAR(255)
);

CREATE TABLE employees (
    id SERIAL PRIMARY KEY,
    name VARCHAR(100) NOT NULL,
    specialty VARCHAR(255),
    shift_start TIME,
    shift_end TIME,
    break_start TIME,
    break_end TIME
);

-- TABELE ASOCJACYJNE (Relacje i harmonogramy)

CREATE TABLE service_employees (
    service_id INT REFERENCES services(id),
    employee_id INT REFERENCES employees(id),
    PRIMARY KEY (service_id, employee_id)
);

CREATE TABLE service_rooms (
    service_id INT REFERENCES services(id),
    room_id INT REFERENCES rooms(id),
    PRIMARY KEY (service_id, room_id)
);

CREATE TABLE rooms_closures (
    room_id INT REFERENCES rooms(id),
    closed_date DATE NOT NULL,
    PRIMARY KEY (room_id, closed_date)
);

CREATE TABLE employee_work_days (
    employee_id INT REFERENCES employees(id),
    day_of_week INT NOT NULL,
    PRIMARY KEY (employee_id, day_of_week)
);

CREATE TABLE employee_off_days (
    employee_id INT REFERENCES employees(id),
    off_date DATE NOT NULL,
    PRIMARY KEY (employee_id, off_date)
);

-- DANE TESTOWE (Seeding)

-- Dodanie 3 usług
INSERT INTO services (name, duration_minutes, price_gr, icon, description) VALUES
('Masaż relaksacyjny', 60, 18000, '🌿', 'Klasyczny masaż zmniejszający napięcie mięśniowe.'),
('Masaż gorącymi kamieniami', 90, 26000, '🪨', 'Głęboko relaksujący zabieg z użyciem rozgrzanych bazaltów.'),
('Masaż tajski', 120, 30000, '🎋', 'Intensywny masaż łączący akupresurę z elementami jogi pasywnej.');

-- Dodanie 3 gabinetów
INSERT INTO rooms (name, description) VALUES
('Gabinet Zen', 'Jasny pokój z muzykoterapią'),
('Gabinet Lotos', 'Przyciemniany pokój, idealny do masaży kamieniami'),
('Gabinet Bambus', 'Duża mata na podłodze, drabinki pod sufitem');

-- Dodanie 2 masażystów z ich stałymi godzinami pracy
INSERT INTO employees (name, specialty, shift_start, shift_end, break_start, break_end) VALUES
('Anna Kowalska', 'Terapeutka SPA', '09:00:00', '17:00:00', '13:00:00', '13:30:00'),
('Jan Nowak', 'Fizjoterapeuta', '12:00:00', '20:00:00', '16:00:00', '16:30:00');

-- Relacje Usługa-Pracownik
INSERT INTO service_employees (service_id, employee_id) VALUES
(1, 1), (2, 1), -- Anna robi masaż relaksacyjny (1) i kamieniami (2)
(1, 2), (3, 2); -- Jan robi masaż relaksacyjny (1) i tajski (3)

-- Relacje Usługa-Gabinet
INSERT INTO service_rooms (service_id, room_id) VALUES
(1, 1), (1, 2), -- Relaksacyjny robiony jest w Zen i Lotosie
(2, 2),         -- Masaż kamieniamy robiony jest tylko w Lotosie
(3, 3);         -- Tajski robiony jest tylko w gabinecie Bambus

-- Ustalenie dni roboczych
INSERT INTO employee_work_days (employee_id, day_of_week) VALUES
(1, 1), (1, 2), (1, 3), (1, 4), (1, 5), -- Anna pracuje od poniedziałku do piątku
(2, 2), (2, 3), (2, 4), (2, 5), (2, 6); -- Jan pracuje od wtorku do soboty