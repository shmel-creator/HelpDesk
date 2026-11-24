using System;
using System.Collections.Generic;

namespace HelpDesk.Data.Entities;

public partial class Comment
{
    public int Id { get; set; }

    public int TicketId { get; set; }

    public int AuthorId { get; set; }

    public string Text { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public virtual Employee Author { get; set; } = null!;

    public virtual Ticket Ticket { get; set; } = null!;
}
