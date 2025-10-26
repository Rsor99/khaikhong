using Khaikhong.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Khaikhong.Infrastructure.Persistence.Configurations;

public sealed class ProductVariantCombinationConfiguration : IEntityTypeConfiguration<ProductVariantCombination>
{
    public void Configure(EntityTypeBuilder<ProductVariantCombination> builder)
    {
        builder.ToTable("product_variant_combination");

        builder.HasKey(combination => combination.Id);

        builder.Property(combination => combination.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(combination => combination.VariantId)
            .HasColumnName("variant_id")
            .IsRequired();

        builder.Property(combination => combination.OptionValueId)
            .HasColumnName("option_value_id")
            .IsRequired();

        builder.HasIndex(combination => combination.VariantId)
            .HasDatabaseName("idx_variant_combination_variant_id");

        builder.HasIndex(combination => combination.OptionValueId)
            .HasDatabaseName("idx_variant_combination_option_value_id");

        builder.HasIndex(combination => new { combination.VariantId, combination.OptionValueId })
            .IsUnique()
            .HasDatabaseName("uq_variant_option_value");

        builder.HasOne(combination => combination.Variant)
            .WithMany(variant => variant.Combinations)
            .HasForeignKey(combination => combination.VariantId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(combination => combination.OptionValue)
            .WithMany(value => value.Combinations)
            .HasForeignKey(combination => combination.OptionValueId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
