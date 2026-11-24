using System;

namespace HelpDesk.Core;

// Приоритеты и статусы жизненного цикла заявки
public enum Priority { Low, Normal, High, Critical }
public enum Status { New, InProgress, Resolved, Closed, Cancelled }

// Исключение при нарушении бизнес-правил
public class DomainException : Exception
{
    public DomainException(string message) : base(message) { }
}

// Исключение, если сущность не найдена в БД
public class NotFoundException : Exception
{
    public NotFoundException(string message) : base(message) { }
}

// Модель единицы оборудования
public class Equipment
{
    public int Id { get; set; }
    public string InventoryNumber { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Department { get; set; } = string.Empty;
    public bool IsDeleted { get; set; }
}

// Модель сотрудника
public class Employee
{
    public int Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Position { get; set; } = string.Empty;
    public string Department { get; set; } = string.Empty;
    public bool IsDeleted { get; set; }
}

// Модель заявки на ремонт
public class Ticket
{
    public int Id { get; set; }
    public int AuthorId { get; set; }
    public int EquipmentId { get; set; }
    public Nullable<int> AssigneeId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public Priority Priority { get; set; }
    public Status Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime SlaDeadline { get; set; }
    public bool IsOverdue { get; set; }
    public List<StatusHistoryItem> StatusHistory { get; set; } = new();
}

// Запись в истории смены статусов
public class StatusHistoryItem
{
    public int Id { get; set; }
    public int TicketId { get; set; }
    public Status FromStatus { get; set; }
    public Status ToStatus { get; set; }
    public DateTime ChangedAt { get; set; }
    public int ChangedById { get; set; }
}

// Абстракция текущего времени для детерминированных тестов
public interface IClock
{
    DateTime UtcNow { get; }
}

public class SystemClock : IClock
{
    public DateTime UtcNow
    {
        get { return DateTime.UtcNow; }
    }
}

// Интерфейсы репозиториев для доступа к данным
public interface ITicketRepository
{
    List<Ticket> GetAll();
    Ticket GetById(int id);
    int Create(Ticket ticket);
    void UpdateStatus(int ticketId, Status fromStatus, Status toStatus, int changedById, DateTime changedAt);
    void Assign(int ticketId, Nullable<int> assigneeId);
}

public interface IEmployeeRepository
{
    List<Employee> GetActive();
    Employee GetById(int id);
    void SoftDelete(int id);
}

public interface IEquipmentRepository
{
    List<Equipment> GetActive();
}

// Расчёт дедлайна SLA в часах в зависимости от приоритета (исправлено BUG-01)
public class SlaCalculator
{
    public DateTime CalculateSlaDeadline(DateTime createdAt, Priority priority)
    {
        switch (priority)
        {
            case Priority.Critical:
                return createdAt.AddHours(4);
            case Priority.High:
                return createdAt.AddHours(8);
            case Priority.Normal:
                return createdAt.AddHours(24);
            case Priority.Low:
                return createdAt.AddHours(72);
            default:
                throw new ArgumentOutOfRangeException(nameof(priority), "Неизвестный приоритет");
        }
    }
}

// Проверка допустимости перехода между статусами
public class StatusTransitionValidator
{
    public bool IsTransitionAllowed(Status from, Status to)
    {
        switch ((from, to))
        {
            case (Status.New, Status.InProgress):
            case (Status.New, Status.Cancelled):
            case (Status.InProgress, Status.Resolved):
            case (Status.InProgress, Status.Cancelled):
            case (Status.Resolved, Status.Closed):
                return true;
            default:
                return false;
        }
    }
}

// Основной сервис бизнес-логики заявок
public class TicketService
{
    private readonly ITicketRepository tickets;
    private readonly IEmployeeRepository employees;
    private readonly StatusTransitionValidator validator;
    private readonly SlaCalculator sla;
    private readonly IClock clock;

    public TicketService(
        ITicketRepository tickets,
        IEmployeeRepository employees,
        StatusTransitionValidator validator,
        SlaCalculator sla,
        IClock clock = null)
    {
        this.tickets = tickets;
        this.employees = employees;
        this.validator = validator;
        this.sla = sla;
        this.clock = clock == null ? new SystemClock() : clock;
    }

    // Получает все заявки и вычисляет для каждой дедлайн SLA и просрочку
    public List<Ticket> GetTicketsWithSla()
    {
        var list = this.tickets.GetAll();
        var now = this.clock.UtcNow;

        foreach (var item in list)
        {
            item.SlaDeadline = this.sla.CalculateSlaDeadline(item.CreatedAt, item.Priority);
            bool isClosedOrDone = item.Status is Status.Resolved or Status.Closed or Status.Cancelled;
            item.IsOverdue = !isClosedOrDone && now > item.SlaDeadline;
        }

        return list;
    }

    // Валидирует поля и создаёт новую заявку со статусом New
    public int CreateTicket(Ticket ticket)
    {
        if (string.IsNullOrWhiteSpace(ticket.Title))
        {
            throw new DomainException("Поле Title (заголовок) не может быть пустым.");
        }

        if (ticket.Description != null && ticket.Description.Length > 2000)
        {
            throw new DomainException("Поле Description (описание) не должно превышать 2000 символов.");
        }

        ticket.Title = ticket.Title.Trim();
        ticket.Description = ticket.Description == null ? string.Empty : ticket.Description;
        ticket.Status = Status.New;
        ticket.CreatedAt = this.clock.UtcNow;

        return this.tickets.Create(ticket);
    }

    // Меняет статус заявки после проверки матрицы переходов
    public void ChangeStatus(int ticketId, Status newStatus, int changedById)
    {
        var ticket = this.tickets.GetById(ticketId);
        if (ticket == null)
        {
            throw new NotFoundException($"Заявка с ID {ticketId} не найдена.");
        }

        if (!this.validator.IsTransitionAllowed(ticket.Status, newStatus))
        {
            throw new DomainException($"Недопустимый переход из {ticket.Status} в {newStatus}.");
        }

        this.tickets.UpdateStatus(ticketId, ticket.Status, newStatus, changedById, this.clock.UtcNow);
    }

    // Мягко удаляет сотрудника, если на нём нет незакрытых заявок
    public void SoftDeleteEmployee(int employeeId)
    {
        var employee = this.employees.GetById(employeeId);
        if (employee == null)
        {
            throw new NotFoundException($"Сотрудник с ID {employeeId} не найден.");
        }

        var allTickets = this.tickets.GetAll();
        bool hasOpenTickets = allTickets.Any(t =>
            t.AssigneeId == employeeId &&
            t.Status is Status.New or Status.InProgress or Status.Resolved);

        if (hasOpenTickets)
        {
            throw new DomainException("Нельзя удалить сотрудника: у него есть незакрытые заявки.");
        }

        this.employees.SoftDelete(employeeId);
    }
}