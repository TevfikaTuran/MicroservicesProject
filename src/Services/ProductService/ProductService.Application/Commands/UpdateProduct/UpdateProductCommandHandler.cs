using MediatR;
using Microsoft.Extensions.Logging;
using ProductService.Application.DTOs;
using ProductService.Application.Interfaces;
using ProductService.Domain.Interfaces;
using Shared.Common.Models;
using Shared.Events;

namespace ProductService.Application.Commands.UpdateProduct;

public class UpdateProductCommandHandler : IRequestHandler<UpdateProductCommand, ApiResponse<ProductDto>>
{
    private readonly IProductRepository _repo;
    private readonly IEventBus _eventBus;
    private readonly ICacheService _cache;
    private readonly ILogger<UpdateProductCommandHandler> _logger;

    public UpdateProductCommandHandler(IProductRepository repo, IEventBus eventBus, ICacheService cache, ILogger<UpdateProductCommandHandler> logger)
    { _repo = repo; _eventBus = eventBus; _cache = cache; _logger = logger; }

    public async Task<ApiResponse<ProductDto>> Handle(UpdateProductCommand request, CancellationToken ct)
    {
        var product = await _repo.GetByIdAsync(request.Id, ct);
        if (product == null) return ApiResponse<ProductDto>.FailResult("Ürün bulunamadı");

        product.Name = request.Name; product.Description = request.Description;
        product.Price = request.Price; product.StockQuantity = request.StockQuantity;
        product.Category = request.Category; product.UpdatedAt = DateTime.UtcNow;
        product.UpdatedBy = request.UpdatedBy;

        await _repo.UpdateAsync(product, ct);
        await _cache.RemoveAsync($"products:detail:{request.Id}", ct);
        await _cache.RemoveByPrefixAsync("products:list:", ct);

        await _eventBus.PublishAsync(new ProductUpdatedEvent
        {
            ProductId = product.Id, ProductName = product.Name, Price = product.Price,
            UpdatedAt = product.UpdatedAt.Value, UpdatedBy = product.UpdatedBy ?? string.Empty
        }, ct);

        _logger.LogInformation("Ürün güncellendi: {ProductId}", product.Id);
        return ApiResponse<ProductDto>.SuccessResult(new ProductDto
        {
            Id = product.Id, Name = product.Name, Description = product.Description,
            Price = product.Price, StockQuantity = product.StockQuantity,
            Category = product.Category, IsActive = product.IsActive,
            CreatedAt = product.CreatedAt, UpdatedAt = product.UpdatedAt
        }, "Ürün başarıyla güncellendi");
    }
}
