using Microsoft.EntityFrameworkCore;
using MiniMes.Api.Models;

namespace MiniMes.Api.Data;

public class MiniMesDbContext : DbContext
{
    public MiniMesDbContext(DbContextOptions<MiniMesDbContext> options) : base(options)
    {
    }

    public DbSet<WorkOrder> WorkOrders => Set<WorkOrder>();
    public DbSet<Lot> Lots => Set<Lot>();
    public DbSet<ProcessResult> ProcessResults => Set<ProcessResult>();
    public DbSet<EquipmentLog> EquipmentLogs => Set<EquipmentLog>();
    public DbSet<Item> Items => Set<Item>();
    public DbSet<ProductionCycle> ProductionCycles => Set<ProductionCycle>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<WorkOrder>(e =>
        {
            e.HasIndex(w => w.OrderNo).IsUnique();
            e.Property(w => w.Status).HasConversion<string>().HasMaxLength(20);
        });

        modelBuilder.Entity<Lot>(e =>
        {
            e.HasIndex(l => l.LotNo).IsUnique();
            e.Property(l => l.Status).HasConversion<string>().HasMaxLength(20);
            e.HasOne(l => l.WorkOrder).WithMany(w => w.Lots).HasForeignKey(l => l.WorkOrderId);
        });

        modelBuilder.Entity<ProcessResult>(e =>
        {
            e.HasIndex(r => new { r.LotId, r.ProcessCode });
            e.HasOne(r => r.Lot).WithMany(l => l.Results).HasForeignKey(r => r.LotId);
        });

        modelBuilder.Entity<EquipmentLog>(e =>
        {
            e.HasIndex(l => new { l.EquipmentCode, l.CollectedAt });
            e.Property(l => l.Status).HasConversion<string>().HasMaxLength(20);
        });

        modelBuilder.Entity<Item>(e =>
        {
            e.HasIndex(i => i.ItemCode).IsUnique();
        });

        modelBuilder.Entity<ProductionCycle>(e =>
        {
            e.HasIndex(c => new { c.ItemCode, c.StartedAt });
            e.HasIndex(c => c.IsGood);
        });
    }
}
