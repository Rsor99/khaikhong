namespace Khaikhong.Domain.Entities;

public sealed class ProductVariantCombination
{
    public Guid Id { get; private set; } = Guid.CreateVersion7();

    public Guid VariantId { get; private set; }

    public Guid OptionValueId { get; private set; }

    public Variant Variant { get; internal set; } = null!;

    public VariantOptionValue OptionValue { get; internal set; } = null!;

    public bool IsActive { get; private set; } = true;

    private ProductVariantCombination()
    {
    }

    public static ProductVariantCombination Create(Guid variantId, Guid optionValueId) =>
        new()
        {
            VariantId = variantId,
            OptionValueId = optionValueId
        };

    public void AttachOptionValue(VariantOptionValue optionValue)
    {
        OptionValue = optionValue;
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
