using ProductService.Domain.Entities;

namespace ProductService.Domain.Interfaces;

/// <summary>
/// Ürün repository soyutlaması (Dependency Inversion Principle).
/// </summary>
public interface IProductRepository
{
    Task<Product?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<Product>> GetAllAsync(CancellationToken ct = default);
    Task<Product> AddAsync(Product product, CancellationToken ct = default);
    Task UpdateAsync(Product product, CancellationToken ct = default);
    Task DeleteAsync(Guid id, CancellationToken ct = default);
    Task<(IReadOnlyList<Product> Items, int TotalCount)> GetPagedAsync(int pageNumber, int pageSize, string? category = null, CancellationToken ct = default);
}
