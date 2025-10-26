using Khaikhong.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Khaikhong.Infrastructure.Persistence.Configurations;

public sealed class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.ToTable("product_master");

        builder.HasKey(product => product.Id);

        builder.Property(product => product.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(product => product.Name)
            .HasColumnName("name")
            .HasMaxLength(255)
            .IsRequired();

        builder.Property(product => product.Description)
            .HasColumnName("description")
            .HasColumnType("text");

        builder.Property(product => product.BasePrice)
            .HasColumnName("base_price")
            .HasPrecision(12, 2)
            .HasDefaultValue(0m)
            .IsRequired();

        builder.Property(product => product.Sku)
            .HasColumnName("sku")
            .HasMaxLength(100);

        builder.Property(product => product.BaseStock)
            .HasColumnName("base_stock");

        builder.HasIndex(product => product.Sku)
            .IsUnique();

        builder.Property(product => product.CreatedBy)
            .HasColumnName("created_by");

        builder.Property(product => product.UpdatedBy)
            .HasColumnName("updated_by");

        builder.Property(product => product.CreatedAt)
            .HasColumnName("created_at")
            .HasColumnType("datetime(6)")
            .HasDefaultValueSql("CURRENT_TIMESTAMP(6)");

        builder.Property(product => product.UpdatedAt)
            .HasColumnName("updated_at")
            .HasColumnType("datetime(6)")
            .HasDefaultValueSql("CURRENT_TIMESTAMP(6)")
            .ValueGeneratedOnAddOrUpdate();

        builder.Property(product => product.IsActive)
            .HasColumnName("is_active")
            .HasDefaultValue(true);

        builder.HasIndex(product => product.IsActive)
            .HasDatabaseName("idx_product_master_is_active");
    }
}
