using CvSU.Ais.Application.Abstractions;

namespace CvSU.Ais.Application.Payments;

// ─── LDDAP-ADA DTOs ─────────────────────────────────────────────────────────

public sealed record LddapAdaItemDto(
    string DvReference,
    string PayeeName,
    string? PayeeAccountNumber,
    string? BankName,
    decimal NetAmount);

public sealed record LddapAdaView(
    string Name,
    DateOnly PeriodFrom,
    DateOnly PeriodTo,
    string ApprovalStatus,
    decimal TotalAmount,
    int TotalPayees);

public sealed record LddapAdaDetailView(
    string Name,
    DateOnly PeriodFrom,
    DateOnly PeriodTo,
    string? FundCluster,
    string BankName,
    string BankAccountNumber,
    decimal TotalAmount,
    int TotalPayees,
    string ApprovalStatus,
    DateOnly? TransmittedDate,
    string? Remarks,
    IReadOnlyList<LddapAdaItemDto> Items);

public sealed record CreateLddapAdaCommand(
    DateOnly PeriodFrom,
    DateOnly PeriodTo,
    string? FundCluster,
    string BankName,
    string BankAccountNumber,
    IReadOnlyList<LddapAdaItemDto> Items,
    string? Remarks);

// ─── DV Transmittal DTOs ─────────────────────────────────────────────────────

public sealed record DvTransmittalItemDto(
    string DvReference,
    decimal DvAmount,
    string? Remarks);

public sealed record DvTransmittalView(
    string Name,
    DateOnly TransmittalDate,
    string TransmittingOfficer,
    string Status,
    decimal TotalAmount);

public sealed record DvTransmittalDetailView(
    string Name,
    DateOnly TransmittalDate,
    string TransmittingOfficer,
    string ReceivingCashier,
    string? AccountantName,
    bool AccountantSignatureConfirmed,
    decimal TotalAmount,
    int TotalDvCount,
    string Status,
    string? ReceivedByCashier,
    DateOnly? ReceivedDate,
    string? Remarks,
    IReadOnlyList<DvTransmittalItemDto> Items);

public sealed record CreateDvTransmittalCommand(
    DateOnly TransmittalDate,
    string TransmittingOfficer,
    string ReceivingCashier,
    IReadOnlyList<DvTransmittalItemDto> Items,
    string? Remarks);

// ─── Audit Intake DTOs ───────────────────────────────────────────────────────

public sealed record AuditIntakeView(
    string Name,
    string DisbursementVoucherName,
    string Status,
    string AuditResult);

public sealed record AuditIntakeDetailView(
    string Name,
    string DisbursementVoucherName,
    DateTime? ReceivedTimestamp,
    DateTime? RecordedTimestamp,
    string AuditResult,
    string? Findings,
    DateTime? ReleasedTimestamp,
    string? ReleasedTo,
    string Status);

public sealed record CreateAuditIntakeCommand(
    string DisbursementVoucherName,
    DateTime ReceivedTimestamp);

// ─── LDDAP-ADA Service ───────────────────────────────────────────────────────

/// <summary>Orchestrates LDDAP-ADA creation and approval-workflow transitions.</summary>
public sealed class LddapAdaService(ILddapAdaRepository repo)
{
    // Allowed predecessor states per target — the only path status may change.
    // "Transmitted" and "Rejected" are terminal and appear in no value list,
    // so they can never be reverted.
    private static readonly IReadOnlyDictionary<string, string[]> AllowedFrom =
        new Dictionary<string, string[]>
        {
            ["Approved"] = ["Draft"],
            ["Transmitted"] = ["Approved"],
            ["Rejected"] = ["Draft", "Approved"],
        };

    public async Task<LddapAdaView> CreateAsync(CreateLddapAdaCommand cmd, CancellationToken ct = default)
    {
        if (!cmd.Items.Any())
            throw new InvalidOperationException("LDDAP-ADA must have at least one item.");
        if (cmd.Items.Any(i => i.NetAmount <= 0))
            throw new InvalidOperationException("Each LDDAP-ADA item amount must be greater than zero.");
        if (cmd.Items.Sum(i => i.NetAmount) <= 0)
            throw new InvalidOperationException("LDDAP-ADA total amount must be greater than zero.");

        return await repo.AddAsync(cmd, ct);
    }

    public Task<IReadOnlyList<LddapAdaView>> ListAsync(CancellationToken ct = default) =>
        repo.ListAsync(ct);

    public async Task<LddapAdaDetailView> GetAsync(string name, CancellationToken ct = default)
    {
        var detail = await repo.GetAsync(name, ct);
        return detail ?? throw new KeyNotFoundException($"LDDAP-ADA '{name}' not found.");
    }

    public async Task<LddapAdaView> ApproveAsync(string name, CancellationToken ct = default)
    {
        await EnsureLegalTransitionAsync(name, "Approved", ct);
        await repo.UpdateStatusAsync(name, "Approved", transmittedDate: null, ct);
        var detail = await repo.GetAsync(name, ct)
            ?? throw new KeyNotFoundException($"LDDAP-ADA '{name}' not found.");
        return ToView(detail);
    }

    public async Task<LddapAdaView> TransmitAsync(string name, DateOnly transmittedDate, CancellationToken ct = default)
    {
        await EnsureLegalTransitionAsync(name, "Transmitted", ct);
        await repo.UpdateStatusAsync(name, "Transmitted", transmittedDate, ct);
        var detail = await repo.GetAsync(name, ct)
            ?? throw new KeyNotFoundException($"LDDAP-ADA '{name}' not found.");
        return ToView(detail);
    }

    public async Task<LddapAdaView> CancelAsync(string name, CancellationToken ct = default)
    {
        await EnsureLegalTransitionAsync(name, "Rejected", ct);
        await repo.UpdateStatusAsync(name, "Rejected", transmittedDate: null, ct);
        var detail = await repo.GetAsync(name, ct)
            ?? throw new KeyNotFoundException($"LDDAP-ADA '{name}' not found.");
        return ToView(detail);
    }

    private async Task EnsureLegalTransitionAsync(string name, string target, CancellationToken ct)
    {
        var detail = await repo.GetAsync(name, ct)
            ?? throw new KeyNotFoundException($"LDDAP-ADA '{name}' not found.");
        var allowed = AllowedFrom[target];
        if (!allowed.Contains(detail.ApprovalStatus))
            throw new InvalidOperationException(
                $"LDDAP-ADA '{name}' cannot move to '{target}' from '{detail.ApprovalStatus}'. " +
                $"Legal predecessor states: {string.Join(", ", allowed)}.");
    }

    private static LddapAdaView ToView(LddapAdaDetailView d) =>
        new(d.Name, d.PeriodFrom, d.PeriodTo, d.ApprovalStatus, d.TotalAmount, d.TotalPayees);
}

// ─── DV Transmittal Service ──────────────────────────────────────────────────

/// <summary>Orchestrates DV Transmittal creation and status transitions.</summary>
public sealed class DvTransmittalService(IDvTransmittalRepository repo)
{
    // Allowed predecessor states per target — Draft → Transmitted →
    // ReceivedByCashier → Completed. "Completed" is terminal and appears in no
    // value list, so a completed transmittal can never be reverted.
    private static readonly IReadOnlyDictionary<string, string[]> AllowedFrom =
        new Dictionary<string, string[]>
        {
            ["Transmitted"] = ["Draft"],
            ["ReceivedByCashier"] = ["Transmitted"],
            ["Completed"] = ["ReceivedByCashier"],
        };

    public async Task<DvTransmittalView> CreateAsync(CreateDvTransmittalCommand cmd, CancellationToken ct = default)
    {
        if (!cmd.Items.Any())
            throw new InvalidOperationException("DV Transmittal must have at least one item.");
        if (cmd.Items.Any(i => i.DvAmount <= 0))
            throw new InvalidOperationException("Each DV Transmittal item amount must be greater than zero.");

        return await repo.AddAsync(cmd, ct);
    }

    public Task<IReadOnlyList<DvTransmittalView>> ListAsync(CancellationToken ct = default) =>
        repo.ListAsync(ct);

    public async Task<DvTransmittalDetailView> GetAsync(string name, CancellationToken ct = default)
    {
        var detail = await repo.GetAsync(name, ct);
        return detail ?? throw new KeyNotFoundException($"DV Transmittal '{name}' not found.");
    }

    public async Task<DvTransmittalView> TransmitAsync(string name, CancellationToken ct = default)
    {
        await EnsureLegalTransitionAsync(name, "Transmitted", ct);
        await repo.UpdateStatusAsync(name, "Transmitted", receivedBy: null, receivedDate: null, ct);
        var detail = await repo.GetAsync(name, ct)
            ?? throw new KeyNotFoundException($"DV Transmittal '{name}' not found.");
        return ToView(detail);
    }

    public async Task<DvTransmittalView> ReceiveAsync(string name, string receiverName, DateOnly receivedDate, CancellationToken ct = default)
    {
        await EnsureLegalTransitionAsync(name, "ReceivedByCashier", ct);
        await repo.UpdateStatusAsync(name, "ReceivedByCashier", receiverName, receivedDate, ct);
        var detail = await repo.GetAsync(name, ct)
            ?? throw new KeyNotFoundException($"DV Transmittal '{name}' not found.");
        return ToView(detail);
    }

    public async Task<DvTransmittalView> CompleteAsync(string name, CancellationToken ct = default)
    {
        await EnsureLegalTransitionAsync(name, "Completed", ct);
        await repo.UpdateStatusAsync(name, "Completed", receivedBy: null, receivedDate: null, ct);
        var detail = await repo.GetAsync(name, ct)
            ?? throw new KeyNotFoundException($"DV Transmittal '{name}' not found.");
        return ToView(detail);
    }

    private async Task EnsureLegalTransitionAsync(string name, string target, CancellationToken ct)
    {
        var detail = await repo.GetAsync(name, ct)
            ?? throw new KeyNotFoundException($"DV Transmittal '{name}' not found.");
        var allowed = AllowedFrom[target];
        if (!allowed.Contains(detail.Status))
            throw new InvalidOperationException(
                $"DV Transmittal '{name}' cannot move to '{target}' from '{detail.Status}'. " +
                $"Legal predecessor states: {string.Join(", ", allowed)}.");
    }

    private static DvTransmittalView ToView(DvTransmittalDetailView d) =>
        new(d.Name, d.TransmittalDate, d.TransmittingOfficer, d.Status, d.TotalAmount);
}

// ─── Audit Intake Service ────────────────────────────────────────────────────

/// <summary>Orchestrates Audit Intake creation and audit-lifecycle transitions.</summary>
public sealed class AuditIntakeService(IAuditIntakeRepository repo)
{
    // Allowed predecessor states per target — Received → Recorded → Audited →
    // Released. "Released" is terminal and appears in no value list, so a
    // released intake can never be reverted.
    private static readonly IReadOnlyDictionary<string, string[]> AllowedFrom =
        new Dictionary<string, string[]>
        {
            ["Recorded"] = ["Received"],
            ["Audited"] = ["Recorded"],
            ["Released"] = ["Audited"],
        };

    public async Task<AuditIntakeView> CreateAsync(CreateAuditIntakeCommand cmd, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(cmd.DisbursementVoucherName))
            throw new InvalidOperationException("Audit Intake must reference a disbursement voucher.");

        return await repo.AddAsync(cmd, ct);
    }

    public Task<IReadOnlyList<AuditIntakeView>> ListAsync(CancellationToken ct = default) =>
        repo.ListAsync(ct);

    public async Task<AuditIntakeDetailView> GetAsync(string name, CancellationToken ct = default)
    {
        var detail = await repo.GetAsync(name, ct);
        return detail ?? throw new KeyNotFoundException($"Audit Intake '{name}' not found.");
    }

    public async Task<AuditIntakeView> RecordAsync(string name, CancellationToken ct = default)
    {
        await EnsureLegalTransitionAsync(name, "Recorded", ct);
        await repo.UpdateAsync(name, "Recorded",
            auditResult: null, findings: null,
            releasedTimestamp: null, releasedTo: null, ct);
        var detail = await repo.GetAsync(name, ct)
            ?? throw new KeyNotFoundException($"Audit Intake '{name}' not found.");
        return ToView(detail);
    }

    public async Task<AuditIntakeView> AuditAsync(string name, string result, string? findings, CancellationToken ct = default)
    {
        await EnsureLegalTransitionAsync(name, "Audited", ct);
        await repo.UpdateAsync(name, "Audited",
            auditResult: result, findings: findings,
            releasedTimestamp: null, releasedTo: null, ct);
        var detail = await repo.GetAsync(name, ct)
            ?? throw new KeyNotFoundException($"Audit Intake '{name}' not found.");
        return ToView(detail);
    }

    public async Task<AuditIntakeView> ReleaseAsync(string name, string releasedTo, CancellationToken ct = default)
    {
        await EnsureLegalTransitionAsync(name, "Released", ct);
        await repo.UpdateAsync(name, "Released",
            auditResult: null, findings: null,
            releasedTimestamp: DateTime.UtcNow, releasedTo: releasedTo, ct);
        var detail = await repo.GetAsync(name, ct)
            ?? throw new KeyNotFoundException($"Audit Intake '{name}' not found.");
        return ToView(detail);
    }

    private async Task EnsureLegalTransitionAsync(string name, string target, CancellationToken ct)
    {
        var detail = await repo.GetAsync(name, ct)
            ?? throw new KeyNotFoundException($"Audit Intake '{name}' not found.");
        var allowed = AllowedFrom[target];
        if (!allowed.Contains(detail.Status))
            throw new InvalidOperationException(
                $"Audit Intake '{name}' cannot move to '{target}' from '{detail.Status}'. " +
                $"Legal predecessor states: {string.Join(", ", allowed)}.");
    }

    private static AuditIntakeView ToView(AuditIntakeDetailView d) =>
        new(d.Name, d.DisbursementVoucherName, d.Status, d.AuditResult);
}
