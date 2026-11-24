using System;
using System.Collections.Generic;

namespace HelpDesk.Data.Entities;

public partial class Equipment
{
    public int Id { get; set; }

    public string InventoryNumber { get; set; } = null!;

    public string Name { get; set; } = null!;

    public string Department { get; set; } = null!;

    public bool IsDeleted { get; set; }

    public virtual ICollection<Ticket> Tickets { get; set; } = new List<Ticket>();
}
