DROP TABLE IF EXISTS comments, ticket_status_history, tickets, employees, equipment CASCADE;

CREATE TABLE equipment (
    id SERIAL PRIMARY KEY,
    inventory_number CHARACTER VARYING(50) UNIQUE NOT NULL,
    name CHARACTER VARYING(200) NOT NULL,
    department CHARACTER VARYING(100) NOT NULL,
    is_deleted BOOLEAN NOT NULL DEFAULT FALSE
);

CREATE TABLE employees (
    id SERIAL PRIMARY KEY,
    full_name CHARACTER VARYING(200) NOT NULL,
    position CHARACTER VARYING(100) NOT NULL,
    department CHARACTER VARYING(100) NOT NULL,
    is_deleted BOOLEAN NOT NULL DEFAULT FALSE
);

CREATE TABLE tickets (
    id SERIAL PRIMARY KEY,
    author_id INTEGER NOT NULL REFERENCES employees(id),
    equipment_id INTEGER NOT NULL REFERENCES equipment(id),
    assignee_id INTEGER NULL REFERENCES employees(id),
    title CHARACTER VARYING(200) NOT NULL CHECK (trim(title) <> ''),
    description CHARACTER VARYING(2000) NOT NULL DEFAULT '',
    priority CHARACTER VARYING(20) NOT NULL CHECK (priority IN ('Low', 'Normal', 'High', 'Critical')),
    status CHARACTER VARYING(20) NOT NULL CHECK (status IN ('New', 'InProgress', 'Resolved', 'Closed', 'Cancelled')),
    created_at TIMESTAMP WITHOUT TIME ZONE NOT NULL DEFAULT (NOW() AT TIME ZONE 'UTC')
);

CREATE TABLE ticket_status_history (
    id SERIAL PRIMARY KEY,
    ticket_id INTEGER NOT NULL REFERENCES tickets(id) ON DELETE CASCADE,
    from_status CHARACTER VARYING(20) NOT NULL,
    to_status CHARACTER VARYING(20) NOT NULL,
    changed_at TIMESTAMP WITHOUT TIME ZONE NOT NULL DEFAULT (NOW() AT TIME ZONE 'UTC'),
    changed_by_id INTEGER NOT NULL REFERENCES employees(id)
);

CREATE TABLE comments (
    id SERIAL PRIMARY KEY,
    ticket_id INTEGER NOT NULL REFERENCES tickets(id) ON DELETE CASCADE,
    author_id INTEGER NOT NULL REFERENCES employees(id),
    text CHARACTER VARYING(1000) NOT NULL CHECK (trim(text) <> ''),
    created_at TIMESTAMP WITHOUT TIME ZONE NOT NULL DEFAULT (NOW() AT TIME ZONE 'UTC')
);

CREATE INDEX idx_tickets_status ON tickets(status);
CREATE INDEX idx_tickets_priority ON tickets(priority);
CREATE INDEX idx_tickets_created_at ON tickets(created_at);
CREATE INDEX idx_tickets_assignee_id ON tickets(assignee_id);