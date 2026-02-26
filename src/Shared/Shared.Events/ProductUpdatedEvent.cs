namespace Shared.Events;

public record ProductUpdatedEvent
{
    public Guid ProductId { get; init; }
    public string ProductName { get; init; } = string.Empty;
    public decimal Price { get; init; }
    public DateTime UpdatedAt { get; init; }
    public string UpdatedBy { get; init; } = string.Empty;
}
