namespace Shared.Events;

/// <summary>
/// Ürün oluşturulduğunda RabbitMQ üzerinden yayınlanan event.
/// </summary>
public record ProductCreatedEvent
{
    public Guid ProductId { get; init; }
    public string ProductName { get; init; } = string.Empty;
    public decimal Price { get; init; }
    public DateTime CreatedAt { get; init; }
    public string CreatedBy { get; init; } = string.Empty;
}
