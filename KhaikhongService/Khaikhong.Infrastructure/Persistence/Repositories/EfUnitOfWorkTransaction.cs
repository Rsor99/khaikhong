using System.Threading;
using System.Threading.Tasks;
using Khaikhong.Application.Contracts.Persistence;
using Microsoft.EntityFrameworkCore.Storage;

namespace Khaikhong.Infrastructure.Persistence.Repositories;

internal sealed class EfUnitOfWorkTransaction(IDbContextTransaction transaction) : IUnitOfWorkTransaction
{
    private readonly IDbContextTransaction _transaction = transaction;

    public Task CommitAsync(CancellationToken cancellationToken = default) =>
        _transaction.CommitAsync(cancellationToken);

    public Task RollbackAsync(CancellationToken cancellationToken = default) =>
        _transaction.RollbackAsync(cancellationToken);

    public ValueTask DisposeAsync() => _transaction.DisposeAsync();
}
