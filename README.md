# MicroservicesProject

## Proje Hakkında

.NET 8 ile geliştirilmiş, mikroservis mimarisine sahip bir backend projesidir. Onion Architecture, SOLID prensipleri ve 12 Faktör Uygulama metodolojisi temel alınarak tasarlanmıştır.

Kullanılan teknolojiler: .NET 8, C#, SQL Server, Redis, RabbitMQ, Serilog, MediatR, FluentValidation, YARP, Entity Framework Core

## Proje Yapısı

```
MicroservicesProject/
├── src/
│   ├── ApiGateway/                            # YARP Reverse Proxy + Rate Limiting + JWT
│   ├── Shared/
│   │   ├── Shared.Common/                     # Ortak modeller (ApiResponse, PaginatedResult)
│   │   └── Shared.Events/                     # RabbitMQ event modelleri
│   └── Services/
│       ├── AuthService/                       # Kimlik doğrulama servisi
│       │   ├── AuthService.Domain/
│       │   ├── AuthService.Application/
│       │   ├── AuthService.Infrastructure/
│       │   └── AuthService.API/
│       ├── ProductService/                    # Ürün yönetim servisi
│       │   ├── ProductService.Domain/
│       │   ├── ProductService.Application/
│       │   ├── ProductService.Infrastructure/
│       │   ├── ProductService.Persistence/
│       │   └── ProductService.API/
│       └── LogService/                        # Merkezi log servisi
│           ├── LogService.Domain/
│           ├── LogService.Application/
│           ├── LogService.Infrastructure/
│           └── LogService.API/
```

Katman bağımlılık kuralı: Domain (en iç) -> Application -> Infrastructure/Persistence -> API (en dış). Domain katmanının hiçbir dış bağımlılığı yoktur; Application sadece Domain'e, Infrastructure ise Application'a bağımlıdır.

## Kurulum

### Gereksinimler

- .NET 8 SDK
- SQL Server Express
- Redis (Windows)
- RabbitMQ + Erlang
- Git

### Adımlar

Projeyi klonlayın:

```bash
git clone https://github.com/TevfikaTuran/MicroservicesProject.git
cd MicroservicesProject
dotnet restore
```

Her servisin `appsettings.json` dosyasındaki connection string'i kendi SQL Server instance adınıza göre düzenleyin:

```json
"ConnectionStrings": {
    "AuthDb": "Server=YOUR_SERVER_NAME;Database=AuthServiceDb;Trusted_Connection=True;TrustServerCertificate=True"
}
```

Migration'ları uygulayın:

```bash
dotnet ef database update --project "src/Services/AuthService/AuthService.Infrastructure" --startup-project "src/Services/AuthService/AuthService.API"
dotnet ef database update --project "src/Services/ProductService/ProductService.Persistence" --startup-project "src/Services/ProductService/ProductService.API"
dotnet ef database update --project "src/Services/LogService/LogService.Infrastructure" --startup-project "src/Services/LogService/LogService.API"
```

### Çalıştırma

Her servisi ayrı bir terminal penceresinde başlatın:

```bash
dotnet run --project "src/Services/AuthService/AuthService.API"        # Port 5001
dotnet run --project "src/Services/ProductService/ProductService.API"   # Port 5002
dotnet run --project "src/Services/LogService/LogService.API"           # Port 5003
dotnet run --project "src/ApiGateway"                                   # Port 5000
```

Swagger arayüzleri:

- Auth Service: http://localhost:5001/swagger
- Product Service: http://localhost:5002/swagger
- Log Service: http://localhost:5003/swagger
- API Gateway: http://localhost:5000 (tüm servislere yönlendirir)

## API Endpoint'leri

### Auth Service (/api/auth)

| Method | Endpoint | Açıklama | Yetki |
|--------|----------|----------|-------|
| POST | /api/auth/register | Yeni kullanıcı kaydı | - |
| POST | /api/auth/login | Giriş, JWT token döner | - |
| POST | /api/auth/refresh-token | Token yenileme | - |
| POST | /api/auth/revoke | Token iptal | JWT |
| POST | /api/auth/assign-role | Rol atama | Admin |

### Product Service (/api/products)

| Method | Endpoint | Açıklama | Yetki |
|--------|----------|----------|-------|
| GET | /api/products | Ürün listesi (Redis cache) | - |
| GET | /api/products/{id} | Ürün detay (Redis cache) | - |
| POST | /api/products | Ürün ekle | JWT |
| PUT | /api/products/{id} | Ürün güncelle | JWT |

### Log Service (/api/logs)

| Method | Endpoint | Açıklama | Yetki |
|--------|----------|----------|-------|
| GET | /api/logs | Log kayıtları | - |

## Teknik Detaylar

### CQRS

Product Service'te komut ve sorgu işlemleri MediatR üzerinden ayrıştırılmıştır. Yazma işlemleri (CreateProductCommand, UpdateProductCommand) command handler'lar ile, okuma işlemleri (GetProductsQuery, GetProductByIdQuery) query handler'lar ile gerçekleştirilir. ValidationBehavior ile pipeline üzerinde girdi doğrulaması yapılır.

### Event-Driven Mimari

Ürün ekleme ve güncelleme işlemlerinde RabbitMQ üzerinden event yayınlanır. Log Service bu event'leri bir BackgroundService aracılığıyla dinler ve veritabanına kaydeder.

### Cache

Ürün sorguları Redis üzerinde Cache-Aside pattern ile önbelleklenir. Ürün eklendiğinde veya güncellendiğinde ilgili cache anahtarları temizlenir.

### Kimlik Doğrulama ve Yetkilendirme

JWT token tabanlı kimlik doğrulama uygulanmıştır. Refresh token rotation mekanizması ile token yenileme desteklenir. Roller (Admin, User, Manager) ve politikalar (RequireAdmin, RequireManager, CanManageProducts) ile yetkilendirme sağlanır.

### Rate Limiting

API Gateway üzerinde YARP desteğiyle Fixed Window rate limiting uygulanmıştır. Varsayılan olarak 60 saniyelik pencerede 100 istek kabul edilir, aşıldığında HTTP 429 döner.

### SOLID Prensipleri

- Single Responsibility: Her sınıf tek bir sorumluluk taşır (TokenService, RedisCacheService, ProductRepository vb.)
- Open/Closed: MediatR handler yapısı sayesinde yeni özellikler mevcut kodu değiştirmeden eklenebilir.
- Liskov Substitution: Repository interface'leri farklı implementasyonlarla değiştirilebilir.
- Interface Segregation: ICacheService, IEventBus, IAuthService gibi ayrık interface'ler tanımlanmıştır.
- Dependency Inversion: Üst katmanlar somut sınıflara değil, soyutlamalara bağımlıdır.

### 12 Faktör Uygulama

Proje 12 faktör metodolojisine uygun şekilde yapılandırılmıştır: Git ile merkezi kod tabanı, NuGet ile bağımlılık yönetimi, appsettings.json ile ortama özel konfigürasyon, SQL Server/Redis/RabbitMQ bağımsız destek servisleri olarak kullanılmakta, build ve çalışma süreçleri ayrılmış durumda, JWT tabanlı stateless tasarım, her servis farklı portta, async/await ile concurrency desteği, IDisposable ile graceful shutdown, Development/Production parity, Serilog ile merkezi log yönetimi ve EF Core migration'lar ile admin süreçleri sağlanmaktadır.

## Veritabanları

| Veritabanı | Servis | Tablolar |
|-----------|--------|---------|
| AuthServiceDb | Auth | AspNetUsers, AspNetRoles, RefreshTokens |
| ProductServiceDb | Product | Products |
| LogServiceDb | Log | LogEntries |

## Versiyonlama

- `master` — Ana branch
- `test/v1.0.0` — Test ortamı
- `prod/v1.0.0` — Production ortamı

## Kod Deposu

https://github.com/TevfikaTuran/MicroservicesProject
