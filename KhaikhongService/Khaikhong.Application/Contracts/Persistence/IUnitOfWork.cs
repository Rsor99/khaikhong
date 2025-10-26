using Khaikhong.Application.Contracts.Persistence.Repositories;

namespace Khaikhong.Application.Contracts.Persistence;

public interface IUnitOfWork
{
    IUserRepository Users { get; }
    Task<int> CompleteAsync();
}
