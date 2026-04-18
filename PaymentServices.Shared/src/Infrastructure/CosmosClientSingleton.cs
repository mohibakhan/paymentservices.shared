using System.Text.Json;
using System.Text.Json.Serialization;
using Azure.Identity;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Cosmos.Serialization.HybridRow;
using Microsoft.Extensions.Logging;

namespace PaymentServices.Shared.Infrastructure;

/// <summary>
/// Provides a singleton <see cref="CosmosClient"/> configured with
/// Managed Identity and System.Text.Json serialization.
///
/// Usage in Function App Program.cs:
/// <code>
///   services.AddSingleton(sp =>
///       CosmosClientSingleton.Create(
///           endpoint: config["app:AppSettings:COSMOS_ENDPOINT"],
///           managedIdentityClientId: config["AZURE_CLIENT_ID"],
///           logger: sp.GetRequiredService&lt;ILogger&lt;CosmosClient&gt;&gt;()));
/// </code>
/// </summary>
public static class CosmosClientSingleton
{
    private static CosmosClient? _instance;
    private static readonly Lock _lock = new();

    /// <summary>
    /// Creates or returns the existing singleton <see cref="CosmosClient"/>.
    /// When <paramref name="managedIdentityClientId"/> is provided, uses
    /// user-assigned Managed Identity. Falls back to connection string for local dev.
    /// </summary>
    public static CosmosClient Create(
        string endpoint,
        string? managedIdentityClientId = null,
        string? connectionString = null,
        ILogger? logger = null)
    {
        if (_instance is not null)
            return _instance;

        lock (_lock)
        {
            if (_instance is not null)
                return _instance;

            var serializerOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
                Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
            };

            var cosmosSerializerOptions = new CosmosSerializationOptions
            {
                PropertyNamingPolicy = CosmosPropertyNamingPolicy.CamelCase
            };

            var clientOptions = new CosmosClientOptions
            {
                Serializer = new CosmosSystemTextJsonSerializer(serializerOptions),
                ConnectionMode = ConnectionMode.Direct,
                MaxRetryAttemptsOnRateLimitedRequests = 9,
                MaxRetryWaitTimeOnRateLimitedRequests = TimeSpan.FromSeconds(30)
            };

            if (!string.IsNullOrWhiteSpace(managedIdentityClientId) && !string.IsNullOrWhiteSpace(endpoint))
            {
                logger?.LogInformation("CosmosClient initializing with Managed Identity {ClientId}", managedIdentityClientId);

                var credentialOptions = new DefaultAzureCredentialOptions
                {
                    ManagedIdentityClientId = managedIdentityClientId
                };
                var credential = new DefaultAzureCredential(credentialOptions);
                _instance = new CosmosClient(endpoint, credential, clientOptions);
            }
            else if (!string.IsNullOrWhiteSpace(connectionString))
            {
                logger?.LogWarning("CosmosClient initializing with connection string — local development mode only");
                _instance = new CosmosClient(connectionString, clientOptions);
            }
            else
            {
                throw new InvalidOperationException(
                    "CosmosClient requires either a Managed Identity client ID + endpoint, or a connection string.");
            }

            return _instance;
        }
    }
}

/// <summary>
/// Cosmos DB serializer backed by System.Text.Json.
/// Replaces the default Newtonsoft.Json serializer.
/// </summary>
public sealed class CosmosSystemTextJsonSerializer : CosmosSerializer
{
    private readonly JsonSerializerOptions _options;

    public CosmosSystemTextJsonSerializer(JsonSerializerOptions options)
    {
        _options = options;
    }

    public override T FromStream<T>(Stream stream)
    {
        using (stream)
        {
            if (stream.Length == 0)
                return default!;

            return JsonSerializer.Deserialize<T>(stream, _options)!;
        }
    }

    public override Stream ToStream<T>(T input)
    {
        var ms = new MemoryStream();
        JsonSerializer.Serialize(ms, input, _options);
        ms.Position = 0;
        return ms;
    }
}
