using System.Threading;
using System.Threading.Tasks;
using Khaikhong.Application.Contracts.Persistence.Repositories;

namespace Khaikhong.Application.Contracts.Persistence;

public interface IUnitOfWork
{
    IUserRepository Users { get; }
    IProductRepository Products { get; }
    IOrderRepository Orders { get; }
    Task<int> CompleteAsync();
    Task<IUnitOfWorkTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default);
}
