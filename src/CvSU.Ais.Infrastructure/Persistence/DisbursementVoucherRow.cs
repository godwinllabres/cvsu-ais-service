using CvSU.Ais.Domain.Disbursement;
using CvSU.Ais.Domain.Funds;

namespace CvSU.Ais.Infrastructure.Persistence;

/// <summary>Disbursement Voucher as stored. Mutable (it transitions), unlike the
/// ledger rows. Enum-valued fields persist as readable strings.</summary>
public sealed class DisbursementVoucherRow
{
    public string Name { get; set; } = default!;
    public string Encoder { get; set; } = default!;
    public decimal Amount { get; set; }
    public decimal TaxWithheld { get; set; }
    public DvType DvType { get; set; }
    public string FundingSourceCode { get; set; } = default!;
    public string Lifecycle { get; set; } = default!;
    public string Status { get; set; } = default!;
    public string? ApprovedBy { get; set; }
    public string? ApprovedForPaymentBy { get; set; }
    // Certifications: each flag plus its audit trail (who certified, when).
    public bool BudgetCertified { get; set; }
    public string? BudgetCertifiedBy { get; set; }
    public DateTime? BudgetCertifiedAt { get; set; }

    public bool InternalAuditConfirmed { get; set; }
    public string? InternalAuditConfirmedBy { get; set; }
    public DateTime? InternalAuditConfirmedAt { get; set; }

    public bool EndUserConfirmed { get; set; }
    public string? EndUserConfirmedBy { get; set; }
    public DateTime? EndUserConfirmedAt { get; set; }

    public bool AccountantSigned { get; set; }
    public string? AccountantSignedBy { get; set; }
    public DateTime? AccountantSignedAt { get; set; }

    public bool SupplyPropertySignedOff { get; set; }
    public string? SupplyPropertySignedOffBy { get; set; }
    public DateTime? SupplyPropertySignedOffAt { get; set; }

    // The UACS budget line the DV charges (nullable until the encoder fills it in).
    public string? PapCode { get; set; }
    public string? LocationCode { get; set; }
    public ExpenseClass? ExpenseClass { get; set; }
    public string? ObjectAccountCode { get; set; }

    // The gapless control number stamped at approval (DV-CN-<cluster>-YYYY-#####).
    public string? ControlNumber { get; set; }

    // How the DV was settled; PaymentReference is unique per method across all DVs.
    public DvPaymentMethod? PaymentMethod { get; set; }
    public string? PaymentReference { get; set; }
}
