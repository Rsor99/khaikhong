using System;
using System.Threading;
using System.Threading.Tasks;
using Khaikhong.Domain.Entities;

namespace Khaikhong.Application.Contracts.Persistence.Repositories;

public interface IVariantRepository : IRepository<Variant>
{
    Task<Variant?> GetByIdTrackingAsync(Guid variantId, CancellationToken cancellationToken = default);
}
