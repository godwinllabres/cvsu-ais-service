using CvSU.Ais.Application.Abstractions;
using CvSU.Ais.Application.CashAdvances;
using CvSU.Ais.Application.DisbursementVouchers;
using CvSU.Ais.Application.Exports;
using CvSU.Ais.Application.JournalEntries;
using CvSU.Ais.Application.Obligations;
using CvSU.Ais.Application.Payroll;
using CvSU.Ais.Domain.Disbursement;
using CvSU.Ais.Domain.Ledgers;

namespace CvSU.Ais.Application.Tests;

// ─── Shared infrastructure fakes ─────────────────────────────────────────────

/// <summary>Runs the supplied delegate inline (no real transaction), so the
/// service's transactional bodies execute against the in-memory fakes.</summary>
internal sealed class FakeUnitOfWork : IUnitOfWork
{
    public Task<T> ExecuteInTransactionAsync<T>(
        Func<CancellationToken, Task<T>> action, CancellationToken cancellationToken = default) =>
        action(cancellationToken);
}

/// <summary>Records every batch appended, so tests can assert what (and how many
/// balanced batches) the service posted.</summary>
internal sealed class FakeGeneralLedger : IGeneralLedger
{
    public List<GlPostingBatch> Batches { get; } = [];

    public Task AppendBatchAsync(GlPostingBatch batch, CancellationToken cancellationToken = default)
    {
        Batches.Add(batch);
        return Task.CompletedTask;
    }
}

// ─── Journal Entry fakes ─────────────────────────────────────────────────────

internal sealed class FakeJournalEntryRepository : IJournalEntryRepository
{
    public JournalEntryDetailView? Detail { get; set; }
    public string? LastStatus { get; private set; }
    public string? LastGlRef { get; private set; }

    public Task<JournalEntryDetailView?> GetAsync(string name, CancellationToken ct) =>
        Task.FromResult(Detail);

    public Task UpdateStatusAsync(string name, string newStatus, string? approvedBy, CancellationToken ct)
    {
        LastStatus = newStatus;
        return Task.CompletedTask;
    }

    public Task SetGlReferenceAsync(string name, string glRef, CancellationToken ct)
    {
        LastGlRef = glRef;
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<JournalEntryView>> ListAsync(CancellationToken ct) =>
        throw new NotImplementedException();

    public Task<JournalEntryView> AddAsync(CreateJournalEntryCommand command, CancellationToken ct) =>
        throw new NotImplementedException();
}

// ─── Obligation Request fakes ────────────────────────────────────────────────

internal sealed class FakeObligationRequestRepository : IObligationRequestRepository
{
    public OrsDetailView? Detail { get; set; }
    public List<string> StatusUpdates { get; } = [];

    public Task<OrsDetailView?> GetAsync(string name, CancellationToken ct) =>
        Task.FromResult(Detail);

    public Task UpdateStatusAsync(string name, string newStatus, CancellationToken ct)
    {
        StatusUpdates.Add(newStatus);
        return Task.CompletedTask;
    }

    public Task<OrsView> AddAsync(CreateOrsCommand command, CancellationToken ct) =>
        Task.FromResult(new OrsView(
            "ORS-TEST", command.PostingDate, command.RequestingUnit, command.Amount, "Draft"));

    public Task<IReadOnlyList<OrsView>> ListAsync(CancellationToken ct) =>
        throw new NotImplementedException();
}

/// <summary>Budget ledger stub. FundVerify tests here are only expected to fail at
/// the transition guard (before the ledger is touched), so these throw if reached.</summary>
internal sealed class FakeBudgetLedger : IBudgetLedger
{
    public Task<AppropriationSnapshot?> LockAppropriationAsync(string appropriationId, CancellationToken cancellationToken = default) =>
        throw new NotImplementedException();

    public Task<AllotmentSnapshot?> LockAllotmentAsync(string allotmentId, CancellationToken cancellationToken = default) =>
        throw new NotImplementedException();

    public Task<IReadOnlyList<AppropriationBalance>> ListAppropriationsAsync(CancellationToken cancellationToken = default) =>
        throw new NotImplementedException();

    public Task AppendAsync(BudgetLedgerEntry entry, CancellationToken cancellationToken = default) =>
        throw new NotImplementedException();
}

// ─── Payroll fakes ───────────────────────────────────────────────────────────

internal sealed class FakePayrollEntryRepository : IPayrollEntryRepository
{
    public PayrollEntryDetailView? Detail { get; set; }
    public List<string> StatusUpdates { get; } = [];
    public string? LastGlRef { get; private set; }

    public Task<PayrollEntryDetailView?> GetAsync(string name, CancellationToken cancellationToken = default) =>
        Task.FromResult(Detail);

    public Task UpdateStatusAsync(string name, string status, CancellationToken cancellationToken = default)
    {
        StatusUpdates.Add(status);
        return Task.CompletedTask;
    }

    public Task SetGlReferenceAsync(string name, string glRef, CancellationToken cancellationToken = default)
    {
        LastGlRef = glRef;
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<PayrollEntryView>> ListAsync(CancellationToken cancellationToken = default) =>
        throw new NotImplementedException();

    public Task<PayrollEntryView> AddAsync(CreatePayrollEntryCommand command, CancellationToken cancellationToken = default) =>
        throw new NotImplementedException();
}

// ─── Cash Advance fakes ──────────────────────────────────────────────────────

internal sealed class FakeCashAdvanceRepository : ICashAdvanceRepository
{
    public CashAdvanceDetailView? Detail { get; set; }
    public List<string> StatusUpdates { get; } = [];
    public string? AddedName { get; set; } = "CA-TEST";

    public Task<CashAdvanceDetailView?> GetAsync(string name, CancellationToken cancellationToken = default) =>
        Task.FromResult(Detail);

    public Task<string> AddAsync(CreateCashAdvanceCommand command, CancellationToken cancellationToken = default) =>
        Task.FromResult(AddedName!);

    public Task UpdateStatusAsync(string name, string status, CancellationToken cancellationToken = default)
    {
        StatusUpdates.Add(status);
        return Task.CompletedTask;
    }

    public Task SetGlReferenceAsync(string name, string glRef, CancellationToken cancellationToken = default) =>
        Task.CompletedTask;

    public Task<IReadOnlyList<CashAdvanceView>> ListAsync(CancellationToken cancellationToken = default) =>
        throw new NotImplementedException();
}

internal sealed class FakeLiquidationReportRepository : ILiquidationReportRepository
{
    public LiquidationReportDetailView? Detail { get; set; }
    public List<string> StatusUpdates { get; } = [];

    public Task<LiquidationReportDetailView?> GetAsync(string name, CancellationToken cancellationToken = default) =>
        Task.FromResult(Detail);

    public Task UpdateStatusAsync(string name, string status, CancellationToken cancellationToken = default)
    {
        StatusUpdates.Add(status);
        return Task.CompletedTask;
    }

    public Task SetGlReferenceAsync(string name, string glRef, CancellationToken cancellationToken = default) =>
        Task.CompletedTask;

    public Task UpdateCashAdvanceLiquidatedAsync(string cashAdvanceName, decimal totalLiquidated, CancellationToken cancellationToken = default) =>
        Task.CompletedTask;

    public Task<string> AddAsync(CreateLiquidationReportCommand command, CancellationToken cancellationToken = default) =>
        throw new NotImplementedException();

    public Task<IReadOnlyList<LiquidationReportView>> ListAsync(CancellationToken cancellationToken = default) =>
        throw new NotImplementedException();
}

// ─── Export fakes ────────────────────────────────────────────────────────────

internal sealed class FakeFindesExportRepository : IFindesExportRepository
{
    public FindesExportDetailView? Added { get; private set; }

    public Task AddAsync(FindesExportDetailView detail, CancellationToken cancellationToken = default)
    {
        Added = detail;
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<FindesExportView>> ListAsync(CancellationToken cancellationToken = default) =>
        throw new NotImplementedException();

    public Task<FindesExportDetailView?> GetAsync(string name, CancellationToken cancellationToken = default) =>
        throw new NotImplementedException();

    public Task UpdateStatusAsync(
        string name, string status, string? reviewedBy, DateTime? reviewedOn,
        string? generatedBy, DateTime? generatedOn, CancellationToken cancellationToken = default) =>
        throw new NotImplementedException();
}

internal sealed class FakeBankCollectionReportRepository : IBankCollectionReportRepository
{
    public BankCollectionReportDetailView? Added { get; private set; }

    public Task AddAsync(BankCollectionReportDetailView detail, CancellationToken cancellationToken = default)
    {
        Added = detail;
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<BankCollectionReportView>> ListAsync(CancellationToken cancellationToken = default) =>
        throw new NotImplementedException();

    public Task<BankCollectionReportDetailView?> GetAsync(string name, CancellationToken cancellationToken = default) =>
        throw new NotImplementedException();

    public Task UpdateStatusAsync(string name, string status, CancellationToken cancellationToken = default) =>
        throw new NotImplementedException();
}

internal sealed class FakeDisbursementVoucherRepository : IDisbursementVoucherRepository
{
    private readonly Dictionary<string, DisbursementVoucher> _byName = new(StringComparer.Ordinal);

    public void Seed(DisbursementVoucher voucher) => _byName[voucher.Name] = voucher;

    public Task<DisbursementVoucher?> FindAsync(string name, CancellationToken cancellationToken = default) =>
        Task.FromResult(_byName.TryGetValue(name, out var dv) ? dv : null);

    public Task<IReadOnlyList<DvStateView>> ListAsync(CancellationToken cancellationToken = default) =>
        throw new NotImplementedException();

    public Task AddAsync(DisbursementVoucher voucher, CancellationToken cancellationToken = default) =>
        throw new NotImplementedException();

    public Task UpdateAsync(DisbursementVoucher voucher, CancellationToken cancellationToken = default) =>
        throw new NotImplementedException();
}
