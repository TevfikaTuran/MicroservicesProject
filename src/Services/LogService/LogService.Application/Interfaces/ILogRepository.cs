using LogService.Domain.Entities;

namespace LogService.Application.Interfaces;

public interface ILogRepository
{
    Task AddAsync(LogEntry logEntry, CancellationToken ct = default);
    Task<IReadOnlyList<LogEntry>> GetAllAsync(int pageNumber, int pageSize, string? level = null, CancellationToken ct = default);
    Task<int> GetCountAsync(string? level = null, CancellationToken ct = default);
}
