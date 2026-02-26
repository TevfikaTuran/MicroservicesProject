using LogService.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace LogService.Infrastructure.Persistence;

public class LogDbContext : DbContext
{
    public LogDbContext(DbContextOptions<LogDbContext> options) : base(options) { }
    public DbSet<LogEntry> LogEntries => Set<LogEntry>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        builder.Entity<LogEntry>(e =>
        {
            e.HasKey(l => l.Id);
            e.Property(l => l.Level).IsRequired().HasMaxLength(20);
            e.Property(l => l.ServiceName).IsRequired().HasMaxLength(100);
            e.Property(l => l.Message).IsRequired();
            e.HasIndex(l => l.Level);
            e.HasIndex(l => l.ServiceName);
            e.HasIndex(l => l.Timestamp);
        });
    }
}
