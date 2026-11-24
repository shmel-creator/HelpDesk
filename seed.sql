INSERT INTO employees (full_name, position, department) VALUES
('Иванов Иван Иванович', 'Инженер', 'ИТ-отдел'),
('Петров Пётр Петрович', 'Системный администратор', 'ИТ-отдел'),
('Сидорова Анна Сергеевна', 'Бухгалтер', 'Бухгалтерия');

INSERT INTO equipment (inventory_number, name, department) VALUES
('INV-001', 'Ноутбук Lenovo ThinkPad', 'Бухгалтерия'),
('INV-002', 'Принтер HP LaserJet', 'ИТ-отдел');

INSERT INTO tickets (author_id, equipment_id, assignee_id, title, description, priority, status, created_at) VALUES
(3, 1, NULL, 'Не включается ноутбук', 'При нажатии кнопки питания индикатор не горит.', 'Critical', 'New', '2026-10-20 08:00:00'),
(3, 2, 1, 'Замятие бумаги', 'Принтер зажёвывает листы из нижнего лотка.', 'High', 'InProgress', '2026-10-20 07:00:00'),
(1, 2, 2, 'Замена картриджа', 'Закончился тонер.', 'Normal', 'Resolved', '2026-10-19 10:00:00'),
(1, 1, 2, 'Установка ПО', 'Установлен пакет офисных программ.', 'Low', 'Closed', '2026-10-18 09:00:00'),
(3, 1, NULL, 'Ошибочная заявка', 'Дубль предыдущего обращения.', 'Low', 'Cancelled', '2026-10-18 11:00:00');