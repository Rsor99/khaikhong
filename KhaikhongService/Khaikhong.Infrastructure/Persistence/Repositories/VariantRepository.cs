using System;
using System.Threading;
using System.Threading.Tasks;
using Khaikhong.Application.Contracts.Persistence.Repositories;
using Khaikhong.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Khaikhong.Infrastructure.Persistence.Repositories;

public sealed class VariantRepository(KhaikhongDbContext context) : BaseRepository<Variant>(context), IVariantRepository
{
    public async Task<Variant?> GetByIdTrackingAsync(Guid variantId, CancellationToken cancellationToken = default)
    {
        return await Context.Set<Variant>()
            .Include(variant => variant.Product)
            .FirstOrDefaultAsync(variant => variant.Id == variantId && variant.IsActive, cancellationToken);
    }
}
