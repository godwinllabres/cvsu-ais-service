using CvSU.Ais.Domain.Ledgers;
using CvSU.Ais.Infrastructure.Persistence;

namespace CvSU.Ais.Infrastructure.Mapping;

/// <summary>Maps immutable domain ledger entries to their storage rows. Only the
/// insert direction is needed — the ledgers are append-only, so rows are never
/// rehydrated into domain entries for mutation.</summary>
internal static class LedgerMappings
{
    public static GeneralLedgerRow ToRow(this GeneralLedgerEntry e) => new()
    {
        PostingDate = e.PostingDate,
        FiscalYear = e.FiscalYear,
        Account = e.Account,
        Debit = e.Debit.Amount,
        Credit = e.Credit.Amount,
        VoucherDoctype = e.VoucherDoctype,
        VoucherNo = e.VoucherNo,
        Remarks = e.Remarks,
    };

    public static BudgetLedgerRow ToRow(this BudgetLedgerEntry e) => new()
    {
        PostingDate = e.PostingDate,
        FiscalYear = e.FiscalYear,
        FundingSourceCode = e.Uacs.FundingSource.Code,
        PapCode = e.Uacs.PapCode,
        LocationCode = e.Uacs.LocationCode,
        ExpenseClass = e.Uacs.ExpenseClass,
        ObjectAccountCode = e.Uacs.ObjectAccountCode,
        EntryType = e.EntryType,
        Debit = e.Debit.Amount,
        Credit = e.Credit.Amount,
        VoucherDoctype = e.VoucherDoctype,
        VoucherNo = e.VoucherNo,
        AppropriationId = e.AppropriationId,
        AllotmentId = e.AllotmentId,
    };
}
