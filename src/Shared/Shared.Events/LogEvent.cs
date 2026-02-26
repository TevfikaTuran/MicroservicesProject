namespace Shared.Events;

/// <summary>
/// Merkezi log event modeli. Structured Logging formatı.
/// </summary>
public record LogEvent
{
    public string Level { get; init; } = "INFO";
    public string ServiceName { get; init; } = string.Empty;
    public string Message { get; init; } = string.Empty;
    public string? Exception { get; init; }
    public string? StackTrace { get; init; }
    public Dictionary<string, object> Properties { get; init; } = new();
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
}
