using System.Linq.Expressions;
using MongoDB.Driver;
using MongoDB.Bson;

namespace Monei.Core.Repositories;

public abstract class MongoRepository<T, TId> : IRepository<T, TId> where T : class
{
    protected readonly IMongoCollection<T> _collection;
    protected readonly Expression<Func<T, TId>> _idSelector;

    protected MongoRepository(IMongoDatabase database, string collectionName, Expression<Func<T, TId>> idSelector)
    {
        _collection = database.GetCollection<T>(collectionName);
        _idSelector = idSelector;
    }

    public virtual async Task<IReadOnlyList<T>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _collection.Find(_ => true).ToListAsync(cancellationToken);
    }

    public virtual async Task<T?> GetByIdAsync(TId id, CancellationToken cancellationToken = default)
    {
        var filter = BuildIdFilter(id);
        return await _collection.Find(filter).FirstOrDefaultAsync(cancellationToken);
    }

    public virtual async Task<T> CreateAsync(T entity, CancellationToken cancellationToken = default)
    {
        await _collection.InsertOneAsync(entity, cancellationToken: cancellationToken);
        return entity;
    }

    public virtual async Task UpdateAsync(T entity, CancellationToken cancellationToken = default)
    {
        var id = _idSelector.Compile()(entity);
        var filter = BuildIdFilter(id);
        await _collection.ReplaceOneAsync(filter, entity, cancellationToken: cancellationToken);
    }

    public virtual async Task DeleteAsync(TId id, CancellationToken cancellationToken = default)
    {
        var filter = BuildIdFilter(id);
        await _collection.DeleteOneAsync(filter, cancellationToken);
    }

    private FilterDefinition<T> BuildIdFilter(TId id)
    {
        var parameter = _idSelector.Parameters[0];
        var property = _idSelector.Body;
        var constant = Expression.Constant(id);
        var equal = Expression.Equal(property, constant);
        var lambda = Expression.Lambda<Func<T, bool>>(equal, parameter);
        return Builders<T>.Filter.Where(lambda);
    }
}