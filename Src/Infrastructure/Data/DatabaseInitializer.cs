using Microsoft.EntityFrameworkCore;

namespace makeBreak.Src.Infrastructure.Data;

/// <summary>
/// Ensures the SQLite database schema is created and up to date on application startup.
/// </summary>
public sealed class DatabaseInitializer
{
    private readonly MakeBreakDbContext _dbContext;

    public DatabaseInitializer(MakeBreakDbContext dbContext) => _dbContext = dbContext;

    /// <summary>
    /// Applies pending migrations to create or update the database schema.
    /// Invoked at application startup.
    /// </summary>
    public void Initialize() => _dbContext.Database.Migrate();
}