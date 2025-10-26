using Khaikhong.Domain.Common;

namespace Khaikhong.Domain.Entities;

public sealed class Bundle : AuditableEntity
{
    private readonly List<BundleItem> _items = new();

    public string Name { get; private set; } = string.Empty;

    public string? Description { get; private set; }

    public decimal Price { get; private set; }

    public IReadOnlyCollection<BundleItem> Items => _items.AsReadOnly();

    private Bundle()
    {
    }

    public static Bundle Create(string name, decimal price, string? description = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        if (price < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(price), "Price must be greater than or equal to zero.");
        }

        return new Bundle
        {
            Name = name,
            Price = price,
            Description = description
        };
    }

    public void UpdateDetails(string name, decimal price, string? description)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        if (price < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(price), "Price must be greater than or equal to zero.");
        }

        Name = name;
        Price = price;
        Description = description;
        Touch();
    }

    public void AddItems(IEnumerable<BundleItem> items)
    {
        if (items == null)
            throw new ArgumentNullException(nameof(items));

        _items.AddRange(items);
        Touch();
    }
}
