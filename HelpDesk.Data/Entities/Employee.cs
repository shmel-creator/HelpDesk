using System;
using System.Collections.Generic;

namespace HelpDesk.Data.Entities;

public partial class Employee
{
    public int Id { get; set; }

    public string FullName { get; set; } = null!;

    public string Position { get; set; } = null!;

    public string Department { get; set; } = null!;

    public bool IsDeleted { get; set; }

    public virtual ICollection<Comment> Comments { get; set; } = new List<Comment>();

    public virtual ICollection<Ticket> TicketAssignees { get; set; } = new List<Ticket>();

    public virtual ICollection<Ticket> TicketAuthors { get; set; } = new List<Ticket>();

    public virtual ICollection<TicketStatusHistory> TicketStatusHistories { get; set; } = new List<TicketStatusHistory>();
}
