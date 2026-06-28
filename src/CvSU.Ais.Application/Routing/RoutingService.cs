using CvSU.Ais.Application.Abstractions;

namespace CvSU.Ais.Application.Routing;

// ── DTOs ────────────────────────────────────────────────────────────────────

public sealed record RoutingTemplateStepDto(
    int StepOrder,
    string OfficeName,
    string? Role,
    int DurationDays,
    bool IsRequired);

public sealed record RoutingTemplateView(
    string Name,
    string TemplateName,
    string? DocumentType,
    IReadOnlyList<RoutingTemplateStepDto> Steps);

public sealed record CreateRoutingTemplateCommand(
    string Name,
    string TemplateName,
    string? DocumentType,
    string? Description,
    decimal? MinAmount,
    decimal? MaxAmount,
    IReadOnlyList<RoutingTemplateStepDto> Steps);

public sealed record RoutingSlipStepDto(
    int StepOrder,
    string OfficeName,
    string Status,
    DateTime? StartedOn,
    DateTime? CompletedOn,
    string? HandledBy,
    string? Remarks);

public sealed record RoutingSlipView(
    string Name,
    string RoutingTemplateName,
    string Status,
    string? ReferenceDoctype,
    string? ReferenceName,
    string? CurrentOffice);

public sealed record RoutingSlipDetailView(
    string Name,
    string RoutingTemplateName,
    string Status,
    string? ReferenceDoctype,
    string? ReferenceName,
    string? CurrentOffice,
    int CurrentStep,
    DateTime? StartedOn,
    DateTime? CompletedOn,
    IReadOnlyList<RoutingSlipStepDto> Steps);

public sealed record CreateRoutingSlipCommand(
    string RoutingTemplateName,
    string? ReferenceDoctype,
    string? ReferenceName);

public sealed record AttachmentRequirementView(
    int Id,
    string ReferenceDoctype,
    string? WorkflowState,
    string RequirementCode,
    string RequirementLabel,
    string ValidationMode,
    string? FilenameKeyword,
    bool IsEnabled);

public sealed record CreateAttachmentRequirementCommand(
    string ReferenceDoctype,
    string? WorkflowState,
    string RequirementCode,
    string RequirementLabel,
    string ValidationMode,
    string? FilenameKeyword);

// ── Services ─────────────────────────────────────────────────────────────────

/// <summary>CRUD for routing templates (named, re-usable step sequences).</summary>
public sealed class RoutingTemplateService(IRoutingTemplateRepository repo)
{
    public Task<RoutingTemplateView> CreateAsync(CreateRoutingTemplateCommand command, CancellationToken cancellationToken = default) =>
        repo.AddAsync(command, cancellationToken);

    public Task<IReadOnlyList<RoutingTemplateView>> ListAsync(CancellationToken cancellationToken = default) =>
        repo.ListAsync(cancellationToken);

    public async Task<RoutingTemplateView> GetAsync(string name, CancellationToken cancellationToken = default) =>
        await repo.GetAsync(name, cancellationToken)
            ?? throw new KeyNotFoundException($"Routing template '{name}' not found.");
}

/// <summary>Manages routing slips (live document-tracking instances) and advances them step-by-step.</summary>
public sealed class RoutingSlipService(IRoutingSlipRepository repo, IUnitOfWork unitOfWork)
{
    public Task<RoutingSlipDetailView> CreateAsync(CreateRoutingSlipCommand command, CancellationToken cancellationToken = default) =>
        repo.AddAsync(command, cancellationToken);

    public Task<IReadOnlyList<RoutingSlipView>> ListAsync(CancellationToken cancellationToken = default) =>
        repo.ListAsync(cancellationToken);

    public async Task<RoutingSlipDetailView> GetAsync(string name, CancellationToken cancellationToken = default) =>
        await repo.GetAsync(name, cancellationToken)
            ?? throw new KeyNotFoundException($"Routing slip '{name}' not found.");

    /// <summary>
    /// Marks the current step Completed (with HandledBy and Remarks), moves the slip
    /// to the next step, and closes the slip when all steps have been completed.
    /// Both operations execute inside one transaction to prevent partial updates.
    /// </summary>
    public Task<RoutingSlipDetailView> AdvanceAsync(
        string name, string handledBy, string? remarks, CancellationToken cancellationToken = default) =>
        unitOfWork.ExecuteInTransactionAsync(async token =>
        {
            var slip = await repo.GetAsync(name, token)
                ?? throw new KeyNotFoundException($"Routing slip '{name}' not found.");

            if (slip.Status is "Completed" or "Cancelled")
                throw new InvalidOperationException(
                    $"Routing slip '{name}' is already {slip.Status} and cannot be advanced.");

            var now = DateTime.UtcNow;
            var steps = slip.Steps.OrderBy(s => s.StepOrder).ToList();

            var currentIndex = steps.FindIndex(s => s.StepOrder == slip.CurrentStep);
            if (currentIndex < 0)
                currentIndex = 0;

            var nextIndex = currentIndex + 1;
            var isLast = nextIndex >= steps.Count;
            var newStatus = isLast ? "Completed" : "Open";
            var nextStep = isLast ? slip.CurrentStep : steps[nextIndex].StepOrder;
            var nextOffice = isLast ? null : steps[nextIndex].OfficeName;
            var completedOn = isLast ? now : (DateTime?)null;

            // Stamp HandledBy/Remarks; UpdateAsync transitions InProgress → Completed.
            await repo.CompleteStepAsync(name, slip.CurrentStep, handledBy, remarks, now, token);
            await repo.UpdateAsync(name, newStatus, nextStep, nextOffice, completedOn, token);

            return await repo.GetAsync(name, token)
                ?? throw new InvalidOperationException($"Routing slip '{name}' disappeared after update.");
        }, cancellationToken);

    /// <summary>Cancels an open routing slip.</summary>
    public async Task<RoutingSlipDetailView> CancelAsync(string name, CancellationToken cancellationToken = default)
    {
        var slip = await repo.GetAsync(name, cancellationToken)
            ?? throw new KeyNotFoundException($"Routing slip '{name}' not found.");

        if (slip.Status is "Cancelled")
            throw new InvalidOperationException($"Routing slip '{name}' is already cancelled.");
        if (slip.Status is "Completed")
            throw new InvalidOperationException($"Routing slip '{name}' is already completed and cannot be cancelled.");

        await repo.UpdateAsync(name, "Cancelled", slip.CurrentStep, slip.CurrentOffice, null, cancellationToken);

        return await repo.GetAsync(name, cancellationToken)
            ?? throw new InvalidOperationException($"Routing slip '{name}' disappeared after cancel.");
    }
}

/// <summary>Manages the catalogue of attachment requirements per doctype/state.</summary>
public sealed class AttachmentRequirementService(IAttachmentRequirementRepository repo)
{
    public Task<AttachmentRequirementView> CreateAsync(CreateAttachmentRequirementCommand command, CancellationToken cancellationToken = default) =>
        repo.AddAsync(command, cancellationToken);

    public Task<IReadOnlyList<AttachmentRequirementView>> ListAsync(CancellationToken cancellationToken = default) =>
        repo.ListAsync(cancellationToken);

    public Task<IReadOnlyList<AttachmentRequirementView>> ListForDoctypeAsync(
        string doctype, string? state, CancellationToken cancellationToken = default) =>
        repo.ListForDoctypeAsync(doctype, state, cancellationToken);
}
