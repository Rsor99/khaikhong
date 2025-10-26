namespace Khaikhong.Domain.Entities;

public sealed class VariantOption
{
    private readonly List<VariantOptionValue> _values = new();

    public Guid Id { get; private set; } = Guid.CreateVersion7();

    public Guid ProductId { get; private set; }

    public string Name { get; private set; } = string.Empty;

    public bool IsActive { get; private set; } = true;

    public Product Product { get; internal set; } = null!;

    public IReadOnlyCollection<VariantOptionValue> Values => _values.AsReadOnly();

    private VariantOption()
    {
    }

    public static VariantOption Create(Guid productId, string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        return new VariantOption
        {
            ProductId = productId,
            Name = name
        };
    }

    public void UpdateName(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        Name = name;
    }

    public void AddValues(IEnumerable<VariantOptionValue> values)
    {
        ArgumentNullException.ThrowIfNull(values);

        foreach (VariantOptionValue value in values)
        {
            value.Option = this;
            value.Activate();
            _values.Add(value);
        }
    }

    public void ReplaceValues(IEnumerable<VariantOptionValue> values)
    {
        ArgumentNullException.ThrowIfNull(values);

        _values.Clear();
        foreach (VariantOptionValue value in values)
        {
            value.Option = this;
            value.Activate();
            _values.Add(value);
        }
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
