using Khaikhong.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Khaikhong.Infrastructure.Persistence.Configurations;

public sealed class VariantOptionConfiguration : IEntityTypeConfiguration<VariantOption>
{
    public void Configure(EntityTypeBuilder<VariantOption> builder)
    {
        builder.ToTable("variant_option");

        builder.HasKey(option => option.Id);

        builder.Property(option => option.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(option => option.ProductId)
            .HasColumnName("product_id")
            .IsRequired();

        builder.Property(option => option.Name)
            .HasColumnName("name")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(option => option.IsActive)
            .HasColumnName("is_active")
            .HasDefaultValue(true);

        builder.HasIndex(option => option.ProductId)
            .HasDatabaseName("idx_variant_option_product_id");

        builder.HasOne(option => option.Product)
            .WithMany(product => product.Options)
            .HasForeignKey(option => option.ProductId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
