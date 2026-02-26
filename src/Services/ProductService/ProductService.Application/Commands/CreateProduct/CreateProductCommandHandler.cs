using MediatR;
using Microsoft.Extensions.Logging;
using ProductService.Application.DTOs;
using ProductService.Application.Interfaces;
using ProductService.Domain.Entities;
using ProductService.Domain.Interfaces;
using Shared.Common.Models;
using Shared.Events;

namespace ProductService.Application.Commands.CreateProduct;

/// <summary>
/// CQRS - Command Handler: Ürün oluşturma. Event fırlatarak diğer servisleri bilgilendirir.
/// </summary>
public class CreateProductCommandHandler : IRequestHandler<CreateProductCommand, ApiResponse<ProductDto>>
{
    private readonly IProductRepository _repo;
    private readonly IEventBus _eventBus;
    private readonly ICacheService _cache;
    private readonly ILogger<CreateProductCommandHandler> _logger;

    public CreateProductCommandHandler(IProductRepository repo, IEventBus eventBus, ICacheService cache, ILogger<CreateProductCommandHandler> logger)
    { _repo = repo; _eventBus = eventBus; _cache = cache; _logger = logger; }

    public async Task<ApiResponse<ProductDto>> Handle(CreateProductCommand request, CancellationToken ct)
    {
        var product = new Product
        {
            Name = request.Name, Description = request.Description,
            Price = request.Price, StockQuantity = request.StockQuantity,
            Category = request.Category, CreatedBy = request.CreatedBy
        };

        var created = await _repo.AddAsync(product, ct);
        await _cache.RemoveByPrefixAsync("products:", ct);

        // Event fırlat - diğer servisler bilgilendirilir
        await _eventBus.PublishAsync(new ProductCreatedEvent
        {
            ProductId = created.Id, ProductName = created.Name,
            Price = created.Price, CreatedAt = created.CreatedAt, CreatedBy = created.CreatedBy
        }, ct);

        _logger.LogInformation("Ürün oluşturuldu: {ProductId} - {Name}", created.Id, created.Name);

        return ApiResponse<ProductDto>.SuccessResult(new ProductDto
        {
            Id = created.Id, Name = created.Name, Description = created.Description,
            Price = created.Price, StockQuantity = created.StockQuantity,
            Category = created.Category, IsActive = created.IsActive, CreatedAt = created.CreatedAt
        }, "Ürün başarıyla oluşturuldu");
    }
}
