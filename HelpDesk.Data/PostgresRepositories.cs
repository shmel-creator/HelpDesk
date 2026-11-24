using HelpDesk.Core;
using HelpDesk.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace HelpDesk.Data;

// Репозиторий заявок на базе Entity Framework Core
public class EfTicketRepository : ITicketRepository
{
    private readonly string connectionString;

    public EfTicketRepository(string connectionString)
    {
        this.connectionString = connectionString;
    }

    // Создаёт экземпляр контекста БД
    private HelpDeskDbContext CreateContext()
    {
        var optionsBuilder = new DbContextOptionsBuilder<HelpDeskDbContext>();
        optionsBuilder.UseNpgsql(this.connectionString);
        return new HelpDeskDbContext(optionsBuilder.Options);
    }

    // Загружает все заявки из БД с сортировкой по убыванию ID
    public List<Core.Ticket> GetAll()
    {
        using var db = CreateContext();
        var entities = db.Tickets
            .OrderByDescending(t => t.Id)
            .ToList();

        var result = new List<Core.Ticket>();
        foreach (var e in entities)
        {
            result.Add(new Core.Ticket
            {
                Id = e.Id,
                AuthorId = e.AuthorId,
                EquipmentId = e.EquipmentId,
                AssigneeId = e.AssigneeId,
                Title = e.Title,
                Description = e.Description,
                Priority = Enum.Parse<Priority>(e.Priority),
                Status = Enum.Parse<Status>(e.Status),
                CreatedAt = e.CreatedAt
            });
        }
        return result;
    }

    // Находит заявку по ID вместе с историей смены статусов
    public Core.Ticket GetById(int id)
    {
        using var db = CreateContext();
        var entity = db.Tickets.FirstOrDefault(t => t.Id == id);

        if (entity == null)
        {
            return null;
        }

        var ticket = new Core.Ticket
        {
            Id = entity.Id,
            AuthorId = entity.AuthorId,
            EquipmentId = entity.EquipmentId,
            AssigneeId = entity.AssigneeId,
            Title = entity.Title,
            Description = entity.Description,
            Priority = Enum.Parse<Priority>(entity.Priority),
            Status = Enum.Parse<Status>(entity.Status),
            CreatedAt = entity.CreatedAt
        };

        var orderedHistory = entity.TicketStatusHistories.OrderBy(h => h.ChangedAt).ToList();
        foreach (var h in orderedHistory)
        {
            ticket.StatusHistory.Add(new StatusHistoryItem
            {
                Id = h.Id,
                TicketId = h.TicketId,
                FromStatus = Enum.Parse<Status>(h.FromStatus),
                ToStatus = Enum.Parse<Status>(h.ToStatus),
                ChangedAt = h.ChangedAt,
                ChangedById = h.ChangedById
            });
        }

        return ticket;
    }

    // Создаёт новую заявку через EF Core и возвращает её ID
    public int Create(Core.Ticket ticket)
    {
        using var db = CreateContext();
        var entity = new Entities.Ticket
        {
            AuthorId = ticket.AuthorId,
            EquipmentId = ticket.EquipmentId,
            AssigneeId = ticket.AssigneeId,
            Title = ticket.Title,
            Description = ticket.Description,
            Priority = ticket.Priority.ToString(),
            Status = ticket.Status.ToString(),
            CreatedAt = DateTime.SpecifyKind(ticket.CreatedAt, DateTimeKind.Unspecified)

        };

        db.Tickets.Add(entity);
        db.SaveChanges();
        return entity.Id;
    }

    // Обновляет статус и добавляет запись в историю (SaveChanges выполняет оба действия в одной транзакции)
    public void UpdateStatus(int ticketId, Status fromStatus, Status toStatus, int changedById, DateTime changedAt)
    {
        using var db = CreateContext();

        var entity = db.Tickets.First(t => t.Id == ticketId);
        entity.Status = toStatus.ToString();

        var historyRecord = new TicketStatusHistory
        {
            TicketId = ticketId,
            FromStatus = fromStatus.ToString(),
            ToStatus = toStatus.ToString(),
            ChangedAt = DateTime.SpecifyKind(changedAt, DateTimeKind.Unspecified),
            ChangedById = changedById
        };

        db.TicketStatusHistories.Add(historyRecord);
        db.SaveChanges();
    }

    // Назначает исполнителя на заявку
    public void Assign(int ticketId, Nullable<int> assigneeId)
    {
        using var db = CreateContext();
        var entity = db.Tickets.First(t => t.Id == ticketId);
        entity.AssigneeId = assigneeId;
        db.SaveChanges();
    }
}

// Репозиторий сотрудников на базе EF Core
public class EfEmployeeRepository : IEmployeeRepository
{
    private readonly string connectionString;

    public EfEmployeeRepository(string connectionString)
    {
        this.connectionString = connectionString;
    }

    private HelpDeskDbContext CreateContext()
    {
        var optionsBuilder = new DbContextOptionsBuilder<HelpDeskDbContext>();
        optionsBuilder.UseNpgsql(this.connectionString);
        return new HelpDeskDbContext(optionsBuilder.Options);
    }

    // Загружает активных сотрудников (is_deleted == false)
    public List<Core.Employee> GetActive()
    {
        using var db = CreateContext();
        var entities = db.Employees
            .Where(e => !e.IsDeleted)
            .OrderBy(e => e.FullName)
            .ToList();

        var result = new List<Core.Employee>();
        foreach (var e in entities)
        {
            result.Add(new Core.Employee
            {
                Id = e.Id,
                FullName = e.FullName,
                Position = e.Position,
                Department = e.Department,
                IsDeleted = e.IsDeleted
            });
        }
        return result;
    }

    // Находит сотрудника по ID
    public Core.Employee GetById(int id)
    {
        using var db = CreateContext();
        var entity = db.Employees
            .FirstOrDefault(e => e.Id == id);

        if (entity == null)
        {
            return null;
        }

        return new Core.Employee
        {
            Id = entity.Id,
            FullName = entity.FullName,
            Position = entity.Position,
            Department = entity.Department,
            IsDeleted = entity.IsDeleted
        };
    }


    // Мягко удаляет сотрудника (устанавливает IsDeleted = true)
    public void SoftDelete(int id)
    {
        using var db = CreateContext();
        var e = db.Employees.First(emp => emp.Id == id);
        e.IsDeleted = true;
        db.SaveChanges();
    }
}

// Репозиторий оборудования на базе EF Core
public class EfEquipmentRepository : IEquipmentRepository
{
    private readonly string connectionString;

    public EfEquipmentRepository(string connectionString)
    {
        this.connectionString = connectionString;
    }

    private HelpDeskDbContext CreateContext()
    {
        var optionsBuilder = new DbContextOptionsBuilder<HelpDeskDbContext>();
        optionsBuilder.UseNpgsql(this.connectionString);
        return new HelpDeskDbContext(optionsBuilder.Options);
    }

    // Загружает активное оборудование (is_deleted == false)
    public List<Core.Equipment> GetActive()
    {
        using var db = CreateContext();
        var entities = db.Equipment
            .Where(eq => !eq.IsDeleted)
            .OrderBy(eq => eq.InventoryNumber)
            .ToList();

        var result = new List<Core.Equipment>();
        foreach (var eq in entities)
        {
            result.Add(new Core.Equipment
            {
                Id = eq.Id,
                InventoryNumber = eq.InventoryNumber,
                Name = eq.Name,
                Department = eq.Department,
                IsDeleted = eq.IsDeleted
            });
        }
        return result;
    }
}