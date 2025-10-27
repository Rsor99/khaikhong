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
        IEnumerable<IGrouping<Guid, BundleItem>> groupedItems = bundle.Items
            .Where(item => item.IsActive && item.ProductId.HasValue)
            .GroupBy(item => item.ProductId!.Value);

        List<BundleResponseProductDto> products = new();
        int? availableBundles = null;
        bool availabilityUnknown = false;
        decimal totalComponentCost = 0m;
        bool costUnknown = false;

        foreach (IGrouping<Guid, BundleItem> group in groupedItems)
        {
            BundleResponseProductDto productDto = ProcessProductGroup(
                group,
                ref availableBundles,
                ref availabilityUnknown,
                ref totalComponentCost,
                ref costUnknown);

            products.Add(productDto);
        }

        if (availabilityUnknown)
        {
            availableBundles = null;
        }

        decimal? savings = costUnknown ? null : totalComponentCost - bundle.Price;

        return new BundleResponseDto
        {
            Id = bundle.Id,
            Name = bundle.Name,
            Description = bundle.Description,
            Price = bundle.Price,
            AvailableBundles = availableBundles,
            Savings = savings,
            Products = products
                .OrderBy(product => product.Name, StringComparer.OrdinalIgnoreCase)
                .ToList()
        };
    }

    private static BundleResponseProductDto ProcessProductGroup(
        IGrouping<Guid, BundleItem> group,
        ref int? availableBundles,
        ref bool availabilityUnknown,
        ref decimal totalComponentCost,
        ref bool costUnknown)
    {
        BundleItem sample = group.First();
        Product? product = sample.Product ?? sample.Variant?.Product;
        string productName = product?.Name ?? string.Empty;

        int baseQuantity = group
            .Where(item => !item.VariantId.HasValue)
            .Sum(item => item.Quantity);

        if (baseQuantity > 0)
        {
            if (product is null)
            {
                availabilityUnknown = true;
                costUnknown = true;
            }
            else
            {
                totalComponentCost += product.BasePrice * baseQuantity;

                if (product.BaseStock.HasValue)
                {
                    int potential = product.BaseStock.Value / baseQuantity;
                    availableBundles = availableBundles.HasValue
                        ? Math.Min(availableBundles.Value, potential)
                        : potential;
                }
                else
                {
                    availabilityUnknown = true;
                }
            }
        }

        List<BundleResponseVariantDto> variantDtos = new();

        foreach (IGrouping<Guid, BundleItem> variantGroup in group
                     .Where(item => item.VariantId.HasValue)
                     .GroupBy(item => item.VariantId!.Value))
        {
            BundleResponseVariantDto variantDto = ProcessVariantGroup(
                variantGroup,
                ref availableBundles,
                ref totalComponentCost);

            variantDtos.Add(variantDto);
        }

        IReadOnlyCollection<BundleResponseVariantDto>? variants = variantDtos.Count > 0
            ? variantDtos
                .OrderBy(variant => variant.Sku ?? string.Empty, StringComparer.OrdinalIgnoreCase)
                .ToList()
            : null;

        return new BundleResponseProductDto
        {
            ProductId = group.Key,
            Name = productName,
            Quantity = baseQuantity > 0 ? baseQuantity : (int?)null,
            Variants = variants
        };
    }

    private static BundleResponseVariantDto ProcessVariantGroup(
        IGrouping<Guid, BundleItem> group,
        ref int? availableBundles,
        ref decimal totalComponentCost)
    {
        BundleItem sample = group.First();
        Variant? variant = sample.Variant;
        if (variant is null)
        {
            throw new InvalidOperationException("Bundle item variant data is missing.");
        }

        int quantity = group.Sum(item => item.Quantity);

        totalComponentCost += variant.Price * quantity;

        if (quantity > 0)
        {
            int potential = variant.Stock / quantity;
            availableBundles = availableBundles.HasValue
                ? Math.Min(availableBundles.Value, potential)
                : potential;
        }

        return new BundleResponseVariantDto
        {
            VariantId = group.Key,
            Sku = variant.Sku,
            Quantity = quantity
        };
    }
}
