using System;
using System.Collections.Generic;
using System.Reflection;
using Khaikhong.Domain.Entities;

namespace Khaikhong.Tests.Features.Bundles.Queries;

internal static class BundleQueryTestData
{
    public static readonly Guid ShirtProductId = Guid.Parse("019a3662-aaaa-4f80-929d-742201aa1111");
    public static readonly Guid MugProductId = Guid.Parse("019a3662-bbbb-4f80-929d-742201aa2222");
    public static readonly Guid MugVariantId = Guid.Parse("019a3662-cccc-4f80-929d-742201aa3333");

    public static Bundle BuildBundleAggregate()
    {
        Bundle bundle = Bundle.Create("Eco Starter Kit", 1290m, "Bundle for eco-conscious beginners");

        Product shirt = Product.Create("Eco T-Shirt", 490m);
        Product mug = Product.Create("Eco Mug", 350m);

        Variant mugVariant = Variant.Create(mug.Id, 350m, 10, "MUG-001");
        FieldInfo? variantProductField = typeof(Variant).GetField("<Product>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic);
        variantProductField?.SetValue(mugVariant, mug);

        BundleItem shirtItem = BundleItem.Create(bundle.Id, 2, ShirtProductId);
        AssignNavigation(shirtItem, bundle, shirt, null);

        BundleItem mugVariantItem = BundleItem.Create(bundle.Id, 1, MugProductId, MugVariantId);
        AssignNavigation(mugVariantItem, bundle, mug, mugVariant);

        bundle.AddItems(new[] { shirtItem, mugVariantItem });

        return bundle;
    }

    private static void AssignNavigation(BundleItem item, Bundle bundle, Product product, Variant? variant)
    {
        FieldInfo? bundleField = typeof(BundleItem).GetField("<Bundle>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic);
        bundleField?.SetValue(item, bundle);

        FieldInfo? productField = typeof(BundleItem).GetField("<Product>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic);
        productField?.SetValue(item, product);

        FieldInfo? variantField = typeof(BundleItem).GetField("<Variant>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic);
        variantField?.SetValue(item, variant);
    }
}
