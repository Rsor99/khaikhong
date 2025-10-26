using Khaikhong.Domain.Common;

namespace Khaikhong.Domain.Entities;

public sealed class Order : BaseEntity
{
    private readonly List<OrderItem> _items = new();

    public Guid UserId { get; private set; }

    public User User { get; private set; } = null!;

    public IReadOnlyCollection<OrderItem> Items => _items.AsReadOnly();

    private Order()
    {
    }

    public static Order Create(Guid userId) =>
        new()
        {
            UserId = userId
        };
}
