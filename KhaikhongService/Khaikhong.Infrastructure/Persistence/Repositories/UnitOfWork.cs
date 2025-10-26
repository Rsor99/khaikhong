using Khaikhong.Application.Contracts.Persistence;
using Khaikhong.Application.Contracts.Persistence.Repositories;
using Khaikhong.Infrastructure.Persistence;

namespace Khaikhong.Infrastructure.Persistence.Repositories;

public sealed class UnitOfWork(
    IdentityDbContext identityDbContext,
    KhaikhongDbContext khaikhongDbContext,
    IUserRepository userRepository) : IUnitOfWork
{
    private readonly IdentityDbContext _identityDbContext = identityDbContext;
    private readonly KhaikhongDbContext _khaikhongDbContext = khaikhongDbContext;
    private readonly IUserRepository _userRepository = userRepository;

    public IUserRepository Users => _userRepository;

    public async Task<int> CompleteAsync()
    {
        int identityChanges = await _identityDbContext.SaveChangesAsync();
        int businessChanges = await _khaikhongDbContext.SaveChangesAsync();

        return identityChanges + businessChanges;
    }
}
