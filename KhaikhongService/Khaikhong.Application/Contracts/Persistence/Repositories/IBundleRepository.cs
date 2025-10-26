using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Khaikhong.Domain.Entities;

namespace Khaikhong.Application.Contracts.Persistence.Repositories;

public interface IBundleRepository
{
    Task AddAsync(Bundle bundle, CancellationToken cancellationToken = default);

    Task BulkInsertItemsAsync(IEnumerable<BundleItem> items, CancellationToken cancellationToken = default);

    Task<bool> IsProductLinkedAsync(Guid productId, CancellationToken cancellationToken = default);

    Task<List<Bundle>> GetAllDetailedAsync(CancellationToken cancellationToken = default);

    Task<Bundle?> GetDetailedByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<Bundle?> GetDetailedByIdTrackingAsync(Guid id, CancellationToken cancellationToken = default);

    Task<Bundle?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
}
