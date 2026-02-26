using MediatR;
using ProductService.Application.DTOs;
using Shared.Common.Models;

namespace ProductService.Application.Queries.GetProducts;

/// <summary>
/// CQRS - Query: Ürün listeleme. Redis Cache ile optimize edilir.
/// </summary>
public record GetProductsQuery : IRequest<ApiResponse<PaginatedResult<ProductDto>>>
{
    public int PageNumber { get; init; } = 1;
    public int PageSize { get; init; } = 10;
    public string? Category { get; init; }
}
