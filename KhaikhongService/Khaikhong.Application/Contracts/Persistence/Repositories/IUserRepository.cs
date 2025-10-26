using Khaikhong.Domain.Entities;

namespace Khaikhong.Application.Contracts.Persistence.Repositories;

public interface IUserRepository : IRepository<User>
{
    Task<User?> GetByEmailAsync(string email);
}
