using CvSU.Ais.Application.Exports;
using CvSU.Ais.Domain.Common;
using CvSU.Ais.Domain.Disbursement;
using CvSU.Ais.Domain.Funds;

namespace CvSU.Ais.Application.Tests;

/// <summary>
/// F3 — export document name generation no longer slices past the end of the string.
/// Creating a FINDES export and a Bank Collection Report must not throw
/// ArgumentOutOfRangeException and must produce a non-empty document name.
/// </summary>
public class ExportNamingGuardTests
{
    private static FundingSource RegularAgencyFund() =>
        new("01101101", "Regular Agency Fund", FundCluster.RegularAgency);

    [Fact]
    public async Task FindesExportCreate_produces_non_empty_name_and_does_not_throw()
    {
        var repo = new FakeFindesExportRepository();
        var dvRepo = new FakeDisbursementVoucherRepository();
        dvRepo.Seed(new DisbursementVoucher("DV-2026-0001", "clerk@cvsu", new Money(1_000m), RegularAgencyFund()));

        var service = new FindesExportService(repo, dvRepo);
        var command = new CreateFindesExportCommand(
            ExportDate: new DateOnly(2026, 6, 24),
            Lines: new List<FindesExportLineDto> { new("DV-2026-0001", 1_000m) },
            Remarks: null);

        var view = await service.CreateAsync(command);

        Assert.False(string.IsNullOrWhiteSpace(view.Name));
        Assert.StartsWith("FINDES-", view.Name);
        Assert.NotNull(repo.Added);
        Assert.Equal(view.Name, repo.Added!.Name);
        // DV total reconciles with what the line claims to remit.
        Assert.Equal(1_000m, view.DvTotalAmount);
        Assert.Equal(1_000m, view.ExportTotalAmount);
        Assert.True(view.VarianceAcceptable);
    }

    [Fact]
    public async Task FindesExportCreate_with_unknown_dv_still_produces_name()
    {
        // A DV the repo cannot find contributes zero and must not abort the batch.
        var repo = new FakeFindesExportRepository();
        var dvRepo = new FakeDisbursementVoucherRepository();
        var service = new FindesExportService(repo, dvRepo);
        var command = new CreateFindesExportCommand(
            ExportDate: new DateOnly(2026, 6, 24),
            Lines: new List<FindesExportLineDto> { new("DV-DOES-NOT-EXIST") },
            Remarks: null);

        var view = await service.CreateAsync(command);

        Assert.False(string.IsNullOrWhiteSpace(view.Name));
        Assert.StartsWith("FINDES-", view.Name);
    }

    [Fact]
    public async Task BankCollectionReportCreate_produces_non_empty_name_and_does_not_throw()
    {
        var repo = new FakeBankCollectionReportRepository();
        var service = new BankCollectionReportService(repo);
        var command = new CreateBankCollectionReportCommand(
            ReportDate: new DateOnly(2026, 6, 24),
            Lines: new List<BankCollectionLineDto>
            {
                new("REF-1", "LBP-1", 500m, true, "ORS-1", null),
                new("REF-2", null, 250m, false, null, "unmatched"),
            },
            Remarks: null);

        var view = await service.CreateAsync(command);

        Assert.False(string.IsNullOrWhiteSpace(view.Name));
        Assert.StartsWith("BCR-", view.Name);
        Assert.NotNull(repo.Added);
        Assert.Equal(750m, view.TotalAmount);
        Assert.Equal(1, view.ExceptionsCount);
    }
}
