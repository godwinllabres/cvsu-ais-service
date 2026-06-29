using CvSU.Ais.Contracts;
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

    /// <summary>True if any DV other than <paramref name="excludeName"/> already carries
    /// this payment instrument reference — the duplicate-disbursement guard.</summary>
    Task<bool> PaymentReferenceExistsAsync(
        DvPaymentMethod method, string reference, string excludeName, CancellationToken cancellationToken = default);
}
