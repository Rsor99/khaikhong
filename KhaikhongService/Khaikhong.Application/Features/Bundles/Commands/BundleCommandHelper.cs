using System;
using System.Collections.Generic;
using System.Linq;
using Khaikhong.Application.Features.Bundles.Dtos;
using Khaikhong.Domain.Entities;

namespace Khaikhong.Application.Features.Bundles.Commands;

internal static class BundleCommandHelper
{
    public static IList<BundleItem> BuildBundleItems(Guid bundleId, IReadOnlyCollection<CreateBundleProductDto> products)
    {
        List<BundleItem> items = new();

        foreach (CreateBundleProductDto dto in products)
        {
            if (dto.Variants is not null && dto.Variants.Count > 0)
            {
                foreach (CreateBundleVariantDto variant in dto.Variants)
                {
                    items.Add(BundleItem.Create(bundleId, variant.Quantity, dto.ProductId, variant.VariantId));
                }

                continue;
            }

            items.Add(BundleItem.Create(bundleId, dto.Quantity!.Value, dto.ProductId));
        }

        return items;
    }

    public static List<object> CollectVariantErrors(
        IReadOnlyCollection<CreateBundleProductDto> products,
        IReadOnlyDictionary<Guid, IReadOnlyCollection<Guid>> variantLookup)
    {
        List<object> errors = new();

        foreach (CreateBundleProductDto dto in products)
        {
            if (dto.Variants is null || dto.Variants.Count == 0)
            {
                continue;
            }

            if (!variantLookup.TryGetValue(dto.ProductId, out IReadOnlyCollection<Guid>? validVariants))
            {
                foreach (CreateBundleVariantDto variant in dto.Variants)
                {
                    errors.Add(new
                    {
                        field = "request.products",
                        error = $"Variant {variant.VariantId} is not active for product {dto.ProductId}."
                    });
                }

                continue;
            }

            HashSet<Guid> variantSet = validVariants is HashSet<Guid> hash
                ? hash
                : new HashSet<Guid>(validVariants);

            foreach (CreateBundleVariantDto variant in dto.Variants)
            {
                if (!variantSet.Contains(variant.VariantId))
                {
                    errors.Add(new
                    {
                        field = "request.products",
                        error = $"Variant {variant.VariantId} is not active for product {dto.ProductId}."
                    });
                }
            }
        }

        return errors;
    }

    public static List<object> CollectQuantityErrors(IReadOnlyCollection<CreateBundleProductDto> products)
    {
        List<object> errors = new();

        foreach (CreateBundleProductDto dto in products)
        {
            if (dto.Variants is null || dto.Variants.Count == 0)
            {
                if (!dto.Quantity.HasValue || dto.Quantity.Value <= 0)
                {
                    errors.Add(new
                    {
                        field = "request.products",
                        error = $"Quantity must be greater than zero for product {dto.ProductId} when no variants are specified."
                    });
                }

                continue;
            }

            foreach (CreateBundleVariantDto variant in dto.Variants)
            {
                if (variant.Quantity <= 0)
                {
                    errors.Add(new
                    {
                        field = "request.products",
                        error = $"Variant {variant.VariantId} must have a quantity greater than zero for product {dto.ProductId}."
                    });
                }
            }
        }

        return errors;
    }
}
