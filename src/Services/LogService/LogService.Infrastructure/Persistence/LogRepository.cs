using LogService.Application.Interfaces;
using LogService.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace LogService.Infrastructure.Persistence;

public class LogRepository : ILogRepository
{
    private readonly LogDbContext _context;
    public LogRepository(LogDbContext context) => _context = context;

    public async Task AddAsync(LogEntry logEntry, CancellationToken ct = default)
    {
        await _context.LogEntries.AddAsync(logEntry, ct);
        await _context.SaveChangesAsync(ct);
    }

    public async Task<IReadOnlyList<LogEntry>> GetAllAsync(int pageNumber, int pageSize, string? level = null, CancellationToken ct = default)
    {
        var query = _context.LogEntries.AsNoTracking().AsQueryable();
        if (!string.IsNullOrEmpty(level)) query = query.Where(l => l.Level == level);
        return await query.OrderByDescending(l => l.Timestamp).Skip((pageNumber - 1) * pageSize).Take(pageSize).ToListAsync(ct);
    }

    public async Task<int> GetCountAsync(string? level = null, CancellationToken ct = default)
    {
        var query = _context.LogEntries.AsQueryable();
        if (!string.IsNullOrEmpty(level)) query = query.Where(l => l.Level == level);
        return await query.CountAsync(ct);
    }
}
