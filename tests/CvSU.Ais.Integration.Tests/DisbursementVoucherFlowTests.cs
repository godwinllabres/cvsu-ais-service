using CvSU.Ais.Application.DisbursementVouchers;
using CvSU.Ais.Contracts;
using CvSU.Ais.Domain.Budget;
using CvSU.Ais.Domain.Common;
using CvSU.Ais.Domain.Disbursement;
using CvSU.Ais.Domain.Funds;
using CvSU.Ais.Domain.Ledgers;
using CvSU.Ais.Infrastructure;
using CvSU.Ais.Infrastructure.Numbering;
using CvSU.Ais.Infrastructure.Persistence;
using CvSU.Ais.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace CvSU.Ais.Integration.Tests;

/// <summary>End-to-end through the application service against real Postgres:
/// gapless numbering, transactional create, persisted transitions and SoD —
/// everything but the HTTP layer.</summary>
[Collection("postgres")]
public class DisbursementVoucherFlowTests(PostgresFixture fixture)
{
    private static DisbursementVoucherService Service(AisDbContext db)
    {
        var catalog = new FundingSourceCatalog(db);
        return new DisbursementVoucherService(
            new DisbursementVoucherRepository(db, catalog),
            catalog,
            new GaplessVoucherNumberService(db),
            new GeneralLedgerRepository(db),
            new BudgetLedgerRepository(db, catalog),
            new UnitOfWork(db));
    }

    /// <summary>IA-first route to Accounting: clerk routes to Internal Audit, the
    /// auditor submits to Accounting. There is no direct Draft → Submitted edge.</summary>
    private static async Task RouteToAccounting(
        DisbursementVoucherService service, string name, string clerk = "clerk@cvsu")
    {
        await service.FireAsync(name, DvAction.RequestIaAudit, new TransitionContext(clerk, DvRoles.Encoder));
        await service.FireAsync(name, DvAction.Submit, new TransitionContext("auditor@cvsu", DvRoles.InternalAuditor));
    }

    /// <summary>Record every required certification through the service, each by the officer
    /// who holds its responsible role (the encoder cannot self-certify).</summary>
    private static async Task CertifyAll(
        DisbursementVoucherService service, string name, bool supplyProperty = true)
    {
        await service.CertifyAsync(name, Certification.BudgetSufficiency, new TransitionContext("budget@cvsu", BudgetRoles.BudgetOfficer));
        await service.CertifyAsync(name, Certification.InternalAudit, new TransitionContext("ia@cvsu", DvRoles.InternalAuditor));
        await service.CertifyAsync(name, Certification.EndUserAcceptance, new TransitionContext("enduser@cvsu", DvRoles.EndUser));
        await service.CertifyAsync(name, Certification.AccountantSignature, new TransitionContext("boxd@cvsu", DvRoles.Accountant));
        if (supplyProperty)
            await service.CertifyAsync(name, Certification.SupplyPropertyInspection, new TransitionContext("supply@cvsu", DvRoles.SupplyPropertyOfficer));
    }

    [Fact]
    public async Task Create_submit_approve_persists_across_contexts()
    {
        await using var db = fixture.CreateContext();
        var service = Service(db);

        var created = await service.CreateAsync(new CreateDvCommand(
            Encoder: "clerk@cvsu", FiscalYear: 2026, Amount: 5000m, FundingSourceCode: "01101101",
            PapCode: "PAP-A", LocationCode: "LOC-A", ExpenseClass: ExpenseClass.Mooe, ObjectAccountCode: "50203010"));

        Assert.StartsWith("DV-2026-", created.Name);
        Assert.Equal(DvWorkflowStatus.Draft, created.Status);
        Assert.Null(created.ControlNumber); // not stamped before approval

        await CertifyAll(service, created.Name);
        await RouteToAccounting(service, created.Name);

        // Still unstamped after routing to Accounting — the number is assigned ONLY at Approve.
        await using (var midDb = fixture.CreateContext())
            Assert.Null((await Service(midDb).GetAsync(created.Name)).ControlNumber);

        var approved = await service.FireAsync(
            created.Name, DvAction.Approve, new TransitionContext("accountant@cvsu", DvRoles.Accountant));

        Assert.Equal(DvWorkflowStatus.Approved, approved.Status);
        Assert.Equal("accountant@cvsu", approved.ApprovedBy);
        // Approval stamps the gapless control number (per cluster, per fiscal year).
        Assert.Matches(@"^DV-CN-01-2026-\d{5}$", approved.ControlNumber!);

        // Reload from a fresh context to prove the state actually persisted.
        await using var freshDb = fixture.CreateContext();
        var reloaded = await Service(freshDb).GetAsync(created.Name);
        Assert.Equal(DvWorkflowStatus.Approved, reloaded.Status);
        Assert.Equal(approved.ControlNumber, reloaded.ControlNumber);
    }

    [Fact]
    public async Task Detail_view_carries_the_full_processing_payload()
    {
        await using var db = fixture.CreateContext();
        var service = Service(db);

        var created = await service.CreateAsync(new CreateDvCommand(
            Encoder: "clerk@cvsu", FiscalYear: 2026, Amount: 12_345.67m, FundingSourceCode: "01101101",
            PapCode: "PAP-A", LocationCode: "LOC-A", ExpenseClass: ExpenseClass.Mooe, ObjectAccountCode: "50203010"));

        // Partially certify: Budget Office and End-user only.
        await service.CertifyAsync(created.Name, Certification.BudgetSufficiency, new TransitionContext("budget@cvsu", BudgetRoles.BudgetOfficer));
        await service.CertifyAsync(created.Name, Certification.EndUserAcceptance, new TransitionContext("enduser@cvsu", DvRoles.EndUser));

        // Re-read from a fresh context so we exercise the persisted → detail path.
        await using var freshDb = fixture.CreateContext();
        var detail = await Service(freshDb).GetAsync(created.Name);

        Assert.Equal(created.Name, detail.Name);
        Assert.Equal(2026, detail.FiscalYear);
        Assert.Equal("clerk@cvsu", detail.Encoder);
        Assert.Equal(12_345.67m, detail.Amount);
        Assert.Equal("01101101", detail.FundingSourceCode);
        Assert.Equal("01", detail.FundClusterCode);
        Assert.Equal("Regular Agency Fund", detail.FundClusterName);
        Assert.Equal(DvType.Suppliers, detail.DvType);
        Assert.Equal("PAP-A", detail.PapCode);
        Assert.Equal("LOC-A", detail.LocationCode);
        Assert.Equal(ExpenseClass.Mooe, detail.ExpenseClass);
        Assert.Equal("50203010", detail.ObjectAccountCode);
        // Certifications carry the audit trail; only the two we recorded are done.
        Assert.True(detail.Budget.Done);
        Assert.Equal("budget@cvsu", detail.Budget.By);
        Assert.NotNull(detail.Budget.At);
        Assert.True(detail.EndUser.Done);
        Assert.False(detail.InternalAudit.Done);
        Assert.False(detail.Accountant.Done);
        Assert.False(detail.SupplyProperty.Done);
        Assert.Null(detail.ControlNumber);
        Assert.Equal(DvWorkflowStatus.Draft, detail.Status);
    }

    [Fact]
    public async Task Encoder_cannot_approve_their_own_dv_through_the_service()
    {
        await using var db = fixture.CreateContext();
        var service = Service(db);

        var created = await service.CreateAsync(new CreateDvCommand(
            "self@cvsu", 2026, 1000m, "01101101",
            PapCode: "PAP-A", LocationCode: "LOC-A", ExpenseClass: ExpenseClass.Mooe, ObjectAccountCode: "50203010"));
        await CertifyAll(service, created.Name);
        await RouteToAccounting(service, created.Name, clerk: "self@cvsu");

        await Assert.ThrowsAsync<SegregationOfDutiesException>(() =>
            service.FireAsync(created.Name, DvAction.Approve, new TransitionContext("self@cvsu", DvRoles.Accountant)));
    }

    [Fact]
    public async Task Post_then_release_emit_the_two_stage_ledger_postings()
    {
        await using var db = fixture.CreateContext();
        var service = Service(db);

        var created = await service.CreateAsync(new CreateDvCommand(
            Encoder: "clerk@cvsu", FiscalYear: 2026, Amount: 1000m, FundingSourceCode: "01101101",
            PapCode: "PAP-A", LocationCode: "LOC-A", ExpenseClass: ExpenseClass.Mooe, ObjectAccountCode: "50203010"));

        await CertifyAll(service, created.Name);
        await RouteToAccounting(service, created.Name);
        await service.FireAsync(created.Name, DvAction.Approve, new TransitionContext("acct@cvsu", DvRoles.Accountant));
        await service.FireAsync(created.Name, DvAction.ApproveForPayment, new TransitionContext("head@cvsu", DvRoles.HeadOfAgency));

        var posted = await service.FireAsync(created.Name, DvAction.Post, new TransitionContext("acct@cvsu", DvRoles.Accountant));
        Assert.Equal(DvWorkflowStatus.Posted, posted.Status);

        // Post emits the accrual journal (DR expense / CR payable), which balances.
        await using (var read = fixture.CreateContext())
        {
            var accrual = await read.GeneralLedger.Where(e => e.VoucherNo == created.Name).ToListAsync();
            Assert.Equal(2, accrual.Count);
            Assert.Equal(accrual.Sum(e => e.Debit), accrual.Sum(e => e.Credit));
        }

        var released = await service.FireAsync(created.Name, DvAction.Release, new TransitionContext("cash@cvsu", DvRoles.Treasury));
        Assert.Equal(DvWorkflowStatus.Released, released.Status);

        // Release adds the cash journal (DR payable / CR cash) plus a budget Disbursement entry.
        await using (var read = fixture.CreateContext())
        {
            var gl = await read.GeneralLedger.Where(e => e.VoucherNo == created.Name).ToListAsync();
            Assert.Equal(4, gl.Count);
            Assert.Equal(gl.Sum(e => e.Debit), gl.Sum(e => e.Credit));

            var disbursement = await read.BudgetLedger
                .Where(e => e.VoucherNo == created.Name && e.EntryType == BudgetEntryType.Disbursement)
                .ToListAsync();
            var entry = Assert.Single(disbursement);
            Assert.Equal(1000m, entry.Debit);
        }
    }

    [Fact]
    public async Task Editing_a_draft_completes_the_budget_line_and_lets_it_route()
    {
        await using var db = fixture.CreateContext();
        var service = Service(db);

        // An incomplete draft (no UACS line) cannot pass the encoding-complete gate.
        var created = await service.CreateAsync(new CreateDvCommand(
            Encoder: "clerk@cvsu", FiscalYear: 2026, Amount: 100m, FundingSourceCode: "01101101"));

        await Assert.ThrowsAsync<InvalidTransitionException>(() => service.FireAsync(
            created.Name, DvAction.RequestIaAudit, new TransitionContext("clerk@cvsu", DvRoles.Encoder)));

        // Complete it via edit; routing then succeeds and the edit persists.
        await service.UpdateAsync(created.Name, new UpdateDvCommand(
            Amount: 4321m, FundingSourceCode: "01101101", DvType: DvType.Others,
            PapCode: "PAP-A", LocationCode: "LOC-A", ExpenseClass: ExpenseClass.Mooe, ObjectAccountCode: "50203010"));

        await service.FireAsync(
            created.Name, DvAction.RequestIaAudit, new TransitionContext("clerk@cvsu", DvRoles.Encoder));

        await using var freshDb = fixture.CreateContext();
        var detail = await Service(freshDb).GetAsync(created.Name);
        Assert.Equal(4321m, detail.Amount);
        Assert.Equal(DvType.Others, detail.DvType);
        Assert.Equal("PAP-A", detail.PapCode);
        Assert.Equal(DvWorkflowStatus.IaAuditRequired, detail.Status);
    }

    [Fact]
    public async Task Approval_assigns_consecutive_control_numbers()
    {
        await using var db = fixture.CreateContext();
        var service = Service(db);

        var first = await CreateApprovedDv(service, "g1");
        var second = await CreateApprovedDv(service, "g2");

        // Both belong to cluster 01 / FY2026, so they share one gapless series and the
        // second is exactly one past the first (no gap between adjacent issues).
        Assert.Matches(@"^DV-CN-01-2026-\d{5}$", first.ControlNumber!);
        Assert.Matches(@"^DV-CN-01-2026-\d{5}$", second.ControlNumber!);
        Assert.Equal(SeriesCounter(first.ControlNumber!) + 1, SeriesCounter(second.ControlNumber!));
    }

    [Fact]
    public async Task Certification_is_recorded_with_its_certifier_and_rejects_the_wrong_role()
    {
        await using var db = fixture.CreateContext();
        var service = Service(db);

        var created = await service.CreateAsync(new CreateDvCommand(
            Encoder: "clerk@cvsu", FiscalYear: 2026, Amount: 300m, FundingSourceCode: "01101101",
            PapCode: "PAP-C", LocationCode: "LOC-C", ExpenseClass: ExpenseClass.Mooe, ObjectAccountCode: "50203010"));

        // The encoder cannot self-certify another office's control.
        await Assert.ThrowsAsync<UnauthorizedTransitionException>(() => service.CertifyAsync(
            created.Name, Certification.BudgetSufficiency, new TransitionContext("clerk@cvsu", DvRoles.Encoder)));

        // The Budget Officer can, and it is recorded with their identity + timestamp.
        await service.CertifyAsync(created.Name, Certification.BudgetSufficiency,
            new TransitionContext("budget@cvsu", BudgetRoles.BudgetOfficer));

        await using var freshDb = fixture.CreateContext();
        var detail = await Service(freshDb).GetAsync(created.Name);
        Assert.True(detail.Budget.Done);
        Assert.Equal("budget@cvsu", detail.Budget.By);
        Assert.NotNull(detail.Budget.At);
        Assert.False(detail.InternalAudit.Done);
    }

    [Fact]
    public async Task Edit_is_rejected_once_the_dv_leaves_draft()
    {
        await using var db = fixture.CreateContext();
        var service = Service(db);

        var created = await service.CreateAsync(new CreateDvCommand(
            Encoder: "clerk@cvsu", FiscalYear: 2026, Amount: 200m, FundingSourceCode: "01101101",
            PapCode: "PAP-Z", LocationCode: "LOC-Z", ExpenseClass: ExpenseClass.Mooe, ObjectAccountCode: "50203010"));
        await service.FireAsync(created.Name, DvAction.RequestIaAudit, new TransitionContext("clerk@cvsu", DvRoles.Encoder));

        // The service/repo wrapping must not persist an edit once the DV has left Draft.
        await Assert.ThrowsAsync<InvalidTransitionException>(() => service.UpdateAsync(created.Name, new UpdateDvCommand(
            Amount: 999m, FundingSourceCode: "01101101", DvType: DvType.Payroll,
            PapCode: "HACK", LocationCode: "HACK", ExpenseClass: ExpenseClass.Co, ObjectAccountCode: "99999999")));

        await using var freshDb = fixture.CreateContext();
        var detail = await Service(freshDb).GetAsync(created.Name);
        Assert.Equal(200m, detail.Amount);             // unchanged
        Assert.Equal("PAP-Z", detail.PapCode);         // unchanged
        Assert.Equal(DvType.Suppliers, detail.DvType); // unchanged (still the create default)
    }

    [Fact]
    public async Task Payment_reference_unique_index_rejects_a_duplicate_row_at_the_database()
    {
        // Bypass the application pre-check by inserting rows directly, proving the partial
        // unique index (the concurrency backstop) is actually present and active.
        await using var db = fixture.CreateContext();

        db.Add(NewPaidRow("DV-IDX-A", DvPaymentMethod.Cheque, "DUP-IDX-TEST"));
        await db.SaveChangesAsync();

        db.Add(NewPaidRow("DV-IDX-B", DvPaymentMethod.Cheque, "DUP-IDX-TEST"));
        await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
    }

    private static DisbursementVoucherRow NewPaidRow(string name, DvPaymentMethod method, string reference) => new()
    {
        Name = name,
        Encoder = "x@cvsu",
        Amount = 1m,
        DvType = DvType.Others,
        FundingSourceCode = "01101101",
        Lifecycle = "Submitted",
        Status = "Released",
        PaymentMethod = method,
        PaymentReference = reference,
    };

    [Fact]
    public async Task Duplicate_payment_reference_is_rejected_across_dvs()
    {
        await using var db = fixture.CreateContext();
        var service = Service(db);

        var a = await CreateApprovedForPaymentDv(service, "p1");
        var b = await CreateApprovedForPaymentDv(service, "p2");

        await service.RecordPaymentAsync(a, DvPaymentMethod.Cheque, "CHK-UNIQUE-1");

        // The same cheque number on a different DV is a duplicate disbursement.
        await Assert.ThrowsAsync<DuplicatePaymentIdentifierException>(
            () => service.RecordPaymentAsync(b, DvPaymentMethod.Cheque, "CHK-UNIQUE-1"));

        // A distinct reference is fine, and re-recording a DV's own reference is not a clash.
        await service.RecordPaymentAsync(b, DvPaymentMethod.Cheque, "CHK-UNIQUE-2");
        await service.RecordPaymentAsync(a, DvPaymentMethod.Cheque, "CHK-UNIQUE-1");

        await using var freshDb = fixture.CreateContext();
        var detailA = await Service(freshDb).GetAsync(a);
        Assert.Equal(DvPaymentMethod.Cheque, detailA.PaymentMethod);
        Assert.Equal("CHK-UNIQUE-1", detailA.PaymentReference);
    }

    private static int SeriesCounter(string controlNumber) => int.Parse(controlNumber.Split('-')[^1]);

    private async Task<DvStateView> CreateApprovedDv(DisbursementVoucherService service, string suffix)
    {
        var created = await service.CreateAsync(new CreateDvCommand(
            Encoder: $"clerk-{suffix}@cvsu", FiscalYear: 2026, Amount: 1000m, FundingSourceCode: "01101101",
            PapCode: "PAP-A", LocationCode: "LOC-A", ExpenseClass: ExpenseClass.Mooe, ObjectAccountCode: "50203010"));
        await CertifyAll(service, created.Name);
        await RouteToAccounting(service, created.Name, clerk: $"clerk-{suffix}@cvsu");
        return await service.FireAsync(
            created.Name, DvAction.Approve, new TransitionContext("acct@cvsu", DvRoles.Accountant));
    }

    private async Task<string> CreateApprovedForPaymentDv(DisbursementVoucherService service, string suffix)
    {
        var approved = await CreateApprovedDv(service, suffix);
        await service.FireAsync(
            approved.Name, DvAction.ApproveForPayment, new TransitionContext("head@cvsu", DvRoles.HeadOfAgency));
        return approved.Name;
    }
}
