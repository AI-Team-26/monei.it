using MongoDB.Driver;
using Monei.Core.Entities;

namespace Monei.Core.Repositories;

public sealed class TransactionRepository : MongoRepository<Transaction, string>
{
    public const string CollectionName = "transactions";

    public TransactionRepository(IMongoDatabase database) : base(database, CollectionName, t => t.Id)
    {
    }

    public async Task<IReadOnlyList<Transaction>> GetByCategoryAsync(string category, CancellationToken cancellationToken = default)
    {
        var filter = Builders<Transaction>.Filter.Eq(t => t.Category, category);
        return await _collection.Find(filter).ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Transaction>> GetByDateRangeAsync(DateTime start, DateTime end, CancellationToken cancellationToken = default)
    {
        var filter = Builders<Transaction>.Filter.And(
            Builders<Transaction>.Filter.Gte(t => t.Date, start),
            Builders<Transaction>.Filter.Lte(t => t.Date, end));
        return await _collection.Find(filter).ToListAsync(cancellationToken);
    }
}