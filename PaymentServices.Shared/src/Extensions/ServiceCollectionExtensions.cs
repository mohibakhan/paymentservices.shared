using System.Text.Json;
using System.Text.Json.Serialization;
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
    public static IServiceCollection AddPaymentCosmosClient(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddSingleton(sp =>
        {
            var logger = sp.GetRequiredService<ILogger<CosmosClient>>();
            var endpoint = configuration["app:AppSettings:COSMOS_ENDPOINT"] ?? string.Empty;
            var connString = configuration["app:AppSettings:COSMOS_CONNSTRING"] ?? string.Empty;
            var managedIdentityClientId = configuration["AZURE_CLIENT_ID"] ?? string.Empty;

            return CosmosClientSingleton.Create(
                endpoint: endpoint,
                managedIdentityClientId: string.IsNullOrWhiteSpace(managedIdentityClientId)
                    ? null : managedIdentityClientId,
                connectionString: string.IsNullOrWhiteSpace(connString)
                    ? null : connString,
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
        string serviceKey)
    {
        services.AddKeyedSingleton<Container>(serviceKey, (sp, _) =>
        {
            var client = sp.GetRequiredService<CosmosClient>();
            var database = configuration["app:AppSettings:COSMOS_DATABASE"] ?? "payments";
            return client.GetContainer(database, containerName);
        });

        return services;
    }

    /// <summary>
    /// Registers the <see cref="ServiceBusPublisher"/> and its interface.
    /// </summary>
    public static IServiceCollection AddPaymentServiceBusPublisher(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddSingleton<IServiceBusPublisher>(sp =>
        {
            var logger = sp.GetRequiredService<ILogger<ServiceBusPublisher>>();
            var connString = configuration["app:AppSettings:SERVICE_BUS_CONNSTRING"]
                ?? throw new InvalidOperationException("SERVICE_BUS_CONNSTRING is required.");
            var topic = configuration["app:AppSettings:SERVICE_BUS_TOPIC"] ?? "payment-processing";

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
        IConfiguration configuration)
    {
        services.AddOptions<AppSettings>()
            .Configure<IConfiguration>((settings, config) =>
                config.GetSection("app:AppSettings").Bind(settings));

        services.AddOptions<TelemetryAppSettings>()
            .Configure<IConfiguration>((settings, config) =>
                config.GetSection("telemetry").Bind(settings));

        return services;
    }
}
