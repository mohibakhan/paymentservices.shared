namespace PaymentServices.Shared.Enums;

/// <summary>
/// Alloy KYC evaluation outcome values.
/// </summary>
public enum KycOutcome
{
    Unknown = 0,
    Approved = 1,
    ManualReview = 2,
    Denied = 3
}

/// <summary>
/// Alloy TMS evaluation outcome values.
/// </summary>
public enum TmsOutcome
{
    Unknown = 0,
    Approved = 1,
    ComplianceAlert = 2,
    Denied = 3
}

/// <summary>
/// Indicates which party in the transaction a compliance check applies to.
/// </summary>
public enum PartyType
{
    Source = 0,
    Destination = 1
}
