// Scaffolded shared contracts (DTOs) for the MAUI desktop client (cvsu-ais-wind).
// Shapes are reconstructed from the client's usage (view models + XAML bindings).
// Reads use init-only records so System.Text.Json can deserialise API responses;
// request records are init-only so the client can build them with object initialisers.

using CvSU.Ais.Domain.Disbursement;
using CvSU.Ais.Domain.Funds;

namespace CvSU.Ais.Contracts;

// ── Disbursement Voucher ────────────────────────────────────────────────────────

/// <summary>Thin DV row for list views.</summary>
public sealed record DvStateView
{
    public string Name { get; init; } = "";
    public string? ControlNumber { get; init; }
    public string Lifecycle { get; init; } = "";
    public DvWorkflowStatus Status { get; init; }
    public string FundCluster { get; init; } = "";
    public string? ApprovedBy { get; init; }
    public string? ApprovedForPaymentBy { get; init; }
}

/// <summary>Full read model for a single DV — everything the detail pane shows.</summary>
public sealed record DvDetailView
{
    public string Name { get; init; } = "";
    public int FiscalYear { get; init; }
    public string Encoder { get; init; } = "";
    public decimal Amount { get; init; }
    public decimal TaxWithheld { get; init; }
    public decimal NetAmountPayable { get; init; }
    public DvType DvType { get; init; }
    public string Lifecycle { get; init; } = "";
    public DvWorkflowStatus Status { get; init; }
    public string? ControlNumber { get; init; }
    public string FundingSourceCode { get; init; } = "";
    public string FundClusterCode { get; init; } = "";
    public string FundClusterName { get; init; } = "";
    public string? PapCode { get; init; }
    public string? LocationCode { get; init; }
    public ExpenseClass? ExpenseClass { get; init; }
    public string? ObjectAccountCode { get; init; }
    public DvPaymentMethod? PaymentMethod { get; init; }
    public string? PaymentReference { get; init; }
    public string? ApprovedBy { get; init; }
    public string? ApprovedForPaymentBy { get; init; }

    // The API models certifications as plain booleans captured at encode time
    // (no per-officer actor/timestamp, no separate Supply/Property step). These map
    // 1:1 to the JSON the service serialises.
    public bool BudgetCertified { get; init; }
    public bool InternalAuditConfirmed { get; init; }
    public bool EndUserConfirmed { get; init; }
    public bool AccountantSigned { get; init; }

    // The UI binds to richer CertificationView rows (Done/By/At). We project the
    // service's booleans into that shape so the detail view's certification section
    // reflects real data. By/At are null because the service does not record them.
    public CertificationView Budget => CertificationView.From(BudgetCertified);
    public CertificationView InternalAudit => CertificationView.From(InternalAuditConfirmed);
    public CertificationView EndUser => CertificationView.From(EndUserConfirmed);
    public CertificationView Accountant => CertificationView.From(AccountantSigned);
    // The service has no Supply/Property certification; surface it as not-applicable/not-done.
    public CertificationView SupplyProperty => CertificationView.From(false);
}

/// <summary>One certification's recorded state. The service tracks only whether a
/// certification was made (a boolean); the actor (<see cref="By"/>) and time
/// (<see cref="At"/>) are not recorded today, so they are null.</summary>
public sealed record CertificationView
{
    public bool Done { get; init; }
    public string? By { get; init; }
    public DateTimeOffset? At { get; init; }

    public static CertificationView From(bool done) => new() { Done = done };
}

/// <summary>Inputs to create or re-encode a DV. The encoder is taken from X-User server-side.</summary>
public sealed record DvCreateRequest
{
    public int FiscalYear { get; set; }
    public decimal Amount { get; set; }
    public decimal TaxWithheld { get; set; }
    public DvType DvType { get; set; }
    public string FundingSourceCode { get; set; } = "";
    public string PapCode { get; set; } = "";
    public string LocationCode { get; set; } = "";
    public ExpenseClass ExpenseClass { get; set; }
    public string ObjectAccountCode { get; set; } = "";

    // Certifications captured at encode time (the service accepts these on create).
    public bool BudgetCertified { get; set; }
    public bool InternalAuditConfirmed { get; set; }
    public bool EndUserConfirmed { get; set; }
    public bool AccountantSigned { get; set; }
}

/// <summary>Records the payment instrument for a released DV.</summary>
public sealed record DvPaymentRequest
{
    public DvPaymentMethod Method { get; init; }
    public string Reference { get; init; } = "";
}

/// <summary>Asserts one certification by the acting officer.</summary>
public sealed record DvCertifyRequest
{
    public Certification Certification { get; init; }
}

// ── Budget registry ─────────────────────────────────────────────────────────────

public sealed record AppropriationView
{
    public string Id { get; init; } = "";
    public decimal FinalAppropriation { get; init; }
    public decimal Allotted { get; init; }
    public decimal Unallotted { get; init; }
}

public sealed record AllotmentView
{
    public string Id { get; init; } = "";
    public string AppropriationId { get; init; } = "";
    public decimal Amount { get; init; }
    public decimal Obligated { get; init; }
    public decimal Unobligated { get; init; }
}

public sealed record ObligationView
{
    public string Id { get; init; } = "";
    public string AllotmentId { get; init; } = "";
    public decimal Amount { get; init; }
    public decimal AllotmentUnobligatedBalance { get; init; }
}

public sealed record AppropriationCreateRequest
{
    public int FiscalYear { get; set; }
    public string FundingSourceCode { get; set; } = "";
    public string PapCode { get; set; } = "";
    public string LocationCode { get; set; } = "";
    public ExpenseClass ExpenseClass { get; set; }
    public string ObjectAccountCode { get; set; } = "";
    public decimal FinalAppropriation { get; set; }
}

/// <summary>A bare amount body for allot / obligate.</summary>
public sealed record AmountRequest(decimal Amount);

// ── Collections / Official Receipts ─────────────────────────────────────────────

public sealed record ReceiptView
{
    public string OrNumber { get; init; } = "";
    public string Payor { get; init; } = "";
    public decimal AmountPaid { get; init; }
    public string Mode { get; init; } = "";
    public string FundCluster { get; init; } = "";
    public DateTimeOffset ReceivedAtUtc { get; init; }
}

public sealed record RecordReceiptRequest
{
    public string IdempotencyKey { get; set; } = "";
    public string Payor { get; set; } = "";
    public decimal AmountPaid { get; set; }
    public string Mode { get; set; } = "";
    public string FundCluster { get; set; } = "";
    public DateTimeOffset ReceivedAtUtc { get; set; }
}

// ── Reports ─────────────────────────────────────────────────────────────────────

public sealed record BudgetRegistryRow
{
    public string FundClusterCode { get; init; } = "";
    public string ExpenseClass { get; init; } = "";
    public decimal Allotment { get; init; }
    public decimal Obligation { get; init; }
    public decimal Disbursement { get; init; }
    public decimal UnobligatedBalance => Allotment - Obligation;
    public decimal UnpaidObligation => Obligation - Disbursement;

    /// <summary>The expense class parsed back to the enum (the API serialises it as the
    /// enum name); used to group rows into the per-object sub-registries.</summary>
    public CvSU.Ais.Domain.Funds.ExpenseClass ExpenseClassValue =>
        Enum.TryParse<CvSU.Ais.Domain.Funds.ExpenseClass>(ExpenseClass, ignoreCase: true, out var v)
            ? v : CvSU.Ais.Domain.Funds.ExpenseClass.Mooe;
}

public sealed record BudgetRegistryTotals(decimal Allotment, decimal Obligation, decimal Disbursement)
{
    public decimal UnobligatedBalance => Allotment - Obligation;
    public decimal UnpaidObligation => Obligation - Disbursement;
}

public sealed record BudgetRegistrySection
{
    public string Registry { get; init; } = "";
    public IReadOnlyList<BudgetRegistryRow> Lines { get; init; } = [];
    public BudgetRegistryTotals Totals { get; init; } = new(0, 0, 0);
}

public sealed record BudgetRegistryReport
{
    public int FiscalYear { get; init; }
    public BudgetRegistrySection Raod { get; init; } = new();
    public BudgetRegistrySection Rbud { get; init; } = new();
}

public sealed record AppropriationAllotmentRow
{
    public string FundClusterCode { get; init; } = "";
    public string ExpenseClass { get; init; } = "";
    public decimal Appropriation { get; init; }
    public decimal Allotment { get; init; }
    public decimal UnallottedBalance => Appropriation - Allotment;

    /// <summary>The expense class parsed back to the enum (see <see cref="BudgetRegistryRow"/>).</summary>
    public CvSU.Ais.Domain.Funds.ExpenseClass ExpenseClassValue =>
        Enum.TryParse<CvSU.Ais.Domain.Funds.ExpenseClass>(ExpenseClass, ignoreCase: true, out var v)
            ? v : CvSU.Ais.Domain.Funds.ExpenseClass.Mooe;
}

public sealed record AppropriationAllotmentReport
{
    public int FiscalYear { get; init; }
    public IReadOnlyList<AppropriationAllotmentRow> Lines { get; init; } = [];
    public decimal TotalAppropriation { get; init; }
    public decimal TotalAllotment { get; init; }
    public decimal TotalUnallotted => TotalAppropriation - TotalAllotment;
}

public sealed record TrialBalanceRow
{
    public string Account { get; init; } = "";
    public decimal Debit { get; init; }
    public decimal Credit { get; init; }
}

public sealed record TrialBalanceReport
{
    public int FiscalYear { get; init; }
    public IReadOnlyList<TrialBalanceRow> Lines { get; init; } = [];
    public decimal TotalDebit { get; init; }
    public decimal TotalCredit { get; init; }
    public bool IsBalanced => Math.Abs(TotalDebit - TotalCredit) < 0.01m;
}

// ── Financial Statements (GAM/PPSAS) ─────────────────────────────────────────────

/// <summary>One financial-statement line: an RCA account and its amount on its natural side.</summary>
public sealed record FsLine
{
    public string Account { get; init; } = "";
    public decimal Amount { get; init; }
}

/// <summary>Statement of Financial Position: Assets = Liabilities + Net Assets/Equity.</summary>
public sealed record StatementOfFinancialPosition
{
    public IReadOnlyList<FsLine> Assets { get; init; } = [];
    public IReadOnlyList<FsLine> Liabilities { get; init; } = [];
    public IReadOnlyList<FsLine> Equity { get; init; } = [];
    public decimal TotalAssets { get; init; }
    public decimal TotalLiabilities { get; init; }
    public decimal TotalEquity { get; init; }
    public decimal TotalLiabilitiesAndEquity => TotalLiabilities + TotalEquity;
    public bool IsBalanced => Math.Abs(TotalAssets - TotalLiabilitiesAndEquity) < 0.01m;
}

/// <summary>Statement of Financial Performance: Revenue − Expenses = Surplus/(Deficit).</summary>
public sealed record StatementOfFinancialPerformance
{
    public IReadOnlyList<FsLine> Revenue { get; init; } = [];
    public IReadOnlyList<FsLine> Expenses { get; init; } = [];
    public decimal TotalRevenue { get; init; }
    public decimal TotalExpenses { get; init; }
    public decimal SurplusDeficit { get; init; }
}

/// <summary>Statement of Changes in Net Assets/Equity.</summary>
public sealed record StatementOfChangesInEquity
{
    public decimal BeginningEquity { get; init; }
    public decimal SurplusDeficit { get; init; }
    public decimal EndingEquity { get; init; }
}

/// <summary>Statement of Cash Flows (net movement in cash &amp; cash equivalents).</summary>
public sealed record StatementOfCashFlows
{
    public IReadOnlyList<FsLine> CashLines { get; init; } = [];
    public decimal NetCashFlow { get; init; }
}

/// <summary>The four General-Purpose Financial Statements for a fiscal year.</summary>
public sealed record FinancialStatementsReport
{
    public int FiscalYear { get; init; }
    public StatementOfFinancialPosition Position { get; init; } = new();
    public StatementOfFinancialPerformance Performance { get; init; } = new();
    public StatementOfChangesInEquity Changes { get; init; } = new();
    public StatementOfCashFlows CashFlows { get; init; } = new();
}

// ── FAR No. 5 / RROR (revenue) and FAR No. 4 (disbursements) ─────────────────────

/// <summary>One RROR/QRROR line: a revenue account and the amount recognised per quarter.</summary>
public sealed record RevenueByQuarterRow
{
    public string Account { get; init; } = "";
    public decimal Q1 { get; init; }
    public decimal Q2 { get; init; }
    public decimal Q3 { get; init; }
    public decimal Q4 { get; init; }
    public decimal Total => Q1 + Q2 + Q3 + Q4;
}

/// <summary>RROR / Quarterly Report of Revenue and Other Receipts (FAR No. 5).</summary>
public sealed record RevenueReport
{
    public int FiscalYear { get; init; }
    public IReadOnlyList<RevenueByQuarterRow> Lines { get; init; } = [];
    public decimal TotalQ1 { get; init; }
    public decimal TotalQ2 { get; init; }
    public decimal TotalQ3 { get; init; }
    public decimal TotalQ4 { get; init; }
    public decimal GrandTotal => TotalQ1 + TotalQ2 + TotalQ3 + TotalQ4;
}

/// <summary>Monthly Report of Disbursements (FAR No. 4): disbursements by allotment class
/// (PS / MOOE / Financial Expenses / Capital Outlays).</summary>
public sealed record MonthlyDisbursementReport
{
    public int FiscalYear { get; init; }
    public decimal Ps { get; init; }
    public decimal Mooe { get; init; }
    public decimal FinEx { get; init; }
    public decimal Co { get; init; }
    public decimal Total => Ps + Mooe + FinEx + Co;
}
