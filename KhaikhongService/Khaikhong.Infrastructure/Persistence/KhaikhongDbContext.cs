using Khaikhong.Domain.Entities;
using Khaikhong.Infrastructure.Persistence.Configurations;
using Microsoft.EntityFrameworkCore;

namespace Khaikhong.Infrastructure.Persistence
{
    public class KhaikhongDbContext(DbContextOptions<KhaikhongDbContext> options) : DbContext(options)
    {
        public DbSet<Product> Products => Set<Product>();

        public DbSet<Variant> Variants => Set<Variant>();

        public DbSet<VariantOption> VariantOptions => Set<VariantOption>();

        public DbSet<VariantOptionValue> VariantOptionValues => Set<VariantOptionValue>();

        public DbSet<ProductVariantCombination> ProductVariantCombinations => Set<ProductVariantCombination>();

        public DbSet<Bundle> Bundles => Set<Bundle>();

        public DbSet<BundleItem> BundleItems => Set<BundleItem>();

        public DbSet<Order> Orders => Set<Order>();

        public DbSet<OrderItem> OrderItems => Set<OrderItem>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            
            modelBuilder.Entity<User>()
                .ToTable("users")
                .Metadata.SetIsTableExcludedFromMigrations(true);

            modelBuilder.ApplyConfiguration(new ProductConfiguration());
            modelBuilder.ApplyConfiguration(new VariantConfiguration());
            modelBuilder.ApplyConfiguration(new VariantOptionConfiguration());
            modelBuilder.ApplyConfiguration(new VariantOptionValueConfiguration());
            modelBuilder.ApplyConfiguration(new ProductVariantCombinationConfiguration());
            modelBuilder.ApplyConfiguration(new BundleConfiguration());
            modelBuilder.ApplyConfiguration(new BundleItemConfiguration());
            modelBuilder.ApplyConfiguration(new OrderConfiguration());
            modelBuilder.ApplyConfiguration(new OrderItemConfiguration());
        }
    }
}
