using System;
using System.Collections.Generic;

namespace HelpDesk.Data.Entities;

public partial class Ticket
{
    public int Id { get; set; }

    public int AuthorId { get; set; }

    public int EquipmentId { get; set; }

    public int? AssigneeId { get; set; }

    public string Title { get; set; } = null!;

    public string Description { get; set; } = null!;

    public string Priority { get; set; } = null!;

    public string Status { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public virtual Employee? Assignee { get; set; }

    public virtual Employee Author { get; set; } = null!;

    public virtual ICollection<Comment> Comments { get; set; } = new List<Comment>();

    public virtual Equipment Equipment { get; set; } = null!;

    public virtual ICollection<TicketStatusHistory> TicketStatusHistories { get; set; } = new List<TicketStatusHistory>();
}
