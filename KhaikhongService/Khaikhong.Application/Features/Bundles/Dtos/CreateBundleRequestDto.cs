namespace Khaikhong.Application.Features.Bundles.Dtos;

public sealed record CreateBundleRequestDto
{
    public string Name { get; init; } = string.Empty;

    public string? Description { get; init; }

    public decimal Price { get; init; }

    public IReadOnlyCollection<CreateBundleProductDto> Products { get; init; } = Array.Empty<CreateBundleProductDto>();
}

public sealed record CreateBundleProductDto(Guid ProductId, List<CreateBundleVariantDto>? Variants, int? Quantity);

public sealed record CreateBundleVariantDto(Guid VariantId, int Quantity);
