namespace ProductService.Application.Interfaces;

/// <summary>
/// Event bus soyutlaması (Dependency Inversion Principle). RabbitMQ impl Infrastructure'da.
/// </summary>
public interface IEventBus
{
    Task PublishAsync<T>(T @event, CancellationToken ct = default) where T : class;
}
