using CvSU.Ais.Application.Collections;

namespace CvSU.Ais.Application.Abstractions;

public interface IOrderOfPaymentRepository
{
    Task<IReadOnlyList<OrderOfPaymentView>> ListAsync(CancellationToken cancellationToken = default);
    Task<OrderOfPaymentDetailView?> GetAsync(string name, CancellationToken cancellationToken = default);
    Task<OrderOfPaymentDetailView> AddAsync(CreateOrderOfPaymentCommand command, CancellationToken cancellationToken = default);
    Task<OrderOfPaymentDetailView> UpdateStatusAsync(string name, string status, CancellationToken cancellationToken = default);
}

public interface IOfficialReceiptRepository
{
    Task<IReadOnlyList<OfficialReceiptView>> ListAsync(CancellationToken cancellationToken = default);
    Task<OfficialReceiptDetailView?> GetAsync(string name, CancellationToken cancellationToken = default);
    Task<OfficialReceiptDetailView> AddAsync(CreateOfficialReceiptCommand command, CancellationToken cancellationToken = default);
    Task<OfficialReceiptDetailView> UpdateStatusAsync(string name, string status, CancellationToken cancellationToken = default);
}

public interface IRcdRepository
{
    Task<IReadOnlyList<RcdView>> ListAsync(CancellationToken cancellationToken = default);
    Task<RcdDetailView?> GetAsync(string name, CancellationToken cancellationToken = default);
    Task<RcdDetailView> AddAsync(CreateRcdCommand command, CancellationToken cancellationToken = default);
    Task<RcdDetailView> UpdateStatusAsync(string name, string status, CancellationToken cancellationToken = default);
}
