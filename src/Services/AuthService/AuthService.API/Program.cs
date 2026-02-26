using System.Text;
using AuthService.Application.Interfaces;
using AuthService.Domain.Enums;
using AuthService.Infrastructure.Identity;
using AuthService.Infrastructure.Persistence;
using AuthService.Infrastructure.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Serilog - Structured Logging (12 Faktör: Loglar standart çıktıya)
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .WriteTo.Console(outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
    .WriteTo.Seq(builder.Configuration["Seq:Url"] ?? "http://localhost:5341")
    .Enrich.FromLogContext()
    .Enrich.WithProperty("ServiceName", "AuthService")
    .CreateLogger();
builder.Host.UseSerilog();

// Veritabanı (12 Faktör - Konfigürasyon: Ortam değişkenleri)
builder.Services.AddDbContext<AuthDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("AuthDb")));

// Microsoft Identity
builder.Services.AddIdentity<AppIdentityUser, IdentityRole>(options =>
{
    options.Password.RequiredLength = 6;
    options.Password.RequireNonAlphanumeric = false;
    options.User.RequireUniqueEmail = true;
})
.AddEntityFrameworkStores<AuthDbContext>()
.AddDefaultTokenProviders();

// JWT Authentication
var jwtSettings = builder.Configuration.GetSection("JwtSettings");
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtSettings["Issuer"],
        ValidAudience = jwtSettings["Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings["Secret"]!)),
        ClockSkew = TimeSpan.Zero
    };
});

// Authorization Policies (Role-Based + Policy-Based)
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy(Policies.RequireAdmin, policy => policy.RequireRole(Roles.Admin));
    options.AddPolicy(Policies.RequireManager, policy => policy.RequireRole(Roles.Manager, Roles.Admin));
    options.AddPolicy(Policies.CanManageProducts, policy => policy.RequireRole(Roles.Admin, Roles.Manager));
});

// DI (Dependency Inversion Principle)
builder.Services.AddScoped<TokenService>();
builder.Services.AddScoped<IAuthService, AuthServiceImpl>();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment()) { app.UseSwagger(); app.UseSwaggerUI(); }

app.UseSerilogRequestLogging();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

// DB Migration + Seed
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<AuthDbContext>();
    await context.Database.MigrateAsync();

    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<AppIdentityUser>>();

    string[] roles = { Roles.Admin, Roles.User, Roles.Manager };
    foreach (var role in roles)
        if (!await roleManager.RoleExistsAsync(role))
            await roleManager.CreateAsync(new IdentityRole(role));

    var adminEmail = builder.Configuration["AdminSettings:Email"] ?? "admin@microservices.com";
    if (await userManager.FindByEmailAsync(adminEmail) == null)
    {
        var adminUser = new AppIdentityUser
        {
            UserName = "admin", Email = adminEmail,
            FirstName = "System", LastName = "Admin", EmailConfirmed = true
        };
        await userManager.CreateAsync(adminUser, builder.Configuration["AdminSettings:Password"] ?? "Admin123!");
        await userManager.AddToRoleAsync(adminUser, Roles.Admin);
    }
}

Log.Information("Auth Service başlatıldı");
app.Run();
