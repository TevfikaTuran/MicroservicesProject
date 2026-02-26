using MediatR;
using Shared.Common.Models;
using ProductService.Application.DTOs;

namespace ProductService.Application.Commands.UpdateProduct;

public record UpdateProductCommand : IRequest<ApiResponse<ProductDto>>
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public decimal Price { get; init; }
    public int StockQuantity { get; init; }
    public string Category { get; init; } = string.Empty;
    public string UpdatedBy { get; init; } = string.Empty;
}
