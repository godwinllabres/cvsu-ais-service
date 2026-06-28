using CvSU.Ais.Application.Abstractions;
using CvSU.Ais.Application.Routing;
using CvSU.Ais.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CvSU.Ais.Infrastructure.Repositories;

// ── Routing Template ─────────────────────────────────────────────────────────

public sealed class RoutingTemplateRepository(AisDbContext db) : IRoutingTemplateRepository
{
    public async Task<IReadOnlyList<RoutingTemplateView>> ListAsync(CancellationToken cancellationToken = default)
    {
        var rows = await db.Set<RoutingTemplateRow>()
            .Include(r => r.Steps.OrderBy(s => s.StepOrder))
            .OrderBy(r => r.Name)
            .ToListAsync(cancellationToken);

        return rows.Select(ToView).ToList();
    }

    public async Task<RoutingTemplateView?> GetAsync(string name, CancellationToken cancellationToken = default)
    {
        var row = await db.Set<RoutingTemplateRow>()
            .Include(r => r.Steps.OrderBy(s => s.StepOrder))
            .FirstOrDefaultAsync(r => r.Name == name, cancellationToken);

        return row is null ? null : ToView(row);
    }

    public async Task<RoutingTemplateView> AddAsync(
        CreateRoutingTemplateCommand command, CancellationToken cancellationToken = default)
    {
        var row = new RoutingTemplateRow
        {
            Name = command.Name,
            TemplateName = command.TemplateName,
            DocumentType = command.DocumentType,
            Description = command.Description,
            MinAmount = command.MinAmount,
            MaxAmount = command.MaxAmount,
            Steps = command.Steps.Select((s, i) => new RoutingTemplateStepRow
            {
                ParentTemplateName = command.Name,
                StepOrder = s.StepOrder,
                OfficeName = s.OfficeName,
                Role = s.Role,
                DurationDays = s.DurationDays,
                IsRequired = s.IsRequired,
            }).ToList(),
        };

        db.Add(row);
        await db.SaveChangesAsync(cancellationToken);
        return ToView(row);
    }

    private static RoutingTemplateView ToView(RoutingTemplateRow r) => new(
        r.Name,
        r.TemplateName,
        r.DocumentType,
        r.Steps.Select(s => new RoutingTemplateStepDto(s.StepOrder, s.OfficeName, s.Role, s.DurationDays, s.IsRequired))
               .ToList());
}

// ── Routing Slip ─────────────────────────────────────────────────────────────

public sealed class RoutingSlipRepository(AisDbContext db) : IRoutingSlipRepository
{
    public async Task<IReadOnlyList<RoutingSlipView>> ListAsync(CancellationToken cancellationToken = default)
    {
        return await db.Set<RoutingSlipRow>()
            .OrderBy(r => r.Name)
            .Select(r => new RoutingSlipView(
                r.Name,
                r.RoutingTemplateName,
                r.Status,
                r.ReferenceDoctype,
                r.ReferenceName,
                r.CurrentOffice))
            .ToListAsync(cancellationToken);
    }

    public async Task<RoutingSlipDetailView?> GetAsync(string name, CancellationToken cancellationToken = default)
    {
        var row = await db.Set<RoutingSlipRow>()
            .Include(r => r.Steps.OrderBy(s => s.StepOrder))
            .FirstOrDefaultAsync(r => r.Name == name, cancellationToken);

        return row is null ? null : ToDetailView(row);
    }

    public async Task<RoutingSlipDetailView> AddAsync(
        CreateRoutingSlipCommand command, CancellationToken cancellationToken = default)
    {
        // Resolve template to copy the steps.
        var template = await db.Set<RoutingTemplateRow>()
            .Include(t => t.Steps.OrderBy(s => s.StepOrder))
            .FirstOrDefaultAsync(t => t.Name == command.RoutingTemplateName, cancellationToken)
            ?? throw new KeyNotFoundException(
                $"Routing template '{command.RoutingTemplateName}' not found.");

        // Generate a short, unique ID for the slip name.
        var shortId = Guid.NewGuid().ToString("N")[..8].ToUpperInvariant();
        var slipName = $"RS-{shortId}";

        var firstStep = template.Steps.MinBy(s => s.StepOrder);
        var now = DateTime.UtcNow;

        var row = new RoutingSlipRow
        {
            Name = slipName,
            RoutingTemplateName = command.RoutingTemplateName,
            Status = "Open",
            ReferenceDoctype = command.ReferenceDoctype,
            ReferenceName = command.ReferenceName,
            CurrentStep = firstStep?.StepOrder ?? 0,
            CurrentOffice = firstStep?.OfficeName,
            StartedOn = now,
            CompletedOn = null,
            Steps = template.Steps.Select(ts => new RoutingSlipStepRow
            {
                ParentSlipName = slipName,
                StepOrder = ts.StepOrder,
                OfficeName = ts.OfficeName,
                Status = ts.StepOrder == firstStep?.StepOrder ? "InProgress" : "Pending",
                StartedOn = ts.StepOrder == firstStep?.StepOrder ? now : null,
                CompletedOn = null,
                HandledBy = null,
                Remarks = null,
            }).ToList(),
        };

        db.Add(row);
        await db.SaveChangesAsync(cancellationToken);
        return ToDetailView(row);
    }

    public async Task UpdateAsync(
        string name,
        string status,
        int currentStep,
        string? currentOffice,
        DateTime? completedOn,
        CancellationToken cancellationToken = default)
    {
        var row = await db.Set<RoutingSlipRow>()
            .Include(r => r.Steps)
            .FirstOrDefaultAsync(r => r.Name == name, cancellationToken)
            ?? throw new InvalidOperationException($"Routing slip '{name}' not found for update.");

        var now = DateTime.UtcNow;

        // Mark the previously-active step as Completed.
        var activeStep = row.Steps.FirstOrDefault(s => s.Status == "InProgress");
        if (activeStep is not null)
        {
            activeStep.Status = "Completed";
            activeStep.CompletedOn = now;
        }

        // Activate the next step if the slip is not finished.
        if (status == "Open")
        {
            var nextStep = row.Steps.FirstOrDefault(s => s.StepOrder == currentStep);
            if (nextStep is not null)
            {
                nextStep.Status = "InProgress";
                nextStep.StartedOn = now;
            }
        }

        row.Status = status;
        row.CurrentStep = currentStep;
        row.CurrentOffice = currentOffice;
        row.CompletedOn = completedOn;

        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task CompleteStepAsync(
        string slipName, int stepOrder, string handledBy, string? remarks,
        DateTime completedOn, CancellationToken cancellationToken = default)
    {
        var row = await db.Set<RoutingSlipRow>()
            .Include(r => r.Steps)
            .FirstOrDefaultAsync(r => r.Name == slipName, cancellationToken)
            ?? throw new KeyNotFoundException($"Routing slip '{slipName}' not found.");

        var step = row.Steps.FirstOrDefault(s => s.StepOrder == stepOrder);
        if (step is not null)
        {
            step.HandledBy = handledBy;
            step.Remarks = remarks;
        }

        await db.SaveChangesAsync(cancellationToken);
    }

    private static RoutingSlipDetailView ToDetailView(RoutingSlipRow r) => new(
        r.Name,
        r.RoutingTemplateName,
        r.Status,
        r.ReferenceDoctype,
        r.ReferenceName,
        r.CurrentOffice,
        r.CurrentStep,
        r.StartedOn,
        r.CompletedOn,
        r.Steps.Select(s => new RoutingSlipStepDto(
            s.StepOrder,
            s.OfficeName,
            s.Status,
            s.StartedOn,
            s.CompletedOn,
            s.HandledBy,
            s.Remarks)).ToList());
}

// ── Attachment Requirement ───────────────────────────────────────────────────

public sealed class AttachmentRequirementRepository(AisDbContext db) : IAttachmentRequirementRepository
{
    public async Task<IReadOnlyList<AttachmentRequirementView>> ListAsync(CancellationToken cancellationToken = default)
    {
        return await db.Set<AttachmentRequirementRow>()
            .OrderBy(r => r.ReferenceDoctype)
            .ThenBy(r => r.WorkflowState)
            .ThenBy(r => r.RequirementCode)
            .Select(r => ToView(r))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<AttachmentRequirementView>> ListForDoctypeAsync(
        string doctype, string? state, CancellationToken cancellationToken = default)
    {
        return await db.Set<AttachmentRequirementRow>()
            .Where(r => r.ReferenceDoctype == doctype
                        && (state == null || r.WorkflowState == null || r.WorkflowState == state)
                        && r.IsEnabled)
            .OrderBy(r => r.RequirementCode)
            .Select(r => ToView(r))
            .ToListAsync(cancellationToken);
    }

    public async Task<AttachmentRequirementView> AddAsync(
        CreateAttachmentRequirementCommand command, CancellationToken cancellationToken = default)
    {
        var row = new AttachmentRequirementRow
        {
            ReferenceDoctype = command.ReferenceDoctype,
            WorkflowState = command.WorkflowState,
            RequirementCode = command.RequirementCode,
            RequirementLabel = command.RequirementLabel,
            ValidationMode = command.ValidationMode,
            FilenameKeyword = command.FilenameKeyword,
            IsEnabled = true,
        };

        db.Add(row);
        await db.SaveChangesAsync(cancellationToken);
        return ToView(row);
    }

    private static AttachmentRequirementView ToView(AttachmentRequirementRow r) => new(
        r.Id,
        r.ReferenceDoctype,
        r.WorkflowState,
        r.RequirementCode,
        r.RequirementLabel,
        r.ValidationMode,
        r.FilenameKeyword,
        r.IsEnabled);
}
