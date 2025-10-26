using System;
using System.Threading;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using EFCore.BulkExtensions;
using Khaikhong.Application.Contracts.Persistence.Repositories;
using Khaikhong.Domain.Entities;
using Khaikhong.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Khaikhong.Infrastructure.Persistence.Repositories;

public sealed class ProductRepository(KhaikhongDbContext context)
    : BaseRepository<Product>(context), IProductRepository
{
    private readonly KhaikhongDbContext _context = context;

    public async Task<(bool NameExists, bool SkuExists)> ExistsByNameOrSkuAsync(
        string name,
        string? sku,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        List<(string Name, string? Sku)> matches = await _context.Products
            .Where(product => product.IsActive
                              && (product.Name == name
                                  || (sku != null && product.Sku == sku)))
            .Select(product => new ValueTuple<string, string?>(product.Name, product.Sku))
            .ToListAsync(cancellationToken);

        bool nameExists = matches.Any(match => string.Equals(match.Name, name, StringComparison.Ordinal));
        bool skuExists = !string.IsNullOrWhiteSpace(sku)
                         && matches.Any(match => string.Equals(match.Item2, sku, StringComparison.Ordinal));

        return (nameExists, skuExists);
    }

    public async Task BulkInsertAsync(Product product, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(product);

        List<Product> products = new(1) { product };
        List<VariantOption> options = product.Options.ToList();
        List<VariantOptionValue> optionValues = options.SelectMany(option => option.Values).ToList();
        List<Variant> variants = product.Variants.ToList();
        List<ProductVariantCombination> combinations = variants.SelectMany(variant => variant.Combinations).ToList();

        BulkConfig bulkConfig = new()
        {
            PreserveInsertOrder = true,
            SetOutputIdentity = false,
            BatchSize = 2000
        };

        await _context.BulkInsertAsync(products, bulkConfig, cancellationToken: cancellationToken);

        if (options.Count > 0)
        {
            await _context.BulkInsertAsync(options, bulkConfig, cancellationToken: cancellationToken);
        }

        if (optionValues.Count > 0)
        {
            await _context.BulkInsertAsync(optionValues, bulkConfig, cancellationToken: cancellationToken);
        }

        if (variants.Count > 0)
        {
            await _context.BulkInsertAsync(variants, bulkConfig, cancellationToken: cancellationToken);
        }

        if (combinations.Count > 0)
        {
            await _context.BulkInsertAsync(combinations, bulkConfig, cancellationToken: cancellationToken);
        }
    }

    public async Task<IReadOnlyCollection<Product>> GetAllDetailedAsync(CancellationToken cancellationToken = default)
    {
        bool originalDetectChanges = _context.ChangeTracker.AutoDetectChangesEnabled;
        _context.ChangeTracker.AutoDetectChangesEnabled = false;

        try
        {
            await using var transaction = await _context.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted, cancellationToken);

            List<Product> products = await _context.Products
                .AsNoTracking()
                .Where(product => product.IsActive)
                .Include(product => product.Options.Where(option => option.IsActive))
                    .ThenInclude(option => option.Values.Where(value => value.IsActive))
                .Include(product => product.Variants.Where(variant => variant.IsActive))
                    .ThenInclude(variant => variant.Combinations.Where(combination => combination.IsActive))
                        .ThenInclude(combination => combination.OptionValue)
                            .ThenInclude(value => value.Option)
                .AsSplitQuery()
                .OrderBy(product => product.Name)
                .ToListAsync(cancellationToken);

            await transaction.CommitAsync(cancellationToken);

            return products;
        }
        finally
        {
            _context.ChangeTracker.AutoDetectChangesEnabled = originalDetectChanges;
        }
    }

    public async Task<Product?> GetDetailedByIdAsync(Guid productId, CancellationToken cancellationToken = default)
    {
        bool originalDetectChanges = _context.ChangeTracker.AutoDetectChangesEnabled;
        _context.ChangeTracker.AutoDetectChangesEnabled = false;

        try
        {
            await using var transaction = await _context.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted, cancellationToken);

            Product? product = await _context.Products
                .AsNoTracking()
                .Where(p => p.IsActive && p.Id == productId)
                .Include(p => p.Options.Where(option => option.IsActive))
                    .ThenInclude(option => option.Values.Where(value => value.IsActive))
                .Include(p => p.Variants.Where(variant => variant.IsActive))
                    .ThenInclude(variant => variant.Combinations.Where(combination => combination.IsActive))
                        .ThenInclude(combination => combination.OptionValue)
                            .ThenInclude(value => value.Option)
                .AsSplitQuery()
                .FirstOrDefaultAsync(cancellationToken);

            await transaction.CommitAsync(cancellationToken);

            return product;
        }
        finally
        {
            _context.ChangeTracker.AutoDetectChangesEnabled = originalDetectChanges;
        }
    }

    public async Task<Product?> GetDetailedByIdTrackingAsync(Guid productId, CancellationToken cancellationToken = default)
    {
        await using var transaction = await _context.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted, cancellationToken);

        Product? product = await _context.Products
            .Where(p => p.Id == productId && p.IsActive)
            .Include(p => p.Options)
                .ThenInclude(option => option.Values)
            .Include(p => p.Variants)
                .ThenInclude(variant => variant.Combinations)
                    .ThenInclude(combination => combination.OptionValue)
                        .ThenInclude(value => value.Option)
            .AsSplitQuery()
            .FirstOrDefaultAsync(cancellationToken);

        await transaction.CommitAsync(cancellationToken);

        return product;
    }
}
