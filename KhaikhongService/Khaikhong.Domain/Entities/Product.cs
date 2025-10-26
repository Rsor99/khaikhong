using Khaikhong.Domain.Common;

namespace Khaikhong.Domain.Entities;

public sealed class Product : AuditableEntity
{
    private readonly List<VariantOption> _options = new();
    private readonly List<Variant> _variants = new();
    private readonly List<BundleItem> _bundleItems = new();
    private readonly List<OrderItem> _orderItems = new();

    public string Name { get; private set; } = string.Empty;

    public string? Description { get; private set; }

    public decimal BasePrice { get; private set; }

    public string? Sku { get; private set; }

    public IReadOnlyCollection<VariantOption> Options => _options.AsReadOnly();

    public IReadOnlyCollection<Variant> Variants => _variants.AsReadOnly();

    public IReadOnlyCollection<BundleItem> BundleItems => _bundleItems.AsReadOnly();

    public IReadOnlyCollection<OrderItem> OrderItems => _orderItems.AsReadOnly();

    private Product()
    {
    }

    public static Product Create(string name, decimal basePrice, string? description = null, string? sku = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        if (basePrice < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(basePrice), "Base price must be greater than or equal to zero.");
        }

        return new Product
        {
            Name = name,
            BasePrice = basePrice,
            Description = description,
            Sku = sku
        };
    }

    public void UpdateDetails(string name, decimal basePrice, string? description, string? sku)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        if (basePrice < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(basePrice), "Base price must be greater than or equal to zero.");
        }

        Name = name;
        BasePrice = basePrice;
        Description = description;
        Sku = sku;
        Touch();
    }

    public void AddOptions(IEnumerable<VariantOption> options)
    {
        ArgumentNullException.ThrowIfNull(options);

        _options.AddRange(options);
    }

    public void AddVariants(IEnumerable<Variant> variants)
    {
        ArgumentNullException.ThrowIfNull(variants);

        _variants.AddRange(variants);
    }
}
