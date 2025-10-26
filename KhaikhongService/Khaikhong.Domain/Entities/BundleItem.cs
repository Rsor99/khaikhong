namespace Khaikhong.Domain.Entities;

public sealed class BundleItem
{
    public Guid Id { get; private set; } = Guid.CreateVersion7();

    public Guid BundleId { get; private set; }

    public Guid? ProductId { get; private set; }

    public Guid? VariantId { get; private set; }

    public int Quantity { get; private set; }

    public bool IsActive { get; private set; } = true;

    public Bundle Bundle { get; private set; } = null!;

    public Product? Product { get; private set; }

    public Variant? Variant { get; private set; }

    private BundleItem()
    {
    }

    public static BundleItem Create(Guid bundleId, int quantity, Guid? productId = null, Guid? variantId = null)
    {
        if (quantity <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(quantity), "Quantity must be greater than zero.");
        }

        if (productId is null && variantId is null)
        {
            throw new ArgumentException("Either productId or variantId must be provided.");
        }

        return new BundleItem
        {
            BundleId = bundleId,
            ProductId = productId,
            VariantId = variantId,
            Quantity = quantity
        };
    }

    public void UpdateQuantity(int quantity)
    {
        if (quantity <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(quantity), "Quantity must be greater than zero.");
        }

        Quantity = quantity;
    }

    public void Deactivate()
    {
        IsActive = false;
    }

    public void Activate()
    {
        IsActive = true;
    }
}
