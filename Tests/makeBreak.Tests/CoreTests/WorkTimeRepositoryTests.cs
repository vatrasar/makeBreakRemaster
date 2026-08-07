using makeBreak.Src.Core.Domain.Models;
using makeBreak.Src.Infrastructure.Data;
using makeBreak.Src.Infrastructure.Data.Repositories;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace makeBreak.Tests.CoreTests;

public class WorkTimeRepositoryTests
{
    [Fact]
    public void AddWorkSeconds_whenRecordMissing_createsRecord()
    {
        using TestDbContext testDb = TestDbContext.Create();
        DateOnly date = DateOnly.FromDateTime(DateTime.Now);

        testDb.Repository.AddWorkSeconds(date, 300);

        IReadOnlyList<WorkDay> loaded = testDb.Repository.GetWorkDaysInRange(date, date);
        WorkDay day = Assert.Single(loaded);
        Assert.Equal(300, day.WorkSeconds);
    }

    [Fact]
    public void AddWorkSeconds_whenRecordExists_accumulatesSeconds()
    {
        using TestDbContext testDb = TestDbContext.Create();
        DateOnly date = DateOnly.FromDateTime(DateTime.Now);

        testDb.Repository.AddWorkSeconds(date, 300);
        testDb.Repository.AddWorkSeconds(date, 120);

        IReadOnlyList<WorkDay> loaded = testDb.Repository.GetWorkDaysInRange(date, date);
        WorkDay day = Assert.Single(loaded);
        Assert.Equal(420, day.WorkSeconds);
    }

    [Fact]
    public void GetWorkDaysInRange_filtersByRange()
    {
        using TestDbContext testDb = TestDbContext.Create();
        DateOnly older = DateOnly.FromDateTime(DateTime.Now.AddDays(-3));
        DateOnly middle = DateOnly.FromDateTime(DateTime.Now.AddDays(-2));
        DateOnly newer = DateOnly.FromDateTime(DateTime.Now.AddDays(-1));
        testDb.Repository.AddWorkSeconds(older, 100);
        testDb.Repository.AddWorkSeconds(middle, 200);
        testDb.Repository.AddWorkSeconds(newer, 300);

        IReadOnlyList<WorkDay> loaded = testDb.Repository.GetWorkDaysInRange(middle, newer);

        Assert.Equal(2, loaded.Count);
        Assert.Equal(new[] { middle, newer }, loaded.Select(day => day.Date));
    }

    [Fact]
    public void DeleteOlderThan_removesOnlyOldRecords()
    {
        using TestDbContext testDb = TestDbContext.Create();
        DateOnly old = DateOnly.FromDateTime(DateTime.Now.AddDays(-10));
        DateOnly recent = DateOnly.FromDateTime(DateTime.Now.AddDays(-2));
        testDb.Repository.AddWorkSeconds(old, 100);
        testDb.Repository.AddWorkSeconds(recent, 200);
        DateOnly cutoff = DateOnly.FromDateTime(DateTime.Now.AddDays(-7));

        testDb.Repository.DeleteOlderThan(cutoff);

        IReadOnlyList<WorkDay> remaining = testDb.Repository.GetWorkDaysInRange(old, recent);
        Assert.Equal(recent, Assert.Single(remaining).Date);
    }

    private sealed class TestDbContext : IDisposable
    {
        private readonly string _filePath;
        private readonly MakeBreakDbContext _dbContext;

        private TestDbContext(string filePath, MakeBreakDbContext dbContext)
        {
            _filePath = filePath;
            _dbContext = dbContext;
            Repository = new WorkTimeRepository(dbContext);
        }

        public WorkTimeRepository Repository { get; }

        public static TestDbContext Create()
        {
            string filePath = Path.Combine(Path.GetTempPath(), $"worktime_{Guid.NewGuid():N}.db");
            DbContextOptions<MakeBreakDbContext> options = new DbContextOptionsBuilder<MakeBreakDbContext>()
                .UseSqlite($"Data Source={filePath}")
                .Options;
            var dbContext = new MakeBreakDbContext(options);
            dbContext.Database.EnsureCreated();
            return new TestDbContext(filePath, dbContext);
        }

        public void Dispose()
        {
            _dbContext.Dispose();

            if (File.Exists(_filePath))
            {
                File.Delete(_filePath);
            }
        }
    }
}