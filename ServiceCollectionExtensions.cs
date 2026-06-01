using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PaymentServices.Shared.Infrastructure;
using PaymentServices.Shared.Interfaces;
using PaymentServices.Shared.Models;

namespace PaymentServices.Shared.Extensions;

/// <summary>
/// Extension methods for <see cref="IServiceCollection"/> to register
/// shared PaymentServices infrastructure in each Function App.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers a singleton <see cref="CosmosClient"/> using Managed Identity
    /// (production) or connection string (local development).
    /// </summary>
    /// <param name="services">DI container.</param>
    /// <param name="configuration">App configuration root.</param>
    /// <param name="prefix">Config section prefix, e.g. "rtpSend:AppSettings".</param>
    /// <param name="useDefaultSerializer">
    /// When false (default): client uses System.Text.Json with camelCase
    /// property naming — the platform convention. When true: client uses
    /// the SDK's built-in Newtonsoft.Json serializer (PascalCase). Set true
    /// when round-tripping PascalCase data (e.g., for the ledger NuGet).
    /// </param>
    public static IServiceCollection AddPaymentCosmosClient(
        this IServiceCollection services,
        IConfiguration configuration,
        string prefix = "app:AppSettings",
        bool useDefaultSerializer = false)
    {
        services.AddSingleton(sp =>
        {
            var logger = sp.GetRequiredService<ILogger<CosmosClient>>();
            var endpoint = configuration[$"{prefix}:COSMOS_ENDPOINT"] ?? string.Empty;
            var connString = configuration[$"{prefix}:COSMOS_CONNSTRING"] ?? string.Empty;
            var managedIdentityClientId = configuration["AZURE_CLIENT_ID"] ?? string.Empty;

            return CosmosClientSingleton.Create(
                endpoint: endpoint,
                managedIdentityClientId: string.IsNullOrWhiteSpace(managedIdentityClientId)
                    ? null : managedIdentityClientId,
                connectionString: string.IsNullOrWhiteSpace(connString)
                    ? null : connString,
                useDefaultSerializer: useDefaultSerializer,
                logger: logger);
        });

        return services;
    }

    /// <summary>
    /// Registers a KEYED singleton <see cref="CosmosClient"/> using Managed
    /// Identity (production) or connection string (local development).
    /// Useful when a service needs MULTIPLE CosmosClient instances — e.g., one
    /// with the default camelCase serializer for the service's own data, and
    /// another with PascalCase for interop with the ledger NuGet.
    ///
    /// Resolve with <c>sp.GetRequiredKeyedService&lt;CosmosClient&gt;(serviceKey)</c>.
    /// </summary>
    /// <param name="services">DI container.</param>
    /// <param name="configuration">App configuration root.</param>
    /// <param name="serviceKey">DI key (e.g. "ledger").</param>
    /// <param name="prefix">Config section prefix, e.g. "rtpSend:AppSettings".</param>
    /// <param name="useDefaultSerializer">
    /// When true: client uses the SDK's built-in Newtonsoft.Json serializer
    /// (PascalCase). When false: camelCase via System.Text.Json. Defaults to
    /// true for keyed clients since the common use case is PascalCase interop.
    /// </param>
    public static IServiceCollection AddKeyedPaymentCosmosClient(
        this IServiceCollection services,
        IConfiguration configuration,
        string serviceKey,
        string prefix = "app:AppSettings",
        bool useDefaultSerializer = true)
    {
        services.AddKeyedSingleton<CosmosClient>(serviceKey, (sp, _) =>
        {
            var logger = sp.GetRequiredService<ILogger<CosmosClient>>();
            var endpoint = configuration[$"{prefix}:COSMOS_ENDPOINT"] ?? string.Empty;
            var connString = configuration[$"{prefix}:COSMOS_CONNSTRING"] ?? string.Empty;
            var managedIdentityClientId = configuration["AZURE_CLIENT_ID"] ?? string.Empty;

            return CosmosClientSingleton.Create(
                endpoint: endpoint,
                managedIdentityClientId: string.IsNullOrWhiteSpace(managedIdentityClientId)
                    ? null : managedIdentityClientId,
                connectionString: string.IsNullOrWhiteSpace(connString)
                    ? null : connString,
                useDefaultSerializer: useDefaultSerializer,
                logger: logger);
        });

        return services;
    }

    /// <summary>
    /// Registers a <see cref="Container"/> for a specific Cosmos container name.
    /// Call once per container needed in the Function App.
    /// </summary>
    public static IServiceCollection AddCosmosContainer(
        this IServiceCollection services,
        IConfiguration configuration,
        string containerName,
        string serviceKey,
        string prefix = "app:AppSettings")
    {
        services.AddKeyedSingleton<Container>(serviceKey, (sp, _) =>
        {
            var client = sp.GetRequiredService<CosmosClient>();
            var database = configuration[$"{prefix}:COSMOS_DATABASE"] ?? "tptch";
            return client.GetContainer(database, containerName);
        });

        return services;
    }

    /// <summary>
    /// Registers the <see cref="ServiceBusPublisher"/> and its interface.
    /// </summary>
    public static IServiceCollection AddPaymentServiceBusPublisher(
        this IServiceCollection services,
        IConfiguration configuration,
        string prefix = "app:AppSettings")
    {
        services.AddSingleton<IServiceBusPublisher>(sp =>
        {
            var logger = sp.GetRequiredService<ILogger<ServiceBusPublisher>>();
            var connString = configuration[$"{prefix}:SERVICE_BUS_CONNSTRING"]
                ?? throw new InvalidOperationException(
                    $"SERVICE_BUS_CONNSTRING is required. Prefix={prefix}");
            var topic = configuration[$"{prefix}:SERVICE_BUS_TOPIC"] ?? "payment-processing";

            return new ServiceBusPublisher(connString, topic, logger);
        });

        return services;
    }

    /// <summary>
    /// Binds the shared <see cref="AppSettings"/> and <see cref="TelemetryAppSettings"/>
    /// from configuration. Call in every Function App.
    /// </summary>
    public static IServiceCollection AddPaymentAppSettings(
        this IServiceCollection services,
        IConfiguration configuration,
        string prefix = "app:AppSettings")
    {
        services.AddOptions<AppSettings>()
            .Configure<IConfiguration>((settings, config) =>
                config.GetSection(prefix).Bind(settings));

        services.AddOptions<TelemetryAppSettings>()
            .Configure<IConfiguration>((settings, config) =>
                config.GetSection("telemetry").Bind(settings));

        return services;
    }
}