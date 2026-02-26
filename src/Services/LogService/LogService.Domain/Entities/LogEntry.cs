namespace LogService.Domain.Entities;

/// <summary>
/// Merkezi log kaydı entity'si. Structured Logging formatı.
/// </summary>
public class LogEntry
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Level { get; set; } = "INFO";
    public string ServiceName { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string? Exception { get; set; }
    public string? StackTrace { get; set; }
    public string? Properties { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}
