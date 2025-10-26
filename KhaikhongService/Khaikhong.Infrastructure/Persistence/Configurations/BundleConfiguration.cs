using Khaikhong.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Khaikhong.Infrastructure.Persistence.Configurations;

public sealed class BundleConfiguration : IEntityTypeConfiguration<Bundle>
{
    public void Configure(EntityTypeBuilder<Bundle> builder)
    {
        builder.ToTable("bundle");

        builder.HasKey(bundle => bundle.Id);

        builder.Property(bundle => bundle.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(bundle => bundle.Name)
            .HasColumnName("name")
            .HasMaxLength(255)
            .IsRequired();

        builder.Property(bundle => bundle.Description)
            .HasColumnName("description")
            .HasColumnType("text");

        builder.Property(bundle => bundle.Price)
            .HasColumnName("price")
            .HasPrecision(12, 2)
            .IsRequired();

        builder.Property(bundle => bundle.CreatedBy)
            .HasColumnName("created_by");

        builder.Property(bundle => bundle.UpdatedBy)
            .HasColumnName("updated_by");

        builder.Property(bundle => bundle.CreatedAt)
            .HasColumnName("created_at")
            .HasColumnType("datetime(6)")
            .HasDefaultValueSql("CURRENT_TIMESTAMP(6)");

        builder.Property(bundle => bundle.UpdatedAt)
            .HasColumnName("updated_at")
            .HasColumnType("datetime(6)")
            .HasDefaultValueSql("CURRENT_TIMESTAMP(6)")
            .ValueGeneratedOnAddOrUpdate();

        builder.Property(bundle => bundle.IsActive)
            .HasColumnName("is_active")
            .HasDefaultValue(true);

        builder.HasIndex(bundle => bundle.IsActive)
            .HasDatabaseName("idx_bundle_is_active");
    }
}
