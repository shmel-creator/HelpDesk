using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace HelpDesk.Data.Entities;

public partial class HelpDeskDbContext : DbContext
{
    public HelpDeskDbContext()
    {
    }

    public HelpDeskDbContext(DbContextOptions<HelpDeskDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Comment> Comments { get; set; }

    public virtual DbSet<Employee> Employees { get; set; }

    public virtual DbSet<Equipment> Equipment { get; set; }

    public virtual DbSet<Ticket> Tickets { get; set; }

    public virtual DbSet<TicketStatusHistory> TicketStatusHistories { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Comment>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("comments_pkey");

            entity.ToTable("comments");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.AuthorId).HasColumnName("author_id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(now() AT TIME ZONE 'UTC'::text)")
                .HasColumnType("timestamp without time zone")
                .HasColumnName("created_at");
            entity.Property(e => e.Text)
                .HasMaxLength(1000)
                .HasColumnName("text");
            entity.Property(e => e.TicketId).HasColumnName("ticket_id");

            entity.HasOne(d => d.Author).WithMany(p => p.Comments)
                .HasForeignKey(d => d.AuthorId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("comments_author_id_fkey");

            entity.HasOne(d => d.Ticket).WithMany(p => p.Comments)
                .HasForeignKey(d => d.TicketId)
                .HasConstraintName("comments_ticket_id_fkey");
        });

        modelBuilder.Entity<Employee>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("employees_pkey");

            entity.ToTable("employees");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Department)
                .HasMaxLength(100)
                .HasColumnName("department");
            entity.Property(e => e.FullName)
                .HasMaxLength(200)
                .HasColumnName("full_name");
            entity.Property(e => e.IsDeleted)
                .HasDefaultValue(false)
                .HasColumnName("is_deleted");
            entity.Property(e => e.Position)
                .HasMaxLength(100)
                .HasColumnName("position");
        });

        modelBuilder.Entity<Equipment>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("equipment_pkey");

            entity.ToTable("equipment");

            entity.HasIndex(e => e.InventoryNumber, "equipment_inventory_number_key").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Department)
                .HasMaxLength(100)
                .HasColumnName("department");
            entity.Property(e => e.InventoryNumber)
                .HasMaxLength(50)
                .HasColumnName("inventory_number");
            entity.Property(e => e.IsDeleted)
                .HasDefaultValue(false)
                .HasColumnName("is_deleted");
            entity.Property(e => e.Name)
                .HasMaxLength(200)
                .HasColumnName("name");
        });

        modelBuilder.Entity<Ticket>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("tickets_pkey");

            entity.ToTable("tickets");

            entity.HasIndex(e => e.AssigneeId, "idx_tickets_assignee_id");

            entity.HasIndex(e => e.CreatedAt, "idx_tickets_created_at");

            entity.HasIndex(e => e.Priority, "idx_tickets_priority");

            entity.HasIndex(e => e.Status, "idx_tickets_status");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.AssigneeId).HasColumnName("assignee_id");
            entity.Property(e => e.AuthorId).HasColumnName("author_id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(now() AT TIME ZONE 'UTC'::text)")
                .HasColumnType("timestamp without time zone")
                .HasColumnName("created_at");
            entity.Property(e => e.Description)
                .HasMaxLength(2000)
                .HasDefaultValueSql("''::character varying")
                .HasColumnName("description");
            entity.Property(e => e.EquipmentId).HasColumnName("equipment_id");
            entity.Property(e => e.Priority)
                .HasMaxLength(20)
                .HasColumnName("priority");
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .HasColumnName("status");
            entity.Property(e => e.Title)
                .HasMaxLength(200)
                .HasColumnName("title");

            entity.HasOne(d => d.Assignee).WithMany(p => p.TicketAssignees)
                .HasForeignKey(d => d.AssigneeId)
                .HasConstraintName("tickets_assignee_id_fkey");

            entity.HasOne(d => d.Author).WithMany(p => p.TicketAuthors)
                .HasForeignKey(d => d.AuthorId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("tickets_author_id_fkey");

            entity.HasOne(d => d.Equipment).WithMany(p => p.Tickets)
                .HasForeignKey(d => d.EquipmentId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("tickets_equipment_id_fkey");
        });

        modelBuilder.Entity<TicketStatusHistory>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("ticket_status_history_pkey");

            entity.ToTable("ticket_status_history");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.ChangedAt)
                .HasDefaultValueSql("(now() AT TIME ZONE 'UTC'::text)")
                .HasColumnType("timestamp without time zone")
                .HasColumnName("changed_at");
            entity.Property(e => e.ChangedById).HasColumnName("changed_by_id");
            entity.Property(e => e.FromStatus)
                .HasMaxLength(20)
                .HasColumnName("from_status");
            entity.Property(e => e.TicketId).HasColumnName("ticket_id");
            entity.Property(e => e.ToStatus)
                .HasMaxLength(20)
                .HasColumnName("to_status");

            entity.HasOne(d => d.ChangedBy).WithMany(p => p.TicketStatusHistories)
                .HasForeignKey(d => d.ChangedById)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("ticket_status_history_changed_by_id_fkey");

            entity.HasOne(d => d.Ticket).WithMany(p => p.TicketStatusHistories)
                .HasForeignKey(d => d.TicketId)
                .HasConstraintName("ticket_status_history_ticket_id_fkey");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
