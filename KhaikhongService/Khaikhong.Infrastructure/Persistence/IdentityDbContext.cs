using Khaikhong.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Khaikhong.Infrastructure.Persistence
{
    public class IdentityDbContext(DbContextOptions<IdentityDbContext> options) : DbContext(options)
    {
        public DbSet<User> Users => Set<User>();
        public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(IdentityDbContext).Assembly);
        }
    }
}
