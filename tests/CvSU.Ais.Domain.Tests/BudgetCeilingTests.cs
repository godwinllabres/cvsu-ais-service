using CvSU.Ais.Domain.Budget;
using CvSU.Ais.Domain.Common;
using CvSU.Ais.Domain.Funds;
using CvSU.Ais.Domain.Ledgers;
using Xunit;

namespace CvSU.Ais.Domain.Tests;

/// <summary>
/// The execution-cycle ceilings (R-BUD-01/02), the STF Personnel-Services
/// prohibition (R-BUD-05), and fund-cluster integrity — the rules that keep a
/// per-cluster trial balance honest.
/// </summary>
public class BudgetCeilingTests
{
    private static Appropriation Appropriation(decimal amount, FundingSource? fs = null)
    {
        fs ??= TestData.RegularAgencyFund();
        return new Appropriation("APP-001", TestData.FiscalYear, TestData.Uacs(fs), new Money(amount));
    }

    [Fact]
    public void Allotment_within_appropriation_succeeds_and_reduces_unallotted_balance()
    {
        var app = Appropriation(1_000_000m);

        var allotment = app.Allot("ALL-001", new Money(600_000m), TestData.Today);

        Assert.Equal(new Money(600_000m), app.AllottedAmount);
        Assert.Equal(new Money(400_000m), app.UnallottedBalance);
        Assert.Equal(new Money(600_000m), allotment.Amount);
    }

    [Fact]
    public void Allotment_exceeding_appropriation_is_blocked_R_BUD_01()
    {
        var app = Appropriation(1_000_000m);
        app.Allot("ALL-001", new Money(800_000m), TestData.Today);

        var ex = Assert.Throws<BudgetCeilingExceededException>(
            () => app.Allot("ALL-002", new Money(300_000m), TestData.Today));

        Assert.Contains("R-BUD-01", ex.Message);
    }

    [Fact]
    public void Obligation_within_allotment_succeeds_and_posts_to_the_credit_side()
    {
        var allotment = Appropriation(1_000_000m).Allot("ALL-001", new Money(500_000m), TestData.Today);
        var uacs = TestData.Uacs(TestData.RegularAgencyFund());

        var entry = allotment.Obligate("ORS-001", new Money(200_000m), uacs, TestData.Today);

        Assert.Equal(new Money(300_000m), allotment.UnobligatedBalance);
        Assert.Equal(BudgetEntryType.Obligation, entry.EntryType);
        Assert.Equal(new Money(200_000m), entry.Credit);
    }

    [Fact]
    public void Obligation_exceeding_allotment_is_blocked_R_BUD_02()
    {
        var allotment = Appropriation(1_000_000m).Allot("ALL-001", new Money(500_000m), TestData.Today);
        var uacs = TestData.Uacs(TestData.RegularAgencyFund());

        var ex = Assert.Throws<BudgetCeilingExceededException>(
            () => allotment.Obligate("ORS-001", new Money(500_001m), uacs, TestData.Today));

        Assert.Contains("R-BUD-02", ex.Message);
    }

    [Fact]
    public void Stf_cannot_fund_personnel_services_R_BUD_05()
    {
        var stf = TestData.StfFund();
        var allotment = Appropriation(1_000_000m, stf).Allot("ALL-001", new Money(500_000m), TestData.Today);
        var psLine = TestData.Uacs(stf, ExpenseClass.Ps);

        Assert.Throws<StfCannotFundPersonnelServicesException>(
            () => allotment.Obligate("ORS-001", new Money(100_000m), psLine, TestData.Today));
    }

    [Fact]
    public void Obligation_cannot_mix_fund_clusters()
    {
        // Allotment is Regular Agency (cluster 01); the obligation line cites an STF
        // (cluster 05) funding source — contamination that would corrupt a per-cluster
        // trial balance.
        var allotment = Appropriation(1_000_000m).Allot("ALL-001", new Money(500_000m), TestData.Today);
        var foreignClusterLine = TestData.Uacs(TestData.StfFund(), ExpenseClass.Mooe);

        Assert.Throws<FundClusterContaminationException>(
            () => allotment.Obligate("ORS-001", new Money(100_000m), foreignClusterLine, TestData.Today));
    }

    [Fact]
    public void A_full_chain_keeps_each_cluster_self_contained()
    {
        // Two independent chains in different clusters; every emitted entry stays in
        // its own cluster, so grouping the ledger by cluster nets cleanly.
        var regularAllot = Appropriation(1_000_000m, TestData.RegularAgencyFund())
            .Allot("ALL-R", new Money(500_000m), TestData.Today);
        var stfAllot = Appropriation(1_000_000m, TestData.StfFund())
            .Allot("ALL-S", new Money(500_000m), TestData.Today);

        var entries = new[]
        {
            regularAllot.Obligate("ORS-R", new Money(100_000m), TestData.Uacs(TestData.RegularAgencyFund()), TestData.Today),
            stfAllot.Obligate("ORS-S", new Money(100_000m), TestData.Uacs(TestData.StfFund()), TestData.Today),
        };

        Assert.All(entries.Where(e => e.AllotmentId == "ALL-R"), e => Assert.Equal("01", e.Cluster.Code));
        Assert.All(entries.Where(e => e.AllotmentId == "ALL-S"), e => Assert.Equal("05", e.Cluster.Code));
        Assert.Equal(2, entries.Select(e => e.Cluster.Code).Distinct().Count());
    }
}
