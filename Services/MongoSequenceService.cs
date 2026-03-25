using MongoDB.Driver;

namespace SmartELibrary.Services;

/// <summary>
/// Provides auto-incrementing integer IDs for MongoDB documents,
/// mimicking the behavior of auto-increment primary keys in relational databases.
/// Uses a dedicated 'counters' collection in MongoDB.
/// </summary>
public interface IMongoSequenceService
{
    Task<int> GetNextIdAsync(string collectionName, CancellationToken cancellationToken = default);
}

public class MongoSequenceService : IMongoSequenceService
{
    private readonly IMongoCollection<CounterDocument> _counters;

    public MongoSequenceService(IConfiguration configuration)
    {
        var connectionString =
            configuration.GetConnectionString("DefaultConnection")
            ?? configuration["MONGODB_URI"];
        if (string.IsNullOrEmpty(connectionString))
            throw new InvalidOperationException(
                "MongoDB connection string is not configured. "
                + "Set ConnectionStrings__DefaultConnection or MONGODB_URI.");
        var databaseName =
            configuration["Database:Name"]
            ?? configuration["MONGODB_DATABASE"]
            ?? "EduVaultDB";
        var client = new MongoClient(connectionString);
        var database = client.GetDatabase(databaseName);
        _counters = database.GetCollection<CounterDocument>("_id_counters");
    }

    public async Task<int> GetNextIdAsync(string collectionName, CancellationToken cancellationToken = default)
    {
        var filter = Builders<CounterDocument>.Filter.Eq(x => x.Id, collectionName);
        var update = Builders<CounterDocument>.Update.Inc(x => x.Sequence, 1);
        var options = new FindOneAndUpdateOptions<CounterDocument>
        {
            IsUpsert = true,
            ReturnDocument = ReturnDocument.After
        };
        var result = await _counters.FindOneAndUpdateAsync(filter, update, options, cancellationToken);
        return result.Sequence;
    }

    private class CounterDocument
    {
        [MongoDB.Bson.Serialization.Attributes.BsonId]
        public string Id { get; set; } = string.Empty;
        public int Sequence { get; set; }
    }
}
