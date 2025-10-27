namespace Khaikhong.Application.Features.Bundles.Dtos;

public sealed record BundleResponseDto
{
    public Guid Id { get; init; }

    public string Name { get; init; } = string.Empty;

    public string? Description { get; init; }

    public decimal Price { get; init; }

    public int? AvailableBundles { get; init; }

    public decimal? Savings { get; init; }

    public IReadOnlyCollection<BundleResponseProductDto> Products { get; init; } = Array.Empty<BundleResponseProductDto>();
}

public sealed record BundleResponseProductDto
{
    public Guid ProductId { get; init; }

    public string Name { get; init; } = string.Empty;

    public int? Quantity { get; init; }

    public IReadOnlyCollection<BundleResponseVariantDto>? Variants { get; init; }
}

public sealed record BundleResponseVariantDto
{
    public Guid VariantId { get; init; }

    public string? Sku { get; init; }

    public int Quantity { get; init; }
}
