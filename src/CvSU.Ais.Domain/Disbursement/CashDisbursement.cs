using CvSU.Ais.Domain.Ledgers;

namespace CvSU.Ais.Domain.Disbursement;

/// <summary>The postings produced when a DV is released: the accrual-reversing
/// cash journal (DR payable / CR cash) and the budget-registry Disbursement entry.</summary>
public sealed record CashDisbursement(GlPostingBatch GeneralLedger, BudgetLedgerEntry BudgetEntry);
