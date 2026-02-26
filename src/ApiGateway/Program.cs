using System.Text;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.IdentityModel.Tokens;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Serilog - Structured Logging (12 Faktör: Loglar standart çıktıya)
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .WriteTo.Console(outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
    .Enrich.FromLogContext().Enrich.WithProperty("ServiceName", "ApiGateway")
    .CreateLogger();
builder.Host.UseSerilog();

// YARP Reverse Proxy
builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

// Rate Limiting - YARP Gateway desteği ile API istekleri kontrol altına alınır
// Fixed Window: Belirli zaman penceresinde maksimum istek sayısı sınırlanır
var rateLimitConfig = builder.Configuration.GetSection("RateLimiting");
builder.Services.AddRateLimiter(options =>
{
    options.AddFixedWindowLimiter("fixed", opt =>
    {
        opt.PermitLimit = int.Parse(rateLimitConfig["PermitLimit"] ?? "100");
        opt.Window = TimeSpan.FromSeconds(int.Parse(rateLimitConfig["WindowSeconds"] ?? "60"));
        opt.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        opt.QueueLimit = int.Parse(rateLimitConfig["QueueLimit"] ?? "10");
    });
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.OnRejected = async (context, token) =>
    {
        context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
        await context.HttpContext.Response.WriteAsJsonAsync(new
        {
            success = false,
            message = "Çok fazla istek gönderildi. Lütfen bekleyip tekrar deneyin.",
            retryAfter = $"{int.Parse(rateLimitConfig["WindowSeconds"] ?? "60")} saniye"
        }, cancellationToken: token);
        Log.Warning("Rate limit aşıldı. IP: {IP}", context.HttpContext.Connection.RemoteIpAddress);
    };
});

// JWT Authentication (tüm servislerle aynı key - merkezi kimlik doğrulama)
var jwtSettings = builder.Configuration.GetSection("JwtSettings");
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true, ValidateAudience = true,
            ValidateLifetime = true, ValidateIssuerSigningKey = true,
            ValidIssuer = jwtSettings["Issuer"], ValidAudience = jwtSettings["Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings["Secret"]!)),
            ClockSkew = TimeSpan.Zero
        };
    });
builder.Services.AddAuthorization();

var app = builder.Build();
app.UseSerilogRequestLogging();
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();
app.MapReverseProxy();

Log.Information("API Gateway başlatıldı - Rate Limiting aktif (Fixed Window: {Limit} istek/{Window}sn)",
    rateLimitConfig["PermitLimit"], rateLimitConfig["WindowSeconds"]);
app.Run();
