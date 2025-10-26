namespace Khaikhong.Application.Features.Products.Dtos;

public sealed record UpdateProductRequestDto
{
    public Guid ProductId { get; init; }

    public string Name { get; init; } = string.Empty;

    public string? Description { get; init; }

    public decimal BasePrice { get; init; }

    public string? Sku { get; init; }

    public int? BaseStock { get; init; }

    public IReadOnlyCollection<ProductOptionDto> Options { get; init; } = Array.Empty<ProductOptionDto>();

    public IReadOnlyCollection<ProductVariantDto> Variants { get; init; } = Array.Empty<ProductVariantDto>();
}
