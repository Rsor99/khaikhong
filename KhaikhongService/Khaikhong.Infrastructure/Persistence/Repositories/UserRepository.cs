using Khaikhong.Application.Contracts.Persistence.Repositories;
using Khaikhong.Domain.Entities;
using Khaikhong.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Khaikhong.Infrastructure.Persistence.Repositories;

public sealed class UserRepository(IdentityDbContext identityDbContext)
    : BaseRepository<User>(identityDbContext), IUserRepository
{
    private readonly IdentityDbContext _identityDbContext = identityDbContext;

    public async Task<User?> GetByEmailAsync(string email)
    {
        return await _identityDbContext.Users.FirstOrDefaultAsync(user => user.Email == email);
    }
}
