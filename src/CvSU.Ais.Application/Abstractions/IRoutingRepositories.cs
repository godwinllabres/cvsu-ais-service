using CvSU.Ais.Application.Routing;

namespace CvSU.Ais.Application.Abstractions;

public interface IRoutingTemplateRepository
{
    Task<IReadOnlyList<RoutingTemplateView>> ListAsync(CancellationToken cancellationToken = default);
    Task<RoutingTemplateView?> GetAsync(string name, CancellationToken cancellationToken = default);
    Task<RoutingTemplateView> AddAsync(CreateRoutingTemplateCommand command, CancellationToken cancellationToken = default);
}

public interface IRoutingSlipRepository
{
    Task<IReadOnlyList<RoutingSlipView>> ListAsync(CancellationToken cancellationToken = default);
    Task<RoutingSlipDetailView?> GetAsync(string name, CancellationToken cancellationToken = default);
    Task<RoutingSlipDetailView> AddAsync(CreateRoutingSlipCommand command, CancellationToken cancellationToken = default);
    Task UpdateAsync(string name, string status, int currentStep, string? currentOffice, DateTime? completedOn, CancellationToken cancellationToken = default);

    /// <summary>
    /// Marks a specific routing step as Completed and records who handled it. Called
    /// alongside <see cref="UpdateAsync"/> so the step record reflects the advance.
    /// </summary>
    Task CompleteStepAsync(string slipName, int stepOrder, string handledBy, string? remarks, DateTime completedOn, CancellationToken cancellationToken = default);
}

public interface IAttachmentRequirementRepository
{
    Task<IReadOnlyList<AttachmentRequirementView>> ListAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AttachmentRequirementView>> ListForDoctypeAsync(string doctype, string? state, CancellationToken cancellationToken = default);
    Task<AttachmentRequirementView> AddAsync(CreateAttachmentRequirementCommand command, CancellationToken cancellationToken = default);
}
