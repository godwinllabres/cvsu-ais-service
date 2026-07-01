using CvSU.Ais.Application.Abstractions;

namespace CvSU.Ais.Application.Exports;

// ── DTOs ────────────────────────────────────────────────────────────────────

/// <param name="DvReference">The DV Name this export line pays.</param>
/// <param name="Amount">
/// The amount the export claims to remit for this DV. Defaults to 0, in which case
/// the referenced DV's own amount is used when computing export totals.
/// </param>
public sealed record FindesExportLineDto(string DvReference, decimal Amount = 0m);

public sealed record FindesExportView(
    string Name,
    DateOnly ExportDate,
    decimal DvTotalAmount,
    decimal ExportTotalAmount,
    decimal Variance,
    bool VarianceAcceptable,
    string ApprovalStatus);

public sealed record FindesExportDetailView(
    string Name,
    string? ExportBatch,
    DateOnly ExportDate,
    decimal DvTotalAmount,
    decimal ExportTotalAmount,
    decimal Variance,
    bool VarianceAcceptable,
    string ApprovalStatus,
    string? ReviewedBy,
    DateTime? ReviewedOn,
    string? GeneratedBy,
    DateTime? GeneratedOn,
    string? Remarks,
    IReadOnlyList<FindesExportLineDto> Lines);

public sealed record CreateFindesExportCommand(
    DateOnly ExportDate,
    IReadOnlyList<FindesExportLineDto> Lines,
    string? Remarks = null);

// ── Service ──────────────────────────────────────────────────────────────────

public sealed class FindesExportService(
    IFindesExportRepository repo,
    IDisbursementVoucherRepository disbursementVouchers)
{
    public async Task<FindesExportView> CreateAsync(
        CreateFindesExportCommand command,
        CancellationToken cancellationToken = default)
    {
        var raw = $"FINDES-{command.ExportDate:yyyyMMdd}-{Guid.NewGuid():N}".ToUpperInvariant();
        var name = raw[..Math.Min(140, raw.Length)];

        // Reconcile the export against the DVs it references: DvTotalAmount is the sum of
        // each referenced DV's actual amount; ExportTotalAmount is what the export claims to
        // remit (falling back to the DV amount when a line supplies none). A DV that cannot
        // be found contributes zero rather than aborting the batch.
        var dvTotal = 0m;
        var exportTotal = 0m;
        foreach (var line in command.Lines)
        {
            var dv = await disbursementVouchers.FindAsync(line.DvReference, cancellationToken);
            var dvAmount = dv?.Amount.Amount ?? 0m;
            dvTotal += dvAmount;
            exportTotal += line.Amount != 0m ? line.Amount : dvAmount;
        }

        var variance = exportTotal - dvTotal;
        var varianceAcceptable = Math.Abs(variance) <= 0.01m;

        var detail = new FindesExportDetailView(
            Name: name,
            ExportBatch: null,
            ExportDate: command.ExportDate,
            DvTotalAmount: dvTotal,
            ExportTotalAmount: exportTotal,
            Variance: variance,
            VarianceAcceptable: varianceAcceptable,
            ApprovalStatus: "Draft",
            ReviewedBy: null,
            ReviewedOn: null,
            GeneratedBy: null,
            GeneratedOn: null,
            Remarks: command.Remarks,
            Lines: command.Lines);

        await repo.AddAsync(detail, cancellationToken);

        return new FindesExportView(
            name, command.ExportDate, dvTotal, exportTotal, variance, varianceAcceptable, "Draft");
    }

    public Task<IReadOnlyList<FindesExportView>> ListAsync(CancellationToken cancellationToken = default) =>
        repo.ListAsync(cancellationToken);

    public async Task<FindesExportDetailView> GetAsync(string name, CancellationToken cancellationToken = default) =>
        await repo.GetAsync(name, cancellationToken)
            ?? throw new KeyNotFoundException($"FINDES export '{name}' not found.");

    public async Task<FindesExportView> ApproveAsync(
        string name,
        string reviewedBy,
        CancellationToken cancellationToken = default)
    {
        var detail = await GetAsync(name, cancellationToken);
        if (detail.ApprovalStatus != "Draft")
            throw new InvalidOperationException(
                $"Cannot approve FINDES export '{name}' from status '{detail.ApprovalStatus}'; expected Draft.");

        await repo.UpdateStatusAsync(name, "Approved", reviewedBy, DateTime.UtcNow, null, null, cancellationToken);
        return new FindesExportView(
            detail.Name, detail.ExportDate, detail.DvTotalAmount, detail.ExportTotalAmount,
            detail.Variance, detail.VarianceAcceptable, "Approved");
    }

    public async Task<FindesExportView> ExportAsync(
        string name,
        string generatedBy,
        CancellationToken cancellationToken = default)
    {
        var detail = await GetAsync(name, cancellationToken);
        if (detail.ApprovalStatus != "Approved")
            throw new InvalidOperationException(
                $"Cannot export FINDES export '{name}' from status '{detail.ApprovalStatus}'; expected Approved.");

        await repo.UpdateStatusAsync(name, "Exported", detail.ReviewedBy, detail.ReviewedOn, generatedBy, DateTime.UtcNow, cancellationToken);
        return new FindesExportView(
            detail.Name, detail.ExportDate, detail.DvTotalAmount, detail.ExportTotalAmount,
            detail.Variance, detail.VarianceAcceptable, "Exported");
    }

    public async Task<FindesExportView> RejectAsync(
        string name,
        CancellationToken cancellationToken = default)
    {
        var detail = await GetAsync(name, cancellationToken);
        // A batch can be rejected while still pending (Draft/Approved) but not once it has
        // reached a terminal state (already Exported or Rejected).
        if (detail.ApprovalStatus is not ("Draft" or "Approved"))
            throw new InvalidOperationException(
                $"Cannot reject FINDES export '{name}' from status '{detail.ApprovalStatus}'; expected Draft or Approved.");

        await repo.UpdateStatusAsync(name, "Rejected", detail.ReviewedBy, detail.ReviewedOn, null, null, cancellationToken);
        return new FindesExportView(
            detail.Name, detail.ExportDate, detail.DvTotalAmount, detail.ExportTotalAmount,
            detail.Variance, detail.VarianceAcceptable, "Rejected");
    }
}

// ── Bank Collection DTOs & Service ──────────────────────────────────────────

public sealed record BankCollectionLineDto(
    string RefNo,
    string? LbpRefNo,
    decimal Amount,
    bool IsMatched,
    string? MatchedOrName,
    string? Remarks);

public sealed record BankCollectionReportView(
    string Name,
    DateOnly ReportDate,
    string ReconciliationStatus,
    decimal TotalAmount,
    int ExceptionsCount);

public sealed record BankCollectionReportDetailView(
    string Name,
    DateOnly ReportDate,
    string ReconciliationStatus,
    int TotalLines,
    decimal TotalAmount,
    int ExceptionsCount,
    string? Remarks,
    IReadOnlyList<BankCollectionLineDto> Lines);

public sealed record CreateBankCollectionReportCommand(
    DateOnly ReportDate,
    IReadOnlyList<BankCollectionLineDto> Lines,
    string? Remarks = null);

public sealed class BankCollectionReportService(IBankCollectionReportRepository repo)
{
    public async Task<BankCollectionReportView> CreateAsync(
        CreateBankCollectionReportCommand command,
        CancellationToken cancellationToken = default)
    {
        var raw = $"BCR-{command.ReportDate:yyyyMMdd}-{Guid.NewGuid():N}".ToUpperInvariant();
        var name = raw[..Math.Min(140, raw.Length)];
        var total = command.Lines.Sum(l => l.Amount);
        var exceptions = command.Lines.Count(l => !l.IsMatched);

        var detail = new BankCollectionReportDetailView(
            Name: name,
            ReportDate: command.ReportDate,
            ReconciliationStatus: "Draft",
            TotalLines: command.Lines.Count,
            TotalAmount: total,
            ExceptionsCount: exceptions,
            Remarks: command.Remarks,
            Lines: command.Lines);

        await repo.AddAsync(detail, cancellationToken);

        return new BankCollectionReportView(name, command.ReportDate, "Draft", total, exceptions);
    }

    public Task<IReadOnlyList<BankCollectionReportView>> ListAsync(CancellationToken cancellationToken = default) =>
        repo.ListAsync(cancellationToken);

    public async Task<BankCollectionReportDetailView> GetAsync(
        string name,
        CancellationToken cancellationToken = default) =>
        await repo.GetAsync(name, cancellationToken)
            ?? throw new KeyNotFoundException($"Bank collection report '{name}' not found.");

    public async Task<BankCollectionReportView> ReconcileAsync(
        string name,
        CancellationToken cancellationToken = default)
    {
        var detail = await GetAsync(name, cancellationToken);
        var status = detail.ExceptionsCount == 0 ? "Reconciled" : "ReconciledWithExceptions";
        await repo.UpdateStatusAsync(name, status, cancellationToken);
        return new BankCollectionReportView(detail.Name, detail.ReportDate, status, detail.TotalAmount, detail.ExceptionsCount);
    }
}

// ── Push Token DTOs & Service ─────────────────────────────────────────────────

public sealed record PushTokenView(
    int Id,
    string UserId,
    string Platform,
    bool IsActive,
    DateTime RegisteredOn);

public sealed record RegisterPushTokenCommand(
    string UserId,
    string Token,
    string Platform);

public sealed class PushTokenService(IPushTokenRepository repo)
{
    public Task RegisterAsync(RegisterPushTokenCommand command, CancellationToken cancellationToken = default) =>
        repo.AddAsync(command, cancellationToken);

    public Task<IReadOnlyList<PushTokenView>> ListForUserAsync(
        string userId,
        CancellationToken cancellationToken = default) =>
        repo.ListForUserAsync(userId, cancellationToken);

    public Task DeactivateAsync(int id, CancellationToken cancellationToken = default) =>
        repo.DeactivateAsync(id, cancellationToken);
}
