using LogService.Application.Interfaces;
using LogService.Infrastructure.Consumers;
using LogService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Serilog
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .WriteTo.Console(outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
    .WriteTo.Seq(builder.Configuration["Seq:Url"] ?? "http://localhost:5341")
    .Enrich.FromLogContext().Enrich.WithProperty("ServiceName", "LogService")
    .CreateLogger();
builder.Host.UseSerilog();

// DB
builder.Services.AddDbContext<LogDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("LogDb")));

// DI
builder.Services.AddScoped<ILogRepository, LogRepository>();

// RabbitMQ Consumer (BackgroundService)
builder.Services.AddHostedService<LogEventConsumer>();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();
if (app.Environment.IsDevelopment()) { app.UseSwagger(); app.UseSwaggerUI(); }
app.UseSerilogRequestLogging();
app.MapControllers();

using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<LogDbContext>();
    await context.Database.MigrateAsync();
}

Log.Information("Log Service başlatıldı");
app.Run();
