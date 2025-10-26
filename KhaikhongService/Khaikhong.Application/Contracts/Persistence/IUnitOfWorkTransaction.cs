using System.Threading;
using System.Threading.Tasks;

namespace Khaikhong.Application.Contracts.Persistence;

public interface IUnitOfWorkTransaction : IAsyncDisposable
{
    Task CommitAsync(CancellationToken cancellationToken = default);

    Task RollbackAsync(CancellationToken cancellationToken = default);
}
