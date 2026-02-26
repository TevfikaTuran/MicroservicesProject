using MediatR;
using Microsoft.Extensions.Logging;
using ProductService.Application.DTOs;
using ProductService.Application.Interfaces;
using ProductService.Domain.Interfaces;
using Shared.Common.Models;

namespace ProductService.Application.Queries.GetProducts;

/// <summary>
/// CQRS - Query Handler: Cache-Aside Pattern ile ürün listeleme.
/// </summary>
public class GetProductsQueryHandler : IRequestHandler<GetProductsQuery, ApiResponse<PaginatedResult<ProductDto>>>
{
    private readonly IProductRepository _repo;
    private readonly ICacheService _cache;
    private readonly ILogger<GetProductsQueryHandler> _logger;

    public GetProductsQueryHandler(IProductRepository repo, ICacheService cache, ILogger<GetProductsQueryHandler> logger)
    { _repo = repo; _cache = cache; _logger = logger; }

    public async Task<ApiResponse<PaginatedResult<ProductDto>>> Handle(GetProductsQuery request, CancellationToken ct)
    {
        var cacheKey = $"products:list:{request.PageNumber}:{request.PageSize}:{request.Category ?? "all"}";
        var cached = await _cache.GetAsync<PaginatedResult<ProductDto>>(cacheKey, ct);
        if (cached != null)
        {
            _logger.LogInformation("Cache HIT: {Key}", cacheKey);
            return ApiResponse<PaginatedResult<ProductDto>>.SuccessResult(cached);
        }

        var (items, totalCount) = await _repo.GetPagedAsync(request.PageNumber, request.PageSize, request.Category, ct);

        var result = new PaginatedResult<ProductDto>
        {
            Items = items.Select(p => new ProductDto
            {
                Id = p.Id, Name = p.Name, Description = p.Description,
                Price = p.Price, StockQuantity = p.StockQuantity, Category = p.Category,
                IsActive = p.IsActive, CreatedAt = p.CreatedAt, UpdatedAt = p.UpdatedAt
            }).ToList(),
            TotalCount = totalCount, PageNumber = request.PageNumber, PageSize = request.PageSize
        };

        await _cache.SetAsync(cacheKey, result, TimeSpan.FromMinutes(5), ct);
        _logger.LogInformation("Cache MISS -> DB'den okundu. Toplam: {Count}", totalCount);
        return ApiResponse<PaginatedResult<ProductDto>>.SuccessResult(result);
    }
}
