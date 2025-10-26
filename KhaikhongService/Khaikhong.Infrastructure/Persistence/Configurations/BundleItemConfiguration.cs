using Khaikhong.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Khaikhong.Infrastructure.Persistence.Configurations;

public sealed class BundleItemConfiguration : IEntityTypeConfiguration<BundleItem>
{
    public void Configure(EntityTypeBuilder<BundleItem> builder)
    {
        builder.ToTable("bundle_item");

        builder.HasKey(item => item.Id);

        builder.Property(item => item.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(item => item.BundleId)
            .HasColumnName("bundle_id")
            .IsRequired();

        builder.Property(item => item.ProductId)
            .HasColumnName("product_id");

        builder.Property(item => item.VariantId)
            .HasColumnName("variant_id");

        builder.Property(item => item.Quantity)
            .HasColumnName("quantity")
            .HasDefaultValue(1)
            .IsRequired();

        builder.HasIndex(item => item.BundleId)
            .HasDatabaseName("idx_bundle_item_bundle_id");

        builder.HasIndex(item => new { item.ProductId, item.VariantId })
            .HasDatabaseName("idx_bundle_item_product_variant");

        builder.HasOne(item => item.Bundle)
            .WithMany(bundle => bundle.Items)
            .HasForeignKey(item => item.BundleId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(item => item.Product)
            .WithMany(product => product.BundleItems)
            .HasForeignKey(item => item.ProductId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(item => item.Variant)
            .WithMany(variant => variant.BundleItems)
            .HasForeignKey(item => item.VariantId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
