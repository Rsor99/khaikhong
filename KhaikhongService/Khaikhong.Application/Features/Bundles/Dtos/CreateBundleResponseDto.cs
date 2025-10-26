namespace Khaikhong.Application.Features.Bundles.Dtos;

public sealed record CreateBundleResponseDto
{
    public Guid Id { get; init; }

    public string Name { get; init; } = string.Empty;

    public string? Description { get; init; }

    public decimal Price { get; init; }

    public int ProductCount { get; init; }

    public IReadOnlyCollection<BundleItemResponseDto> Items { get; init; } = Array.Empty<BundleItemResponseDto>();
}

public sealed record BundleItemResponseDto
{
    public Guid ProductId { get; init; }

    public Guid? VariantId { get; init; }

    public int Quantity { get; init; }
}
