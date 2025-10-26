namespace Khaikhong.Application.Features.Orders.Commands.CreateOrder;

public sealed record CreateOrderResponseDto
{
    public Guid OrderId { get; init; }

    public int ItemCount { get; init; }
}
