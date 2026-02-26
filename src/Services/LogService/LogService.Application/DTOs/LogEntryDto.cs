namespace LogService.Application.DTOs;

public record LogEntryDto
{
    public Guid Id { get; init; }
    public string Level { get; init; } = string.Empty;
    public string ServiceName { get; init; } = string.Empty;
    public string Message { get; init; } = string.Empty;
    public string? Exception { get; init; }
    public DateTime Timestamp { get; init; }
}
