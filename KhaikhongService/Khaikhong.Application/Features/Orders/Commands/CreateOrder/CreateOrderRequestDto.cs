namespace Khaikhong.Application.Features.Orders.Commands.CreateOrder;

public sealed record CreateOrderRequestDto
{
    public IReadOnlyCollection<CreateOrderItemRequestDto> Items { get; init; } = Array.Empty<CreateOrderItemRequestDto>();
}

public sealed record CreateOrderItemRequestDto
{
    public Guid Id { get; init; }

    public string Type { get; init; } = string.Empty;

    public int Quantity { get; init; }
}
