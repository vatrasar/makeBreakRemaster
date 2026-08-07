using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace makeBreak.Src.Infrastructure.Data;

/// <summary>
/// Design-time factory used by the EF Core tooling to create a <see cref="MakeBreakDbContext"/>
/// for generating migrations, independently of the application DI container.
/// </summary>
public sealed class MakeBreakDbContextFactory : IDesignTimeDbContextFactory<MakeBreakDbContext>
{
    public MakeBreakDbContext CreateDbContext(string[] args)
    {
        DbContextOptionsBuilder<MakeBreakDbContext> optionsBuilder = new();
        optionsBuilder.UseSqlite("Data Source=worktime.design.db");
        return new MakeBreakDbContext(optionsBuilder.Options);
    }
}