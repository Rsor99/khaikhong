using Khaikhong.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Khaikhong.Infrastructure.Persistence.Configurations;

public sealed class VariantConfiguration : IEntityTypeConfiguration<Variant>
{
    public void Configure(EntityTypeBuilder<Variant> builder)
    {
        builder.ToTable("product_variant");

        builder.HasKey(variant => variant.Id);

        builder.Property(variant => variant.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(variant => variant.ProductId)
            .HasColumnName("product_id")
            .IsRequired();

        builder.Property(variant => variant.Sku)
            .HasColumnName("sku")
            .HasMaxLength(100);

        builder.HasIndex(variant => variant.Sku)
            .IsUnique();

        builder.Property(variant => variant.Price)
            .HasColumnName("price")
            .HasPrecision(12, 2)
            .IsRequired();

        builder.Property(variant => variant.Stock)
            .HasColumnName("stock")
            .HasDefaultValue(0)
            .IsRequired();

        builder.Property(variant => variant.IsActive)
            .HasColumnName("is_active")
            .HasDefaultValue(true);

        builder.Property(variant => variant.CreatedBy)
            .HasColumnName("created_by");

        builder.Property(variant => variant.UpdatedBy)
            .HasColumnName("updated_by");

        builder.Property(variant => variant.CreatedAt)
            .HasColumnName("created_at")
            .HasColumnType("datetime(6)")
            .HasDefaultValueSql("CURRENT_TIMESTAMP(6)");

        builder.Property(variant => variant.UpdatedAt)
            .HasColumnName("updated_at")
            .HasColumnType("datetime(6)")
            .HasDefaultValueSql("CURRENT_TIMESTAMP(6)")
            .ValueGeneratedOnAddOrUpdate();

        builder.Property(variant => variant.IsActive)
            .HasColumnName("is_active")
            .HasDefaultValue(true);

        builder.HasIndex(variant => variant.ProductId)
            .HasDatabaseName("idx_product_variant_product_id");

        builder.HasIndex(variant => variant.IsActive)
            .HasDatabaseName("idx_product_variant_is_active");

        builder.HasIndex(variant => variant.Stock)
            .HasDatabaseName("idx_product_variant_stock");

        builder.HasOne(variant => variant.Product)
            .WithMany(product => product.Variants)
            .HasForeignKey(variant => variant.ProductId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
