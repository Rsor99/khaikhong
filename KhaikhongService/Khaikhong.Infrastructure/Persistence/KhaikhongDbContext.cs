
using Microsoft.EntityFrameworkCore;

namespace Khaikhong.Infrastructure.Persistence
{
    public class KhaikhongDbContext(DbContextOptions<KhaikhongDbContext> options) : DbContext(options)
    {
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(KhaikhongDbContext).Assembly);
        }
    }
}