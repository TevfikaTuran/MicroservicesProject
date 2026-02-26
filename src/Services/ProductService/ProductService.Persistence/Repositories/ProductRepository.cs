using Microsoft.EntityFrameworkCore;
using ProductService.Domain.Entities;
using ProductService.Domain.Interfaces;
using ProductService.Persistence.Context;

namespace ProductService.Persistence.Repositories;

/// <summary>
/// IProductRepository EF Core implementasyonu. Liskov Substitution: Farklı ORM ile değiştirilebilir.
/// </summary>
public class ProductRepository : IProductRepository
{
    private readonly ProductDbContext _context;
    public ProductRepository(ProductDbContext context) => _context = context;

    public async Task<Product?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await _context.Products.AsNoTracking().FirstOrDefaultAsync(p => p.Id == id, ct);

    public async Task<IReadOnlyList<Product>> GetAllAsync(CancellationToken ct = default)
        => await _context.Products.AsNoTracking().Where(p => p.IsActive).OrderByDescending(p => p.CreatedAt).ToListAsync(ct);

    public async Task<Product> AddAsync(Product product, CancellationToken ct = default)
    {
        await _context.Products.AddAsync(product, ct);
        await _context.SaveChangesAsync(ct);
        return product;
    }

    public async Task UpdateAsync(Product product, CancellationToken ct = default)
    {
        _context.Products.Update(product);
        await _context.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var product = await _context.Products.FindAsync(new object[] { id }, ct);
        if (product != null) { product.IsActive = false; await _context.SaveChangesAsync(ct); }
    }

    public async Task<(IReadOnlyList<Product> Items, int TotalCount)> GetPagedAsync(
        int pageNumber, int pageSize, string? category = null, CancellationToken ct = default)
    {
        var query = _context.Products.AsNoTracking().Where(p => p.IsActive);
        if (!string.IsNullOrEmpty(category))
            query = query.Where(p => p.Category == category);
        query = query.OrderByDescending(p => p.CreatedAt);

        var totalCount = await query.CountAsync(ct);
        var items = await query.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToListAsync(ct);
        return (items, totalCount);
    }
}
