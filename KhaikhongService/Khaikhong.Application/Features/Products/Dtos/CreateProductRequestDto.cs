namespace Khaikhong.Application.Features.Products.Dtos;

public sealed record CreateProductRequestDto
{
    public string Name { get; init; } = string.Empty;

    public string? Description { get; init; }

    public decimal BasePrice { get; init; }

    public string? Sku { get; init; }

    public IReadOnlyCollection<ProductOptionDto> Options { get; init; } = Array.Empty<ProductOptionDto>();

    public IReadOnlyCollection<ProductVariantDto> Variants { get; init; } = Array.Empty<ProductVariantDto>();
}

public sealed record ProductOptionDto
{
    public string Name { get; init; } = string.Empty;

    public IReadOnlyCollection<string> Values { get; init; } = Array.Empty<string>();
}

public sealed record ProductVariantDto
{
    public string? Sku { get; init; }

    public decimal Price { get; init; }

    public int Stock { get; init; }

    public IReadOnlyCollection<VariantSelectionDto> Selections { get; init; } = Array.Empty<VariantSelectionDto>();
}

public sealed record VariantSelectionDto
{
    public string OptionName { get; init; } = string.Empty;

    public string Value { get; init; } = string.Empty;
}
