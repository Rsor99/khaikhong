using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EFCore.BulkExtensions;
using Khaikhong.Application.Contracts.Persistence.Repositories;
using Khaikhong.Domain.Entities;
using Khaikhong.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using MySqlConnector;

namespace Khaikhong.Infrastructure.Persistence.Repositories;

public sealed class BundleRepository(KhaikhongDbContext context) : IBundleRepository
{
    private readonly KhaikhongDbContext _context = context;

    public Task AddAsync(Bundle bundle, CancellationToken cancellationToken = default) =>
        _context.Bundles.AddAsync(bundle, cancellationToken).AsTask();

    public async Task BulkInsertItemsAsync(IEnumerable<BundleItem> items, CancellationToken cancellationToken = default)
    {
        List<BundleItem> bundleItems = items switch
        {
            List<BundleItem> list => list,
            _ => new List<BundleItem>(items)
        };

        if (bundleItems.Count == 0)
        {
            return;
        }

        var bulkConfig = new BulkConfig
        {
            PreserveInsertOrder = true,
            SetOutputIdentity = false,
            BatchSize = 2000,
            PropertiesToInclude = new List<string>
            {
                nameof(BundleItem.Id),
                nameof(BundleItem.BundleId),
                nameof(BundleItem.ProductId),
                nameof(BundleItem.VariantId),
                nameof(BundleItem.Quantity),
                nameof(BundleItem.IsActive)
            },
            UseTempDB = false,
            CalculateStats = true
        };

        try
        {
            if (_context.ChangeTracker.HasChanges())
            {
                await _context.SaveChangesAsync(cancellationToken);
            }

            await _context.BulkInsertAsync(bundleItems, bulkConfig, cancellationToken: cancellationToken);
        }
        catch (MySqlException exception) when (IsBulkStatsMismatch(exception))
        {
            await _context.BundleItems.AddRangeAsync(bundleItems, cancellationToken);
        }
    }

    public async Task<bool> IsProductLinkedAsync(Guid productId, CancellationToken cancellationToken = default)
    {
        return await _context.BundleItems
            .AsNoTracking()
            .AnyAsync(item => item.ProductId == productId && item.Bundle.IsActive, cancellationToken);
    }

    public async Task<List<Bundle>> GetAllDetailedAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Bundles
            .AsNoTracking()
            .Where(bundle => bundle.IsActive)
            .Include(bundle => bundle.Items.Where(item => item.IsActive))
                .ThenInclude(item => item.Product)
            .Include(bundle => bundle.Items.Where(item => item.IsActive))
                .ThenInclude(item => item.Variant)
            .OrderBy(bundle => bundle.Name)
            .AsSplitQuery()
            .ToListAsync(cancellationToken);
    }

    public async Task<Bundle?> GetDetailedByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Bundles
            .AsNoTracking()
            .Where(bundle => bundle.IsActive && bundle.Id == id)
            .Include(bundle => bundle.Items.Where(item => item.IsActive))
                .ThenInclude(item => item.Product)
            .Include(bundle => bundle.Items.Where(item => item.IsActive))
                .ThenInclude(item => item.Variant)
            .AsSplitQuery()
            .FirstOrDefaultAsync(cancellationToken);
    }

    private static bool IsBulkStatsMismatch(MySqlException exception) =>
        exception.Message.Contains("were copied", StringComparison.OrdinalIgnoreCase)
        && exception.Message.Contains("were inserted", StringComparison.OrdinalIgnoreCase);
}
