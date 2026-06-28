// Scaffolded contract enums for the MAUI desktop client (cvsu-ais-wind).
// These mirror the shapes the client binds against. They are declared in the
// CvSU.Ais.Domain.* namespaces because the client's source imports those
// namespaces while referencing only this Contracts assembly.

namespace CvSU.Ais.Domain.Funds
{
    /// <summary>UACS expense classification.</summary>
    public enum ExpenseClass
    {
        /// <summary>Personnel Services.</summary>
        Ps,

        /// <summary>Maintenance and Other Operating Expenses.</summary>
        Mooe,

        /// <summary>Financial Expenses.</summary>
        Fe,

        /// <summary>Capital Outlays.</summary>
        Co,
    }
}

namespace CvSU.Ais.Domain.Disbursement
{
    /// <summary>The lifecycle states of a disbursement voucher.</summary>
    public enum DvWorkflowStatus
    {
        Draft,
        IaAuditRequired,
        Submitted,
        Approved,
        ApprovedForPayment,
        Posted,
        Released,
        Closed,
        Rejected,
    }

    /// <summary>The kind of disbursement (drives which certifications apply).</summary>
    public enum DvType
    {
        Suppliers,
        CashAdvance,
    }

    /// <summary>How a released payment is settled.</summary>
    public enum DvPaymentMethod
    {
        Cheque,
        Ada,
        BankTransfer,
    }

    /// <summary>The per-officer certifications a DV may require before approval.</summary>
    public enum Certification
    {
        BudgetSufficiency,
        InternalAudit,
        EndUserAcceptance,
        AccountantSignature,
        SupplyPropertyInspection,
    }
}
