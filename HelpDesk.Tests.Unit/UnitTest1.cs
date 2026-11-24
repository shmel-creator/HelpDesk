using FluentAssertions;
using HelpDesk.Core;
using System.Net.Sockets;
using Xunit;

namespace HelpDesk.Tests.Unit;

// Fake-репозиторий заявок в памяти (без подключения к БД)
public class FakeTicketRepository : ITicketRepository
{
    public List<Ticket> Storage { get; } = new();

    public List<Ticket> GetAll()
    {
        return this.Storage.ToList();
    }

    public Ticket GetById(int id)
    {
        return this.Storage.FirstOrDefault(t => t.Id == id);
    }

    public int Create(Ticket ticket)
    {
        ticket.Id = this.Storage.Count + 1;
        this.Storage.Add(ticket);
        return ticket.Id;
    }

    public void UpdateStatus(int ticketId, Status fromStatus, Status toStatus, int changedById, DateTime changedAt)
    {
        var ticket = this.Storage.First(t => t.Id == ticketId);
        ticket.Status = toStatus;
    }

    public void Assign(int ticketId, Nullable<int> assigneeId)
    {
        var ticket = this.Storage.First(t => t.Id == ticketId);
        ticket.AssigneeId = assigneeId;
    }
}

// Fake-репозиторий сотрудников в памяти
public class FakeEmployeeRepository : IEmployeeRepository
{
    public List<Employee> Storage { get; } = new();

    public List<Employee> GetActive()
    {
        return this.Storage.Where(e => !e.IsDeleted).ToList();
    }

    public Employee GetById(int id)
    {
        return this.Storage.FirstOrDefault(e => e.Id == id);
    }

    public void SoftDelete(int id)
    {
        var employee = this.Storage.First(e => e.Id == id);
        employee.IsDeleted = true;
    }
}

// Модульные тесты расчёта дедлайнов SLA
public class SlaCalculatorTests
{
    [Theory]
    [InlineData(Priority.Low, 72)]
    [InlineData(Priority.Normal, 24)]
    [InlineData(Priority.High, 8)]
    [InlineData(Priority.Critical, 4)]
    public async Task CalculateSlaDeadline_KnownPriority_ReturnsExpectedDeadline(Priority priority, int expectedHours)
    {
        await Task.CompletedTask;
        // Arrange
        var createdAt = new DateTime(2026, 10, 20, 10, 0, 0, DateTimeKind.Utc);
        var sut = new SlaCalculator();

        // Act
        var deadline = sut.CalculateSlaDeadline(createdAt, priority);

        // Assert
        deadline.Should().Be(createdAt.AddHours(expectedHours));
    }

    [Fact]
    public async Task CalculateSlaDeadline_UnknownPriority_ThrowsArgumentOutOfRangeException()
    {
        await Task.CompletedTask;
        // Arrange
        var sut = new SlaCalculator();

        // Act
        var exception = Record.Exception(delegate
        {
            sut.CalculateSlaDeadline(DateTime.UnixEpoch, (Priority)999);
        });

        // Assert
        exception.Should().BeOfType<ArgumentOutOfRangeException>();
    }
}

// Модульные тесты матрицы переходов статусов
public class StatusTransitionValidatorTests
{
    [Theory]
    [InlineData(Status.New, Status.InProgress, true)]
    [InlineData(Status.InProgress, Status.Resolved, true)]
    [InlineData(Status.Resolved, Status.Closed, true)]
    [InlineData(Status.Closed, Status.New, false)]
    public async Task IsTransitionAllowed_StatusPair_ReturnsExpectedResult(Status from, Status to, bool expected)
    {
        await Task.CompletedTask;
        // Arrange
        var sut = new StatusTransitionValidator();

        // Act
        var result = sut.IsTransitionAllowed(from, to);

        // Assert
        result.Should().Be(expected);
    }
}

// Модульные тесты бизнес-правил TicketService
public class TicketServiceTests
{
    [Fact]
    public async Task CreateTicket_EmptyTitle_ThrowsDomainException()
    {
        // Arrange
        var ticketRepo = new FakeTicketRepository();
        var employeeRepo = new FakeEmployeeRepository();
        var sut = new TicketService(ticketRepo, employeeRepo, new StatusTransitionValidator(), new SlaCalculator(), new SystemClock());
        var ticket = new Ticket { Title = "   ", Description = "Описание" };

        // Act
        void Act()
        {
            sut.CreateTicket(ticket);
        }

        // Assert
        FluentActions.Invoking(Act)
            .Should().Throw<DomainException>()
            .WithMessage("*Title*");
    }

    [Fact]
    public async Task CreateTicket_DescriptionExceeds2000Chars_ThrowsDomainException()
    {
        // Arrange
        var ticketRepo = new FakeTicketRepository();
        var employeeRepo = new FakeEmployeeRepository();
        var sut = new TicketService(ticketRepo, employeeRepo, new StatusTransitionValidator(), new SlaCalculator(), new SystemClock());
        var ticket = new Ticket { Title = "Тест", Description = new string('x', 2001) };

        // Act
        void Act()
        {
            sut.CreateTicket(ticket);
        }

        // Assert
        FluentActions.Invoking(Act)
            .Should().Throw<DomainException>()
            .WithMessage("*2000*");
    }

    [Fact]
    public async Task ChangeStatus_ClosedToNew_ThrowsDomainException()
    {
        // Arrange
        var ticketRepo = new FakeTicketRepository();
        ticketRepo.Storage.Add(new Ticket { Id = 1, Status = Status.Closed });
        var employeeRepo = new FakeEmployeeRepository();
        var sut = new TicketService(ticketRepo, employeeRepo, new StatusTransitionValidator(), new SlaCalculator(), new SystemClock());

        // Act
        void Act()
        {
            sut.ChangeStatus(1, Status.New, 1);
        }

        // Assert
        FluentActions.Invoking(Act)
            .Should().Throw<DomainException>()
            .WithMessage("*Недопустимый переход*");
    }

    [Fact]
    public async Task SoftDeleteEmployee_HasOpenTickets_ThrowsDomainException()
    {
        await Task.CompletedTask;
        // Arrange
        var ticketRepo = new FakeTicketRepository();
        ticketRepo.Storage.Add(new Ticket { Id = 10, AssigneeId = 1, Status = Status.InProgress });
        var empRepo = new FakeEmployeeRepository();
        empRepo.Storage.Add(new Employee { Id = 1, FullName = "Иванов И.И." });
        var sut = new TicketService(
            ticketRepo,
            empRepo,
            new StatusTransitionValidator(),
            new SlaCalculator());

        // Act
        var exception = Record.Exception(delegate
        {
            sut.SoftDeleteEmployee(1);
        });

        // Assert
        exception.Should().BeOfType<DomainException>();
    }
}