using Khaikhong.Application.Contracts.Persistence.Repositories;
using Khaikhong.Domain.Entities;

namespace Khaikhong.Infrastructure.Persistence.Repositories;

public sealed class OrderRepository(KhaikhongDbContext context) : BaseRepository<Order>(context), IOrderRepository
{
}
