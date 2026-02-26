using MediatR;
using ProductService.Application.DTOs;
using Shared.Common.Models;

namespace ProductService.Application.Queries.GetProductById;

public record GetProductByIdQuery(Guid Id) : IRequest<ApiResponse<ProductDto>>;
