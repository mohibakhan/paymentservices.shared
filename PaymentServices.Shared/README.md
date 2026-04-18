# PaymentServices.Shared

Shared contracts, models, and infrastructure NuGet package for the PaymentServices pipeline.

## Contents

| Namespace | Contents |
|---|---|
| `PaymentServices.Shared.Messages` | `PaymentMessage` — the Service Bus envelope flowing through all services |
| `PaymentServices.Shared.Models` | Cosmos DB models: `CosmosTransaction`, `CosmosAccount`, `CosmosLedger`, `AppSettings` |
| `PaymentServices.Shared.Enums` | `TransactionState`, `KycOutcome`, `TmsOutcome`, `PartyType` |
| `PaymentServices.Shared.Interfaces` | `ICosmosRepository<T>`, `IServiceBusPublisher` |
| `PaymentServices.Shared.Infrastructure` | `CosmosClientSingleton`, `CosmosSystemTextJsonSerializer`, `ServiceBusPublisher` |
| `PaymentServices.Shared.Extensions` | `ServiceCollectionExtensions` — DI helpers for all Function Apps |

## Usage in a Function App

### Program.cs
```csharp
using PaymentServices.Shared.Extensions;

var host = new HostBuilder()
    .ConfigureAppConfiguration(SetupAppConfiguration)
    .ConfigureFunctionsWebApplication()
    .ConfigureServices((context, services) =>
    {
        var config = context.Configuration;

        services.AddPaymentAppSettings(config);
        services.AddPaymentCosmosClient(config);
        services.AddPaymentServiceBusPublisher(config);

        // Register specific containers needed by this Function App
        services.AddCosmosContainer(config, "tch-send-transactions", "transactions");
        services.AddCosmosContainer(config, "tch-send-idempotency", "idempotency");
    })
    .Build();
```

### local.settings.json
```json
{
  "IsEncrypted": false,
  "Values": {
    "AzureWebJobsStorage": "UseDevelopmentStorage=true",
    "FUNCTIONS_WORKER_RUNTIME": "dotnet-isolated",
    "AZURE_CLIENT_ID": "<managed-identity-client-id>",
    "AppConfig:Endpoint": "<app-config-endpoint>",
    "app:AppSettings:COSMOS_ENDPOINT": "<cosmos-endpoint>",
    "app:AppSettings:COSMOS_CONNSTRING": "<local-dev-connstring>",
    "app:AppSettings:COSMOS_DATABASE": "payments",
    "app:AppSettings:SERVICE_BUS_CONNSTRING": "<service-bus-connstring>",
    "app:AppSettings:SERVICE_BUS_TOPIC": "payment-processing",
    "telemetry:APP_INSIGHTS_CUSTOM_PROP_EVOLVE_TRAIN": "Digital",
    "telemetry:APP_INSIGHTS_CUSTOM_PROP_EVOLVE_TEAM": "Services"
  }
}
```

## Cosmos DB Containers

| Container | Partition Key | TTL | Used By |
|---|---|---|---|
| `tch-send-transactions` | `/evolveId` | None (90 day recommended) | Gateway, all services |
| `tch-send-idempotency` | `/evolveId` | 86400s (24h) | Gateway |
| `tch-accounts` | `/accountNumber` | None | AccountResolution, Gateway |
| `ledgers` | `/ledgerId` | None | Transfer |

## Service Bus Topic

**Topic:** `payment-processing`

| Subscription | Filter (Subject) | Handler |
|---|---|---|
| `account-resolution` | `AccountResolutionPending` | PaymentServices.AccountResolution |
| `kyc-check` | `KycPending` | PaymentServices.Compliance |
| `tms-check` | `TmsPending` | PaymentServices.Compliance |
| `transfer` | `TmsCompleted` | PaymentServices.Transfer |
| `event-notification` | `TransferCompleted`, `*Failed`, `*Alert`, `*Review` | PaymentServices.EventNotification |

## Versioning

`Major.Minor.BuildId` — bump Major/Minor manually in the ADO pipeline variables on breaking changes.

- `main` branch → stable: `1.0.123`
- feature branches → pre-release: `1.0.123-preview.123`
