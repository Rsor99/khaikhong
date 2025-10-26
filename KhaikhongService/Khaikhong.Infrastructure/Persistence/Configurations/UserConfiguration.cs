using Khaikhong.Domain.Entities;
using Khaikhong.Domain.Enums;
using Khaikhong.Domain.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Khaikhong.Infrastructure.Persistence.Configurations
{
    public class UserConfiguration : IEntityTypeConfiguration<User>
    {
        public void Configure(EntityTypeBuilder<User> builder)
        {
            builder.ToTable("users");

            //Id Primary key uuid v7
            builder.HasKey(u => u.Id);
            builder.Property(u => u.Id)
                .ValueGeneratedNever();

            //Email Unique & Index
            builder.Property(u => u.Email)
                .IsRequired()
                .HasMaxLength(255);
            builder.HasIndex(u => u.Email)
                .IsUnique();

            builder.Property(u => u.PasswordHash)
                .HasColumnName("password_hash")
                .IsRequired()
                .HasMaxLength(255);

            builder.Property(u => u.FirstName)
                .HasColumnName("first_name")
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(u => u.LastName)
                .HasColumnName("last_name")
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(u => u.Role)
                .HasDefaultValue(UserRole.USER)
                .HasSentinel(UserRole.UNKNOWN)
                .HasConversion(
                    v => v.GetDescription(),
                    v => v.ToEnumFromDescription<UserRole>()
                )
                .HasMaxLength(50)
                .HasDefaultValue(UserRole.USER);

            builder.Property(u => u.CreatedAt)
                .HasColumnName("created_at")
                .HasColumnType("datetime(6)")
                .HasDefaultValueSql("CURRENT_TIMESTAMP(6)");

            builder.Property(u => u.UpdatedAt)
                .HasColumnName("updated_at")
                .HasColumnType("datetime(6)")
                .HasDefaultValueSql("CURRENT_TIMESTAMP(6)")
                .ValueGeneratedOnAddOrUpdate();

            builder.Property(u => u.IsActive)
                .HasColumnName("is_active")
                .HasDefaultValue(true);
        }
    }
}
