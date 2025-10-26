namespace Khaikhong.Domain.Entities;

public sealed class VariantOptionValue
{
    private readonly List<ProductVariantCombination> _combinations = new();

    public Guid Id { get; private set; } = Guid.CreateVersion7();

    public Guid OptionId { get; private set; }

    public string Value { get; private set; } = string.Empty;

    public bool IsActive { get; private set; } = true;

    public VariantOption Option { get; internal set; } = null!;

    public IReadOnlyCollection<ProductVariantCombination> Combinations => _combinations.AsReadOnly();

    private VariantOptionValue()
    {
    }

    public static VariantOptionValue Create(Guid optionId, string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);

        return new VariantOptionValue
        {
            OptionId = optionId,
            Value = value
        };
    }

    public void UpdateValue(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        Value = value;
    }

    public void AddCombinations(IEnumerable<ProductVariantCombination> combinations)
    {
        ArgumentNullException.ThrowIfNull(combinations);

        _combinations.AddRange(combinations);
    }

    public void ReplaceCombinations(IEnumerable<ProductVariantCombination> combinations)
    {
        ArgumentNullException.ThrowIfNull(combinations);

        _combinations.Clear();
        _combinations.AddRange(combinations);
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
