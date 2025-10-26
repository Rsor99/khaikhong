namespace Khaikhong.Application.Features.Products.Dtos;

public sealed class CreateProductResponseDto
{
    public Guid Id { get; init; }

    public decimal BasePrice { get; init; }
}
