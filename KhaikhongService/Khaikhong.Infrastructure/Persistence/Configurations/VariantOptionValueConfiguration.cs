using Khaikhong.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Khaikhong.Infrastructure.Persistence.Configurations;

public sealed class VariantOptionValueConfiguration : IEntityTypeConfiguration<VariantOptionValue>
{
    public void Configure(EntityTypeBuilder<VariantOptionValue> builder)
    {
        builder.ToTable("variant_option_value");

        builder.HasKey(value => value.Id);

        builder.Property(value => value.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(value => value.OptionId)
            .HasColumnName("option_id")
            .IsRequired();

        builder.Property(value => value.Value)
            .HasColumnName("value")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(value => value.IsActive)
            .HasColumnName("is_active")
            .HasDefaultValue(true);

        builder.HasIndex(value => value.OptionId)
            .HasDatabaseName("idx_variant_option_value_option_id");

        builder.HasIndex(value => value.Value)
            .HasDatabaseName("idx_variant_option_value_value");

        builder.HasOne(value => value.Option)
            .WithMany(option => option.Values)
            .HasForeignKey(value => value.OptionId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
