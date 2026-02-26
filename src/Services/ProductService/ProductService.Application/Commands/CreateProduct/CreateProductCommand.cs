using MediatR;
using Shared.Common.Models;
using ProductService.Application.DTOs;

namespace ProductService.Application.Commands.CreateProduct;

/// <summary>
/// CQRS - Command: Ürün oluşturma komutu.
/// </summary>
public record CreateProductCommand : IRequest<ApiResponse<ProductDto>>
{
    public string Name { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public decimal Price { get; init; }
    public int StockQuantity { get; init; }
    public string Category { get; init; } = string.Empty;
    public string CreatedBy { get; init; } = string.Empty;
}
