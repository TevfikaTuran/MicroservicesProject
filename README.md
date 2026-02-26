# MicroservicesProject

## 📋 Proje Hakkında

.NET 8 tabanlı mikroservis mimarisiyle geliştirilmiş backend projesidir. SOLID prensipleri, 12 Faktör Uygulama metodolojisi ve Onion Architecture kullanılarak tasarlanmıştır.

**Teknolojiler:** .NET 8, C#, SQL Server, Redis, RabbitMQ, Serilog, MediatR, FluentValidation, YARP, Entity Framework Core

## 🏗️ Mimari Yapı

```
MicroservicesProject/
├── src/
│   ├── ApiGateway/                          # YARP Reverse Proxy + Rate Limiting + JWT
│   ├── Shared/
│   │   ├── Shared.Common/                   # Ortak modeller (ApiResponse, PaginatedResult)
│   │   └── Shared.Events/                   # RabbitMQ event modelleri
│   └── Services/
│       ├── AuthService/                     # Kimlik Doğrulama Servisi
│       │   ├── AuthService.Domain/          # Entity'ler (ApplicationUser, RefreshToken)
│       │   ├── AuthService.Application/     # Interface'ler, DTO'lar
│       │   ├── AuthService.Infrastructure/  # Identity, JWT, EF Core, DbContext
│       │   └── AuthService.API/             # Controller'lar, Program.cs
│       ├── ProductService/                  # Ürün Yönetim Servisi
│       │   ├── ProductService.Domain/       # Entity'ler, Repository Interface'leri
│       │   ├── ProductService.Application/  # CQRS Commands/Queries, Validators
│       │   ├── ProductService.Infrastructure/ # Redis Cache, RabbitMQ EventBus
│       │   ├── ProductService.Persistence/  # EF Core DbContext, Repositories
│       │   └── ProductService.API/          # Controller'lar, Program.cs
│       └── LogService/                      # Merkezi Log Servisi
│           ├── LogService.Domain/           # Entity'ler (LogEntry)
│           ├── LogService.Application/      # Interface'ler, DTO'lar
│           ├── LogService.Infrastructure/   # RabbitMQ Consumer, EF Core
│           └── LogService.API/              # Controller'lar, Program.cs
```

### Onion Architecture (Katman Bağımlılık Kuralı)

```
Domain (en iç) → Application → Infrastructure/Persistence → API (en dış)
```

- **Domain:** Entity'ler, Enum'lar — hiçbir dış bağımlılığı yoktur
- **Application:** Interface'ler, DTO'lar, CQRS Command/Query — sadece Domain'e bağımlı
- **Infrastructure/Persistence:** Implementasyonlar (EF Core, Redis, RabbitMQ) — Application'a bağımlı
- **API:** Controller'lar, DI konfigürasyonu — tüm katmanlara bağımlı

## 🔧 Kurulum ve Çalıştırma

### Ön Gereksinimler

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [SQL Server Express](https://www.microsoft.com/en-us/sql-server/sql-server-downloads)
- [Redis](https://github.com/tporadowski/redis/releases) (Windows)
- [RabbitMQ](https://www.rabbitmq.com/download.html) + [Erlang](https://www.erlang.org/downloads)
- [Git](https://git-scm.com/downloads)

### 1. Projeyi Klonlayın

```bash
git clone https://github.com/TevfikaTuran/MicroservicesProject.git
cd MicroservicesProject
```

### 2. Bağımlılıkları Geri Yükleyin

```bash
dotnet restore
```

### 3. Veritabanını Yapılandırın

`appsettings.json` dosyalarındaki connection string'leri kendi SQL Server instance adınıza göre güncelleyin:

```json
"ConnectionStrings": {
    "AuthDb": "Server=YOUR_SERVER_NAME;Database=AuthServiceDb;Trusted_Connection=True;TrustServerCertificate=True"
}
```

### 4. Migration'ları Uygulayın

```bash
dotnet ef database update --project "src/Services/AuthService/AuthService.Infrastructure" --startup-project "src/Services/AuthService/AuthService.API"
dotnet ef database update --project "src/Services/ProductService/ProductService.Persistence" --startup-project "src/Services/ProductService/ProductService.API"
dotnet ef database update --project "src/Services/LogService/LogService.Infrastructure" --startup-project "src/Services/LogService/LogService.API"
```

### 5. Servisleri Başlatın (Her biri ayrı terminalde)

```bash
# Terminal 1 - Auth Service (Port 5001)
dotnet run --project "src/Services/AuthService/AuthService.API"

# Terminal 2 - Product Service (Port 5002)
dotnet run --project "src/Services/ProductService/ProductService.API"

# Terminal 3 - Log Service (Port 5003)
dotnet run --project "src/Services/LogService/LogService.API"

# Terminal 4 - API Gateway (Port 5000)
dotnet run --project "src/ApiGateway"
```

### 6. Swagger UI ile Test

| Servis | URL |
|--------|-----|
| Auth Service | http://localhost:5001/swagger |
| Product Service | http://localhost:5002/swagger |
| Log Service | http://localhost:5003/swagger |
| API Gateway | http://localhost:5000 (tüm istekler) |

## 📡 API Endpoint'leri

### Auth Service (`/api/auth`)

| Method | Endpoint | Açıklama | Yetki |
|--------|----------|----------|-------|
| POST | `/api/auth/register` | Yeni kullanıcı kaydı | - |
| POST | `/api/auth/login` | Giriş (JWT token döner) | - |
| POST | `/api/auth/refresh-token` | Token yenileme | - |
| POST | `/api/auth/revoke` | Token iptal | JWT |
| POST | `/api/auth/assign-role` | Rol atama | Admin |

### Product Service (`/api/products`)

| Method | Endpoint | Açıklama | Yetki |
|--------|----------|----------|-------|
| GET | `/api/products` | Ürün listesi (Redis Cache) | - |
| GET | `/api/products/{id}` | Ürün detay (Redis Cache) | - |
| POST | `/api/products` | Ürün ekle (RabbitMQ event) | JWT |
| PUT | `/api/products/{id}` | Ürün güncelle | JWT |

### Log Service (`/api/logs`)

| Method | Endpoint | Açıklama | Yetki |
|--------|----------|----------|-------|
| GET | `/api/logs` | Log kayıtları | - |

## 🎯 Design Pattern'ler ve Prensipler

### CQRS (Command Query Responsibility Segregation)
- **Commands:** CreateProductCommand, UpdateProductCommand → Veritabanına yazma
- **Queries:** GetProductsQuery, GetProductByIdQuery → Redis Cache'den okuma
- **MediatR** ile pipeline behavior (ValidationBehavior)

### Event-Driven Architecture
- **RabbitMQ** ile asenkron iletişim
- ProductCreatedEvent → Log Service tarafından tüketilir
- ProductUpdatedEvent → Log Service tarafından tüketilir

### Cache-Aside Pattern
- Redis ile ürün sorguları önbelleklenir
- Cache Invalidation: Ürün eklendiğinde/güncellendiğinde cache temizlenir

### SOLID Prensipleri
- **SRP:** Her sınıf tek sorumluluk (TokenService, RedisCacheService, ProductRepository)
- **OCP:** MediatR handler'ları ile yeni özellikler eklemeye açık
- **LSP:** IProductRepository implementasyonları birbirinin yerine geçebilir
- **ISP:** ICacheService, IEventBus, IAuthService — ayrık interface'ler
- **DIP:** Üst katmanlar interface'lere bağımlı, implementasyonlara değil

### 12 Faktör Uygulama
1. **Kod Tabanı:** Git ile merkezi repo
2. **Bağımlılıklar:** NuGet paketleri
3. **Konfigürasyon:** appsettings.json + ortam değişkenleri
4. **Destek Servisleri:** SQL Server, Redis, RabbitMQ bağımsız
5. **Build/Çalışma Ayrımı:** dotnet build / dotnet run
6. **Stateless:** JWT token tabanlı, session yok
7. **Port Bağımsızlığı:** Her servis farklı port
8. **Concurrency:** Async/await, yatay ölçeklenebilir
9. **Disposability:** IDisposable, graceful shutdown
10. **Test/Prod Paritesi:** appsettings.Development.json / appsettings.json
11. **Loglar:** Serilog → Console + Seq (standart çıktı)
12. **Admin Prosesleri:** EF Core migrations, seed data

### Role-Based & Policy-Based Authorization
- **Roller:** Admin, User, Manager
- **Politikalar:** RequireAdmin, RequireManager, CanManageProducts

### Rate Limiting (YARP Gateway)
- Fixed Window: 100 istek/60 saniye
- Aşıldığında HTTP 429 (Too Many Requests)

## 🗃️ Veritabanları

| Veritabanı | Servis | Tablolar |
|-----------|--------|---------|
| AuthServiceDb | Auth | AspNetUsers, AspNetRoles, RefreshTokens |
| ProductServiceDb | Product | Products |
| LogServiceDb | Log | LogEntries |

## 📦 Versiyonlama

- `master` — Ana branch
- `test/v1.0.0` — Test ortamı
- `prod/v1.0.0` — Production ortamı

## 📂 Kod Deposu

👉 [https://github.com/TevfikaTuran/MicroservicesProject](https://github.com/TevfikaTuran/MicroservicesProject)
