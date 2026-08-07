using makeBreak.Src.Core.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace makeBreak.Src.Infrastructure.Data;

/// <summary>
/// Entity Framework Core database context for the SQLite work time database.
/// </summary>
public sealed class MakeBreakDbContext : DbContext
{
    public MakeBreakDbContext(DbContextOptions<MakeBreakDbContext> options) : base(options)
    {
    }

    public DbSet<WorkDayEntity> WorkDays => Set<WorkDayEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<WorkDayEntity>(entity =>
        {
            entity.HasKey(workDay => workDay.Id);
            entity.Property(workDay => workDay.Date).IsRequired();
            entity.HasIndex(workDay => workDay.Date).IsUnique();
        });
    }
}
