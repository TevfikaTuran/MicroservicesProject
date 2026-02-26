using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using ProductService.Application.Interfaces;
using RabbitMQ.Client;

namespace ProductService.Infrastructure.Messaging;

/// <summary>
/// RabbitMQ Event Bus. Event-Driven Architecture ile asenkron iletişim.
/// Disposability: IDisposable ile kaynaklar düzgün serbest bırakılır.
/// </summary>
public class RabbitMqEventBus : IEventBus, IDisposable
{
    private readonly IConnection _connection;
    private readonly IModel _channel;
    private readonly ILogger<RabbitMqEventBus> _logger;
    private const string ExchangeName = "microservices_exchange";

    public RabbitMqEventBus(IConfiguration config, ILogger<RabbitMqEventBus> logger)
    {
        _logger = logger;
        var factory = new ConnectionFactory
        {
            HostName = config["RabbitMQ:HostName"] ?? "localhost",
            Port = int.Parse(config["RabbitMQ:Port"] ?? "5672"),
            UserName = config["RabbitMQ:UserName"] ?? "guest",
            Password = config["RabbitMQ:Password"] ?? "guest",
            AutomaticRecoveryEnabled = true
        };
        try
        {
            _connection = factory.CreateConnection();
            _channel = _connection.CreateModel();
            _channel.ExchangeDeclare(ExchangeName, ExchangeType.Topic, durable: true);
            _logger.LogInformation("RabbitMQ bağlantısı kuruldu");
        }
        catch (Exception ex) { _logger.LogError(ex, "RabbitMQ bağlantı hatası"); throw; }
    }

    public Task PublishAsync<T>(T @event, CancellationToken ct = default) where T : class
    {
        var eventName = typeof(T).Name;
        var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(@event));
        var props = _channel.CreateBasicProperties();
        props.Persistent = true;
        props.ContentType = "application/json";
        _channel.BasicPublish(exchange: ExchangeName, routingKey: eventName, basicProperties: props, body: body);
        _logger.LogInformation("Event yayınlandı: {EventName}", eventName);
        return Task.CompletedTask;
    }

    public void Dispose()
    {
        _channel?.Close(); _channel?.Dispose();
        _connection?.Close(); _connection?.Dispose();
    }
}
