using Khaikhong.Application.Contracts.Persistence;
using Khaikhong.Application.Contracts.Persistence.Repositories;
using System.Threading;
using System.Threading.Tasks;
using Khaikhong.Infrastructure.Persistence;

namespace Khaikhong.Infrastructure.Persistence.Repositories;

public sealed class UnitOfWork(
    IdentityDbContext identityDbContext,
    KhaikhongDbContext khaikhongDbContext,
    IUserRepository userRepository,
    IProductRepository productRepository,
    IOrderRepository orderRepository) : IUnitOfWork
{
    private readonly IdentityDbContext _identityDbContext = identityDbContext;
    private readonly KhaikhongDbContext _khaikhongDbContext = khaikhongDbContext;
    private readonly IUserRepository _userRepository = userRepository;
    private readonly IProductRepository _productRepository = productRepository;
    private readonly IOrderRepository _orderRepository = orderRepository;

    public IUserRepository Users => _userRepository;

    public IProductRepository Products => _productRepository;

    public IOrderRepository Orders => _orderRepository;

    public async Task<int> CompleteAsync()
    {
        int identityChanges = await _identityDbContext.SaveChangesAsync();
        int businessChanges = await _khaikhongDbContext.SaveChangesAsync();

        return identityChanges + businessChanges;
    }

    public async Task<IUnitOfWorkTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default)
    {
        var transaction = await _khaikhongDbContext.Database.BeginTransactionAsync(cancellationToken);
        return new EfUnitOfWorkTransaction(transaction);
    }
}
