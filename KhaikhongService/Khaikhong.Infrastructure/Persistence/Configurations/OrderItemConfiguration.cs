using Khaikhong.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Khaikhong.Infrastructure.Persistence.Configurations;

public sealed class OrderItemConfiguration : IEntityTypeConfiguration<OrderItem>
{
    public void Configure(EntityTypeBuilder<OrderItem> builder)
    {
        builder.ToTable("order_item");

        builder.HasKey(item => item.Id);

        builder.Property(item => item.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(item => item.OrderId)
            .HasColumnName("order_id")
            .IsRequired();

        builder.Property(item => item.ProductId)
            .HasColumnName("product_id");

        builder.Property(item => item.VariantId)
            .HasColumnName("variant_id");

        builder.Property(item => item.BundleId)
            .HasColumnName("bundle_id");

        builder.Property(item => item.Quantity)
            .HasColumnName("quantity")
            .HasDefaultValue(1)
            .IsRequired();

        builder.HasIndex(item => item.OrderId)
            .HasDatabaseName("idx_order_item_order_id");

        builder.HasIndex(item => new { item.ProductId, item.VariantId, item.BundleId })
            .HasDatabaseName("idx_order_item_product_variant");

        builder.HasOne(item => item.Order)
            .WithMany(order => order.Items)
            .HasForeignKey(item => item.OrderId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(item => item.Product)
            .WithMany(product => product.OrderItems)
            .HasForeignKey(item => item.ProductId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(item => item.Variant)
            .WithMany(variant => variant.OrderItems)
            .HasForeignKey(item => item.VariantId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(item => item.Bundle)
            .WithMany()
            .HasForeignKey(item => item.BundleId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
