namespace PaymentServices.Shared.Enums;

/// <summary>
/// Represents the state of a payment transaction as it moves through the pipeline.
/// Each state maps to a Service Bus subscription handler.
/// </summary>
public enum TransactionState
{
    /// <summary>Received by the Gateway, pending account resolution.</summary>
    Received = 0,

    /// <summary>Account resolution in progress.</summary>
    AccountResolutionPending = 10,

    /// <summary>Both source and destination accounts verified successfully.</summary>
    AccountResolutionCompleted = 20,

    /// <summary>Account not found or verification failed. Terminal state.</summary>
    AccountResolutionFailed = 21,

    /// <summary>KYC check in progress for source and/or destination.</summary>
    KycPending = 30,

    /// <summary>KYC passed for both source and destination.</summary>
    KycCompleted = 40,

    /// <summary>KYC flagged for manual review. Terminal state — notified via EventNotification.</summary>
    KycManualReview = 41,

    /// <summary>KYC failed. Terminal state — notified via EventNotification.</summary>
    KycFailed = 42,

    /// <summary>TMS/Sanctions check in progress.</summary>
    TmsPending = 50,

    /// <summary>TMS passed. Safe to proceed to transfer.</summary>
    TmsCompleted = 60,

    /// <summary>TMS compliance alert. Terminal state — notified via EventNotification.</summary>
    TmsComplianceAlert = 61,

    /// <summary>TMS check failed. Terminal state — notified via EventNotification.</summary>
    TmsFailed = 62,

    /// <summary>Internal ledger transfer in progress.</summary>
    TransferPending = 70,

    /// <summary>Transfer written to ledger successfully.</summary>
    TransferCompleted = 80,

    /// <summary>Transfer failed. Terminal state — notified via EventNotification.</summary>
    TransferFailed = 81,

    /// <summary>EventNotification sent to RTPSend webhook.</summary>
    NotificationSent = 90,

    /// <summary>Notification delivery failed. Will retry.</summary>
    NotificationFailed = 91
}
