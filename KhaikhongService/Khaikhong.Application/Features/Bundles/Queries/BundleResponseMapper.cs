using System;
using System.Collections.Generic;
using System.Linq;
using Khaikhong.Application.Features.Bundles.Dtos;
using Khaikhong.Domain.Entities;

namespace Khaikhong.Application.Features.Bundles.Queries;

internal static class BundleResponseMapper
{
    public static BundleResponseDto Map(Bundle bundle)
    {
        List<BundleResponseProductDto> products = bundle.Items
            .Where(item => item.IsActive && item.ProductId.HasValue)
            .GroupBy(item => item.ProductId!.Value)
            .Select(MapProduct)
            .OrderBy(product => product.Name)
            .ToList();

        return new BundleResponseDto
        {
            Id = bundle.Id,
            Name = bundle.Name,
            Description = bundle.Description,
            Price = bundle.Price,
            Products = products
        };
    }

    private static BundleResponseProductDto MapProduct(IGrouping<Guid, BundleItem> group)
    {
        BundleItem sample = group.First();
        Product? product = sample.Product ?? sample.Variant?.Product;
        string productName = product?.Name ?? string.Empty;

        int baseQuantity = group
            .Where(item => !item.VariantId.HasValue)
            .Sum(item => item.Quantity);

        List<BundleResponseVariantDto>? variants = group
            .Where(item => item.VariantId.HasValue)
            .GroupBy(item => item.VariantId!.Value)
            .Select(MapVariant)
            .OrderBy(variant => variant.Sku ?? string.Empty)
            .ToList();

        if (variants.Count == 0)
        {
            variants = null;
        }

        int? quantity = baseQuantity > 0 ? baseQuantity : (int?)null;

        return new BundleResponseProductDto
        {
            ProductId = group.Key,
            Name = productName,
            Quantity = quantity,
            Variants = variants
        };
    }

    private static BundleResponseVariantDto MapVariant(IGrouping<Guid, BundleItem> group)
    {
        BundleItem sample = group.First();
        Variant? variant = sample.Variant;
        if (variant is null)
        {
            throw new InvalidOperationException("Bundle item variant data is missing.");
        }

        int quantity = group.Sum(item => item.Quantity);

        return new BundleResponseVariantDto
        {
            VariantId = group.Key,
            Sku = variant.Sku,
            Quantity = quantity
        };
    }
}
