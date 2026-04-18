using System.Text.Json;
using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.Logging;
using PaymentServices.Shared.Messages;

namespace PaymentServices.Shared.Infrastructure;

/// <summary>
/// Publishes <see cref="PaymentMessage"/> instances to Azure Service Bus.
/// Uses a singleton <see cref="ServiceBusClient"/> per best practice.
///
/// Usage in Function App Program.cs:
/// <code>
///   services.AddSingleton(sp =>
///       new ServiceBusPublisher(
///           connectionString: config["app:AppSettings:SERVICE_BUS_CONNSTRING"],
///           topicName: config["app:AppSettings:SERVICE_BUS_TOPIC"],
///           logger: sp.GetRequiredService&lt;ILogger&lt;ServiceBusPublisher&gt;&gt;()));
/// </code>
/// </summary>
public sealed class ServiceBusPublisher : IAsyncDisposable
{
    private readonly ServiceBusClient _client;
    private readonly string _topicName;
    private readonly ILogger<ServiceBusPublisher> _logger;

    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    public ServiceBusPublisher(
        string connectionString,
        string topicName,
        ILogger<ServiceBusPublisher> logger)
    {
        _client = new ServiceBusClient(connectionString);
        _topicName = topicName;
        _logger = logger;
    }

    /// <summary>
    /// Publishes a <see cref="PaymentMessage"/> to the configured Service Bus topic.
    /// The message subject is set to the current <see cref="PaymentMessage.State"/>
    /// so subscriptions can filter by state.
    /// </summary>
    public async Task PublishAsync(PaymentMessage message, CancellationToken cancellationToken = default)
    {
        await using var sender = _client.CreateSender(_topicName);

        var body = JsonSerializer.SerializeToUtf8Bytes(message, _jsonOptions);
        var serviceBusMessage = new ServiceBusMessage(body)
        {
            MessageId = message.EvolveId,
            CorrelationId = message.CorrelationId,
            Subject = message.State.ToString(),
            ContentType = "application/json",
            ApplicationProperties =
            {
                ["evolveId"] = message.EvolveId,
                ["fintechId"] = message.FintechId,
                ["state"] = message.State.ToString()
            }
        };

        _logger.LogInformation(
            "Publishing message {EvolveId} with state {State} to topic {Topic}",
            message.EvolveId, message.State, _topicName);

        await sender.SendMessageAsync(serviceBusMessage, cancellationToken);
    }

    /// <summary>
    /// Deserializes a received <see cref="ServiceBusReceivedMessage"/> body
    /// back into a <see cref="PaymentMessage"/>.
    /// </summary>
    public static PaymentMessage Deserialize(ServiceBusReceivedMessage message)
    {
        return JsonSerializer.Deserialize<PaymentMessage>(
            message.Body.ToArray(), _jsonOptions)
            ?? throw new InvalidOperationException(
                $"Failed to deserialize PaymentMessage for MessageId={message.MessageId}");
    }

    public async ValueTask DisposeAsync()
    {
        await _client.DisposeAsync();
    }
}
