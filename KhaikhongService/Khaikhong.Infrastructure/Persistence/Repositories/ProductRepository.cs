using System.Threading;
using System.Collections.Generic;
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
}
