using Microsoft.Azure.Cosmos;

namespace PaymentServices.Shared.Interfaces;

/// <summary>
/// Generic Cosmos DB repository contract.
/// Each Function App registers concrete implementations bound to specific containers.
/// </summary>
public interface ICosmosRepository<T> where T : class
{
    /// <summary>Creates a document. Throws if the item already exists.</summary>
    Task<T> CreateAsync(T item, string partitionKey, CancellationToken cancellationToken = default);

    /// <summary>Upserts a document.</summary>
    Task<T> UpsertAsync(T item, string partitionKey, CancellationToken cancellationToken = default);

    /// <summary>Reads a document by ID and partition key. Returns null if not found.</summary>
    Task<T?> GetAsync(string id, string partitionKey, CancellationToken cancellationToken = default);

    /// <summary>Deletes a document. Returns false if not found.</summary>
    Task<bool> DeleteAsync(string id, string partitionKey, CancellationToken cancellationToken = default);

    /// <summary>Runs a parameterised query and returns all matching items.</summary>
    Task<IReadOnlyList<T>> QueryAsync(QueryDefinition query, CancellationToken cancellationToken = default);
}

/// <summary>
/// Generic Cosmos DB repository implementation.
/// Bind to a specific container via DI in each Function App.
/// </summary>
public sealed class CosmosRepository<T> : ICosmosRepository<T> where T : class
{
    private readonly Container _container;

    public CosmosRepository(Container container)
    {
        _container = container;
    }

    public async Task<T> CreateAsync(T item, string partitionKey, CancellationToken cancellationToken = default)
    {
        var response = await _container.CreateItemAsync(
            item,
            new PartitionKey(partitionKey),
            cancellationToken: cancellationToken);

        return response.Resource;
    }

    public async Task<T> UpsertAsync(T item, string partitionKey, CancellationToken cancellationToken = default)
    {
        var response = await _container.UpsertItemAsync(
            item,
            new PartitionKey(partitionKey),
            cancellationToken: cancellationToken);

        return response.Resource;
    }

    public async Task<T?> GetAsync(string id, string partitionKey, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _container.ReadItemAsync<T>(
                id,
                new PartitionKey(partitionKey),
                cancellationToken: cancellationToken);

            return response.Resource;
        }
        catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }
    }

    public async Task<bool> DeleteAsync(string id, string partitionKey, CancellationToken cancellationToken = default)
    {
        try
        {
            await _container.DeleteItemAsync<T>(
                id,
                new PartitionKey(partitionKey),
                cancellationToken: cancellationToken);

            return true;
        }
        catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return false;
        }
    }

    public async Task<IReadOnlyList<T>> QueryAsync(QueryDefinition query, CancellationToken cancellationToken = default)
    {
        var results = new List<T>();
        using var iterator = _container.GetItemQueryIterator<T>(query);

        while (iterator.HasMoreResults)
        {
            var page = await iterator.ReadNextAsync(cancellationToken);
            results.AddRange(page);
        }

        return results.AsReadOnly();
    }
}
