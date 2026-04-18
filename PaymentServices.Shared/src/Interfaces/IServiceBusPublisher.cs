using PaymentServices.Shared.Messages;

namespace PaymentServices.Shared.Interfaces;

/// <summary>
/// Abstraction over <see cref="Infrastructure.ServiceBusPublisher"/>.
/// Allows Function Apps to depend on the interface for testability.
/// </summary>
public interface IServiceBusPublisher
{
    /// <summary>
    /// Publishes a <see cref="PaymentMessage"/> to the payment-processing topic.
    /// The message subject is set to the current state for subscription filtering.
    /// </summary>
    Task PublishAsync(PaymentMessage message, CancellationToken cancellationToken = default);
}
