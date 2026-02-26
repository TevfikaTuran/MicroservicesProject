using MediatR;
using Microsoft.Extensions.Logging;
using ProductService.Application.DTOs;
using ProductService.Application.Interfaces;
using ProductService.Domain.Interfaces;
using Shared.Common.Models;

namespace ProductService.Application.Queries.GetProductById;

public class GetProductByIdQueryHandler : IRequestHandler<GetProductByIdQuery, ApiResponse<ProductDto>>
{
    private readonly IProductRepository _repo;
    private readonly ICacheService _cache;
    private readonly ILogger<GetProductByIdQueryHandler> _logger;

    public GetProductByIdQueryHandler(IProductRepository repo, ICacheService cache, ILogger<GetProductByIdQueryHandler> logger)
    { _repo = repo; _cache = cache; _logger = logger; }

    public async Task<ApiResponse<ProductDto>> Handle(GetProductByIdQuery request, CancellationToken ct)
    {
        var cacheKey = $"products:detail:{request.Id}";
        var cached = await _cache.GetAsync<ProductDto>(cacheKey, ct);
        if (cached != null) return ApiResponse<ProductDto>.SuccessResult(cached);

        var product = await _repo.GetByIdAsync(request.Id, ct);
        if (product == null) return ApiResponse<ProductDto>.FailResult("Ürün bulunamadı");

        var dto = new ProductDto
        {
            Id = product.Id, Name = product.Name, Description = product.Description,
            Price = product.Price, StockQuantity = product.StockQuantity,
            Category = product.Category, IsActive = product.IsActive,
            CreatedAt = product.CreatedAt, UpdatedAt = product.UpdatedAt
        };
        await _cache.SetAsync(cacheKey, dto, TimeSpan.FromMinutes(10), ct);
        return ApiResponse<ProductDto>.SuccessResult(dto);
    }
}
