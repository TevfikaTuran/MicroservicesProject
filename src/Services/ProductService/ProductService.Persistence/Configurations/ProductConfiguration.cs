using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProductService.Domain.Entities;

namespace ProductService.Persistence.Configurations;

public class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Name).IsRequired().HasMaxLength(200);
        builder.Property(p => p.Description).HasMaxLength(1000);
        builder.Property(p => p.Price).HasPrecision(18, 2);
        builder.Property(p => p.Category).IsRequired().HasMaxLength(100);
        builder.Property(p => p.CreatedBy).HasMaxLength(256);
        builder.HasIndex(p => p.Category);
        builder.HasIndex(p => p.IsActive);
    }
}
