using System.Text.Json;
using System.Text.Json.Serialization;
using Azure.Identity;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Logging;

namespace PaymentServices.Shared.Infrastructure;

/// <summary>
/// Factory for <see cref="CosmosClient"/> instances configured with Managed
/// Identity (production) or connection string (local dev).
///
/// IMPORTANT — Lifetime: this is a FACTORY, not a singleton. The CosmosClient
/// SDK is internally thread-safe and connection-pools across calls, so DI
/// should own the lifetime via <c>AddSingleton</c> in
/// <see cref="Extensions.ServiceCollectionExtensions.AddPaymentCosmosClient"/>.
/// Process-wide static caching here would prevent services from having
/// multiple CosmosClient instances (different accounts, different serializers,
/// test isolation), with no offsetting benefit.
///
/// Serialization defaults to System.Text.Json with camelCase property
/// naming — matching the convention used across PaymentServices. Pass
/// <paramref name="useDefaultSerializer"/> = true to skip this and use the
/// SDK's built-in Newtonsoft.Json serializer (PascalCase). Used when
/// interoperating with libraries whose models expect PascalCase JSON.
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
    /// <summary>
    /// Creates a new <see cref="CosmosClient"/>. The CosmosClient SDK
    /// is thread-safe and connection-pools internally — multiple instances
    /// can coexist in one process (e.g., to access different accounts or
    /// to use different serializers).
    /// </summary>
    /// <param name="endpoint">Cosmos account endpoint URL.</param>
    /// <param name="managedIdentityClientId">User-assigned MI client ID for prod auth.</param>
    /// <param name="connectionString">Connection string fallback for local dev.</param>
    /// <param name="useDefaultSerializer">
    /// When false (default): use System.Text.Json with camelCase property
    /// naming — the platform convention. When true: use the SDK's built-in
    /// Newtonsoft.Json serializer (PascalCase). Set true when models must
    /// round-trip PascalCase JSON (e.g., ledger / TPTCH legacy data).
    /// </param>
    /// <param name="logger">Optional logger for initialization messages.</param>
    public static CosmosClient Create(
        string endpoint,
        string? managedIdentityClientId = null,
        string? connectionString = null,
        bool useDefaultSerializer = false,
        ILogger? logger = null)
    {
        var clientOptions = new CosmosClientOptions
        {
            ConnectionMode = ConnectionMode.Direct,
            MaxRetryAttemptsOnRateLimitedRequests = 9,
            MaxRetryWaitTimeOnRateLimitedRequests = TimeSpan.FromSeconds(30)
        };

        if (!useDefaultSerializer)
        {
            var serializerOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
                Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
            };
            clientOptions.Serializer = new CosmosSystemTextJsonSerializer(serializerOptions);
        }

        if (!string.IsNullOrWhiteSpace(managedIdentityClientId) && !string.IsNullOrWhiteSpace(endpoint))
        {
            logger?.LogInformation(
                "CosmosClient initializing with Managed Identity {ClientId}, serializer={Serializer}",
                managedIdentityClientId,
                useDefaultSerializer ? "default (PascalCase)" : "camelCase");

            var credential = new DefaultAzureCredential(new DefaultAzureCredentialOptions
            {
                ManagedIdentityClientId = managedIdentityClientId
            });
            return new CosmosClient(endpoint, credential, clientOptions);
        }

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "CosmosClient requires either a Managed Identity client ID + endpoint, or a connection string.");
        }

        logger?.LogWarning(
            "CosmosClient initializing with connection string (local development mode), serializer={Serializer}",
            useDefaultSerializer ? "default (PascalCase)" : "camelCase");

        return new CosmosClient(connectionString, clientOptions);
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
