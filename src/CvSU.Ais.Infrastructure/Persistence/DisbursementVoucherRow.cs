namespace CvSU.Ais.Infrastructure.Persistence;

/// <summary>Disbursement Voucher as stored. Mutable (it transitions), unlike the
/// ledger rows. Enum-valued fields persist as readable strings.</summary>
public sealed class DisbursementVoucherRow
{
    public string Name { get; set; } = default!;
    public string Encoder { get; set; } = default!;
    public decimal Amount { get; set; }
    public string FundingSourceCode { get; set; } = default!;
    public string Lifecycle { get; set; } = default!;
    public string Status { get; set; } = default!;
    public string? ApprovedBy { get; set; }
    public string? ApprovedForPaymentBy { get; set; }
    public bool BudgetCertified { get; set; }
    public bool InternalAuditConfirmed { get; set; }
    public bool EndUserConfirmed { get; set; }
    public bool AccountantSigned { get; set; }
}
