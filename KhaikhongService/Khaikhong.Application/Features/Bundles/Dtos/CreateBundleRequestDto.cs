using System;
using System.Collections.Generic;

namespace Khaikhong.Application.Features.Bundles.Dtos;

public sealed record CreateBundleRequestDto
{
    public string Name { get; init; } = string.Empty;

    public string? Description { get; init; }

    public decimal Price { get; init; }

    public IReadOnlyCollection<BundleProductDto> Products { get; init; } = Array.Empty<BundleProductDto>();
}

public sealed record BundleProductDto
{
    public Guid ProductId { get; init; }

    public IReadOnlyCollection<Guid>? Variants { get; init; }

    public int Quantity { get; init; } = 1;
}
