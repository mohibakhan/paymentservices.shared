namespace PaymentServices.Shared.Models;

/// <summary>
/// Base application settings shared across all PaymentServices Function Apps.
/// Each service extends this with service-specific settings.
///
/// Bound in Program.cs via:
/// <code>
///   services.AddOptions&lt;AppSettings&gt;()
///       .Configure&lt;IConfiguration&gt;((s, c) =>
///           c.GetSection("app:AppSettings").Bind(s));
/// </code>
/// </summary>
public class AppSettings
{
    // -------------------------------------------------------------------------
    // Cosmos DB
    // -------------------------------------------------------------------------

    /// <summary>Cosmos DB account endpoint URI. Use with Managed Identity.</summary>
    public string COSMOS_ENDPOINT { get; set; } = string.Empty;

    /// <summary>Cosmos DB connection string. Local development only.</summary>
    public string COSMOS_CONNSTRING { get; set; } = string.Empty;

    /// <summary>Cosmos DB database name.</summary>
    public string COSMOS_DATABASE { get; set; } = string.Empty;

    // -------------------------------------------------------------------------
    // Service Bus
    // -------------------------------------------------------------------------

    /// <summary>Azure Service Bus namespace connection string.</summary>
    public string SERVICE_BUS_CONNSTRING { get; set; } = string.Empty;

    /// <summary>Service Bus topic name. Default: payment-processing.</summary>
    public string SERVICE_BUS_TOPIC { get; set; } = "payment-processing";
}

/// <summary>
/// Telemetry settings bound from the <c>telemetry:</c> config section.
/// Same structure as existing services.
/// </summary>
public sealed class TelemetryAppSettings
{
    public string APP_INSIGHTS_CUSTOM_PROP_EVOLVE_TRAIN { get; set; } = string.Empty;
    public string APP_INSIGHTS_CUSTOM_PROP_EVOLVE_TEAM { get; set; } = string.Empty;
}
