using Khaikhong.Domain.Common;

namespace Khaikhong.Domain.Entities;

public sealed class Variant : AuditableEntity
{
    private readonly List<ProductVariantCombination> _combinations = new();
    private readonly List<BundleItem> _bundleItems = new();
    private readonly List<OrderItem> _orderItems = new();

    public Guid ProductId { get; private set; }

    public string? Sku { get; private set; }

    public decimal Price { get; private set; }

    public int Stock { get; private set; }

    public Product Product { get; internal set; } = null!;

    public IReadOnlyCollection<ProductVariantCombination> Combinations => _combinations.AsReadOnly();

    public IReadOnlyCollection<BundleItem> BundleItems => _bundleItems.AsReadOnly();

    public IReadOnlyCollection<OrderItem> OrderItems => _orderItems.AsReadOnly();

    private Variant()
    {
    }

    public static Variant Create(Guid productId, decimal price, int stock, string? sku = null)
    {
        if (price < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(price), "Price must be greater than or equal to zero.");
        }

        if (stock < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(stock), "Stock must be greater than or equal to zero.");
        }

        return new Variant
        {
            ProductId = productId,
            Price = price,
            Stock = stock,
            Sku = sku
        };
    }

    public void UpdateInventory(int stock)
    {
        if (stock < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(stock), "Stock must be greater than or equal to zero.");
        }

        Stock = stock;
        Touch();
    }

    public void UpdatePricing(decimal price, string? sku)
    {
        if (price < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(price), "Price must be greater than or equal to zero.");
        }

        Price = price;
        Sku = sku;
        Touch();
    }

    public void AddCombinations(IEnumerable<ProductVariantCombination> combinations)
    {
        ArgumentNullException.ThrowIfNull(combinations);

        foreach (ProductVariantCombination combination in combinations)
        {
            _combinations.Add(combination);
            combination.Activate();
        }
    }

    public void ReplaceCombinations(IEnumerable<ProductVariantCombination> combinations)
    {
        ArgumentNullException.ThrowIfNull(combinations);

        _combinations.Clear();
        _combinations.AddRange(combinations);
        Touch();
    }
}
