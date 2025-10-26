using System.Linq.Expressions;
using Khaikhong.Application.Contracts.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Khaikhong.Infrastructure.Persistence.Repositories;

public class BaseRepository<T> : IRepository<T> where T : class
{
    protected BaseRepository(DbContext context)
    {
        Context = context;
        DbSet = context.Set<T>();
    }

    protected DbContext Context { get; }
    protected DbSet<T> DbSet { get; }

    public async Task<T?> GetByIdAsync(Guid id)
    {
        return await DbSet.FindAsync(id);
    }

    public async Task<IEnumerable<T>> GetAllAsync()
    {
        return await DbSet.ToListAsync();
    }

    public async Task<T?> FindAsync(Expression<Func<T, bool>> predicate)
    {
        return await DbSet.FirstOrDefaultAsync(predicate);
    }

    public async Task AddAsync(T entity)
    {
        await DbSet.AddAsync(entity);
    }

    public void Update(T entity)
    {
        DbSet.Update(entity);
    }

    public void Remove(T entity)
    {
        DbSet.Remove(entity);
    }
}
