using System.Text;
using System.Text.Json;
using LogService.Application.Interfaces;
using LogService.Domain.Entities;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Shared.Events;

namespace LogService.Infrastructure.Consumers;

/// <summary>
/// RabbitMQ'dan log event'lerini dinleyen background service.
/// Merkezi log toplama mekanizması.
/// </summary>
public class LogEventConsumer : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<LogEventConsumer> _logger;
    private readonly IConnection _connection;
    private readonly IModel _channel;
    private const string ExchangeName = "microservices_exchange";
    private const string QueueName = "log_service_queue";

    public LogEventConsumer(IServiceProvider serviceProvider, IConfiguration config, ILogger<LogEventConsumer> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        var factory = new ConnectionFactory
        {
            HostName = config["RabbitMQ:HostName"] ?? "localhost",
            Port = int.Parse(config["RabbitMQ:Port"] ?? "5672"),
            UserName = config["RabbitMQ:UserName"] ?? "guest",
            Password = config["RabbitMQ:Password"] ?? "guest",
            AutomaticRecoveryEnabled = true
        };
        _connection = factory.CreateConnection();
        _channel = _connection.CreateModel();
        _channel.ExchangeDeclare(ExchangeName, ExchangeType.Topic, durable: true);
        _channel.QueueDeclare(QueueName, durable: true, exclusive: false, autoDelete: false);
        _channel.QueueBind(QueueName, ExchangeName, "ProductCreatedEvent");
        _channel.QueueBind(QueueName, ExchangeName, "ProductUpdatedEvent");
        _channel.QueueBind(QueueName, ExchangeName, "LogEvent");
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var consumer = new EventingBasicConsumer(_channel);
        consumer.Received += async (_, ea) =>
        {
            try
            {
                var body = Encoding.UTF8.GetString(ea.Body.ToArray());
                var routingKey = ea.RoutingKey;
                _logger.LogInformation("Event alındı: {RoutingKey}", routingKey);

                using var scope = _serviceProvider.CreateScope();
                var repo = scope.ServiceProvider.GetRequiredService<ILogRepository>();

                var logEntry = new LogEntry
                {
                    Level = "INFO",
                    ServiceName = routingKey.Contains("Product") ? "ProductService" : "Unknown",
                    Message = $"Event: {routingKey}",
                    Properties = body,
                    Timestamp = DateTime.UtcNow
                };

                await repo.AddAsync(logEntry);
                _channel.BasicAck(ea.DeliveryTag, false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Event işleme hatası");
                _channel.BasicNack(ea.DeliveryTag, false, true);
            }
        };
        _channel.BasicConsume(QueueName, false, consumer);
        return Task.CompletedTask;
    }

    public override void Dispose()
    {
        _channel?.Close(); _channel?.Dispose();
        _connection?.Close(); _connection?.Dispose();
        base.Dispose();
    }
}
