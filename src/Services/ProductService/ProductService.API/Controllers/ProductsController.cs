using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProductService.Application.Commands.CreateProduct;
using ProductService.Application.Commands.UpdateProduct;
using ProductService.Application.Queries.GetProductById;
using ProductService.Application.Queries.GetProducts;

namespace ProductService.API.Controllers;

/// <summary>
/// Ürün CRUD. CQRS: Command ve Query ayrıştırılmıştır.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class ProductsController : ControllerBase
{
    private readonly IMediator _mediator;
    public ProductsController(IMediator mediator) => _mediator = mediator;

    /// <summary>GET api/products - Redis Cache ile optimize edilir</summary>
    [HttpGet]
    public async Task<IActionResult> GetProducts([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10, [FromQuery] string? category = null)
    {
        var result = await _mediator.Send(new GetProductsQuery { PageNumber = pageNumber, PageSize = pageSize, Category = category });
        return Ok(result);
    }

    /// <summary>GET api/products/{id}</summary>
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetProductById(Guid id)
    {
        var result = await _mediator.Send(new GetProductByIdQuery(id));
        return result.Success ? Ok(result) : NotFound(result);
    }

    /// <summary>POST api/products - JWT gerektirir. Event fırlatılır.</summary>
    [HttpPost]
    [Authorize]
    public async Task<IActionResult> CreateProduct([FromBody] CreateProductCommand command)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "anonymous";
        var result = await _mediator.Send(command with { CreatedBy = userId });
        return result.Success ? CreatedAtAction(nameof(GetProductById), new { id = result.Data?.Id }, result) : BadRequest(result);
    }

    /// <summary>PUT api/products/{id} - JWT doğrulaması gerektirir (task requirement)</summary>
    [HttpPut("{id:guid}")]
    [Authorize]
    public async Task<IActionResult> UpdateProduct(Guid id, [FromBody] UpdateProductCommand command)
    {
        if (id != command.Id) return BadRequest(new { message = "ID uyuşmuyor" });
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "anonymous";
        var result = await _mediator.Send(command with { UpdatedBy = userId });
        return result.Success ? Ok(result) : NotFound(result);
    }
}
