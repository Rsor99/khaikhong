namespace Khaikhong.Application.Features.Products.Dtos;

public sealed record ProductResponseDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public decimal BasePrice { get; init; }
    public string? Sku { get; init; }
    public int? BaseStock { get; init; }
    public IReadOnlyCollection<ProductOptionResponseDto> Options { get; init; } = Array.Empty<ProductOptionResponseDto>();
    public IReadOnlyCollection<ProductVariantResponseDto> Variants { get; init; } = Array.Empty<ProductVariantResponseDto>();
}

public sealed record ProductOptionResponseDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public IReadOnlyCollection<ProductOptionValueResponseDto> Values { get; init; } = Array.Empty<ProductOptionValueResponseDto>();
}

public sealed record ProductOptionValueResponseDto
{
    public Guid Id { get; init; }
    public string Value { get; init; } = string.Empty;
}

public sealed record ProductVariantResponseDto
{
    public Guid Id { get; init; }
    public string? Sku { get; init; }
    public decimal Price { get; init; }
    public int Stock { get; init; }
    public IReadOnlyCollection<ProductVariantCombinationResponseDto> Combinations { get; init; } = Array.Empty<ProductVariantCombinationResponseDto>();
}

public sealed record ProductVariantCombinationResponseDto
{
    public Guid Id { get; init; }
    public Guid OptionValueId { get; init; }
}
