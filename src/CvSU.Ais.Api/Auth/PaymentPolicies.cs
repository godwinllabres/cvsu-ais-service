namespace CvSU.Ais.Api.Auth;

/// <summary>Authorization policy names for batch-payment and transmittal documents
/// (LDDAP-ADA, DV Transmittal, Audit Intake). Approving a batch disbursement is an
/// accountant task; physically transmitting for payment is Treasury's.</summary>
public static class PaymentPolicies
{
    public const string LddapManage = "lddap:manage";
    public const string LddapApprove = "lddap:approve";
    public const string LddapTransmit = "lddap:transmit";
    public const string Transmittal = "dv-transmittal:manage";
    public const string AuditIntake = "audit-intake:manage";
}
