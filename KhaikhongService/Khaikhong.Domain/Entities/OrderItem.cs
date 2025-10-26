namespace Khaikhong.Domain.Entities;

public sealed class OrderItem
{
    public Guid Id { get; private set; } = Guid.CreateVersion7();

    public Guid OrderId { get; private set; }

    public Guid? ProductId { get; private set; }

    public Guid? VariantId { get; private set; }

    public Guid? BundleId { get; private set; }

    public int Quantity { get; private set; }

    public Order Order { get; private set; } = null!;

    public Product? Product { get; private set; }

    public Variant? Variant { get; private set; }

    public Bundle? Bundle { get; private set; }

    private OrderItem()
    {
    }

    public static OrderItem Create(Guid orderId, int quantity, Guid? productId = null, Guid? variantId = null, Guid? bundleId = null)
    {
        if (quantity <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(quantity), "Quantity must be greater than zero.");
        }

        return new OrderItem
        {
            OrderId = orderId,
            Quantity = quantity,
            ProductId = productId,
            VariantId = variantId,
            BundleId = bundleId
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
}
