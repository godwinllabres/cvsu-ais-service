using CvSU.Ais.Application.DisbursementVouchers;
using CvSU.Ais.Domain.Disbursement;

namespace CvSU.Ais.Application.Abstractions;

public interface IDisbursementVoucherRepository
{
    Task<DisbursementVoucher?> FindAsync(string name, CancellationToken cancellationToken = default);

    /// <summary>Lightweight read model of all DVs for the inbox/list view.</summary>
    Task<IReadOnlyList<DvStateView>> ListAsync(CancellationToken cancellationToken = default);

    Task AddAsync(DisbursementVoucher voucher, CancellationToken cancellationToken = default);

    /// <summary>Persist a state change produced by <see cref="DisbursementVoucher.Fire"/>.</summary>
    Task UpdateAsync(DisbursementVoucher voucher, CancellationToken cancellationToken = default);
}
