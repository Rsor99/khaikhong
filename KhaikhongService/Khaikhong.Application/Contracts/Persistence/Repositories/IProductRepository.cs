using System;
using System.Threading;
using System.Threading.Tasks;
using Khaikhong.Domain.Entities;

namespace Khaikhong.Application.Contracts.Persistence.Repositories;

public interface IProductRepository : IRepository<Product>
{
    Task<(bool NameExists, bool SkuExists)> ExistsByNameOrSkuAsync(
        string name,
        string? sku,
        CancellationToken cancellationToken = default);

    Task BulkInsertAsync(Product product, CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<Product>> GetAllDetailedAsync(CancellationToken cancellationToken = default);

    Task<Product?> GetDetailedByIdAsync(Guid productId, CancellationToken cancellationToken = default);

    Task<Product?> GetDetailedByIdTrackingAsync(Guid productId, CancellationToken cancellationToken = default);
}
