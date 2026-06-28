using CvSU.Ais.Domain.Funds;
using CvSU.Ais.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CvSU.Ais.Infrastructure;

/// <summary>
/// The persistence context. Notable choices, all mirroring the BLE hardening the
/// Frappe app added at the storage layer (CLAUDE.md "BLE hardening"):
/// <list type="bullet">
/// <item>CHECK constraints make negative or double-sided amounts unrepresentable
/// at the database, not just in code.</item>
/// <item><c>ledger_seq</c> is a DB identity column — monotonic audit ordering.</item>
/// <item>Composite indexes target the RAOD/RBUD reporting query shapes.</item>
/// </list>
/// </summary>
public sealed class AisDbContext(DbContextOptions<AisDbContext> options) : DbContext(options)
{
    // ── Ledgers ──────────────────────────────────────────────────────────────
    public DbSet<GeneralLedgerRow> GeneralLedger => Set<GeneralLedgerRow>();
    public DbSet<BudgetLedgerRow> BudgetLedger => Set<BudgetLedgerRow>();

    // ── Funding / Reference ───────────────────────────────────────────────────
    public DbSet<FundingSourceRow> FundingSources => Set<FundingSourceRow>();
    public DbSet<VoucherCounter> VoucherCounters => Set<VoucherCounter>();
    public DbSet<PapCodeRow> PapCodes => Set<PapCodeRow>();
    public DbSet<LocationCodeRow> LocationCodes => Set<LocationCodeRow>();
    public DbSet<OperationalFundRow> OperationalFunds => Set<OperationalFundRow>();

    // ── Disbursement Vouchers ─────────────────────────────────────────────────
    public DbSet<DisbursementVoucherRow> DisbursementVouchers => Set<DisbursementVoucherRow>();

    // ── Journal Entries ───────────────────────────────────────────────────────
    public DbSet<JournalEntryRow> JournalEntries => Set<JournalEntryRow>();
    public DbSet<JeLineRow> JeLines => Set<JeLineRow>();

    // ── Obligations ───────────────────────────────────────────────────────────
    public DbSet<ObligationRequestRow> ObligationRequests => Set<ObligationRequestRow>();
    public DbSet<OrsLineItemRow> OrsLineItems => Set<OrsLineItemRow>();
    public DbSet<NoticeOfCashAllocationRow> NoticesOfCashAllocation => Set<NoticeOfCashAllocationRow>();

    // ── Collections ───────────────────────────────────────────────────────────
    public DbSet<OrderOfPaymentRow> OrdersOfPayment => Set<OrderOfPaymentRow>();
    public DbSet<OfficialReceiptRow> OfficialReceipts => Set<OfficialReceiptRow>();
    public DbSet<ReportOfCollectionsRow> ReportsOfCollections => Set<ReportOfCollectionsRow>();
    public DbSet<RcdLineRow> RcdLines => Set<RcdLineRow>();

    // ── Cash Advances ─────────────────────────────────────────────────────────
    public DbSet<CashAdvanceRow> CashAdvances => Set<CashAdvanceRow>();
    public DbSet<LiquidationReportRow> LiquidationReports => Set<LiquidationReportRow>();
    public DbSet<LiquidationLineRow> LiquidationLines => Set<LiquidationLineRow>();

    // ── Payroll ───────────────────────────────────────────────────────────────
    public DbSet<PayrollEntryRow> PayrollEntries => Set<PayrollEntryRow>();
    public DbSet<PayrollLoanDeductionRow> PayrollLoanDeductions => Set<PayrollLoanDeductionRow>();
    public DbSet<JoCosPayrollEntryRow> JoCosPayrollEntries => Set<JoCosPayrollEntryRow>();
    public DbSet<JoCosPayrollLineRow> JoCosPayrollLines => Set<JoCosPayrollLineRow>();
    public DbSet<SalaryTrancheRow> SalaryTranches => Set<SalaryTrancheRow>();
    public DbSet<SalaryTrancheEntryRow> SalaryTrancheEntries => Set<SalaryTrancheEntryRow>();
    public DbSet<EmployeeSalaryGradeRow> EmployeeSalaryGrades => Set<EmployeeSalaryGradeRow>();

    // ── Payments ──────────────────────────────────────────────────────────────
    public DbSet<LddapAdaRow> LddapAda => Set<LddapAdaRow>();
    public DbSet<LddapAdaItemRow> LddapAdaItems => Set<LddapAdaItemRow>();
    public DbSet<DvTransmittalRow> DvTransmittals => Set<DvTransmittalRow>();
    public DbSet<DvTransmittalItemRow> DvTransmittalItems => Set<DvTransmittalItemRow>();
    public DbSet<AuditIntakeRow> AuditIntake => Set<AuditIntakeRow>();

    // ── Exports ───────────────────────────────────────────────────────────────
    public DbSet<FindesExportRow> FindesExports => Set<FindesExportRow>();
    public DbSet<FindesExportLineRow> FindesExportLines => Set<FindesExportLineRow>();
    public DbSet<BankCollectionReportRow> BankCollectionReports => Set<BankCollectionReportRow>();
    public DbSet<BankCollectionLineRow> BankCollectionLines => Set<BankCollectionLineRow>();
    public DbSet<PushTokenRow> PushTokens => Set<PushTokenRow>();

    // ── Routing ───────────────────────────────────────────────────────────────
    public DbSet<RoutingTemplateRow> RoutingTemplates => Set<RoutingTemplateRow>();
    public DbSet<RoutingTemplateStepRow> RoutingTemplateSteps => Set<RoutingTemplateStepRow>();
    public DbSet<RoutingSlipRow> RoutingSlips => Set<RoutingSlipRow>();
    public DbSet<RoutingSlipStepRow> RoutingSlipSteps => Set<RoutingSlipStepRow>();
    public DbSet<AttachmentRequirementRow> AttachmentRequirements => Set<AttachmentRequirementRow>();

    // ── Compliance ────────────────────────────────────────────────────────────
    public DbSet<CoaCaseRow> CoaCases => Set<CoaCaseRow>();
    public DbSet<Bir2307Row> Bir2307 => Set<Bir2307Row>();
    public DbSet<WithholdingTaxStatementRow> WithholdingTaxStatements => Set<WithholdingTaxStatementRow>();
    public DbSet<WithholdingTaxLineRow> WithholdingTaxLines => Set<WithholdingTaxLineRow>();
    public DbSet<StateHistoryRow> StateHistory => Set<StateHistoryRow>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // ── General Ledger ────────────────────────────────────────────────────
        modelBuilder.Entity<GeneralLedgerRow>(e =>
        {
            e.ToTable("gl_entry", t =>
            {
                t.HasCheckConstraint("ck_gl_debit_nonneg", "debit >= 0");
                t.HasCheckConstraint("ck_gl_credit_nonneg", "credit >= 0");
                t.HasCheckConstraint("ck_gl_single_sided", "NOT (debit > 0 AND credit > 0)");
            });
            e.HasKey(x => x.LedgerSeq);
            e.Property(x => x.LedgerSeq).UseIdentityByDefaultColumn();
            e.Property(x => x.Account).HasMaxLength(20);
            e.Property(x => x.Debit).HasPrecision(18, 2);
            e.Property(x => x.Credit).HasPrecision(18, 2);
            e.Property(x => x.VoucherDoctype).HasMaxLength(140);
            e.Property(x => x.VoucherNo).HasMaxLength(140);
            e.HasIndex(x => new { x.VoucherDoctype, x.VoucherNo });
            e.HasIndex(x => new { x.FiscalYear, x.PostingDate });
        });

        // ── Budget Ledger ──────────────────────────────────────────────────────
        modelBuilder.Entity<BudgetLedgerRow>(e =>
        {
            e.ToTable("budget_ledger_entry", t =>
            {
                t.HasCheckConstraint("ck_ble_debit_nonneg", "debit >= 0");
                t.HasCheckConstraint("ck_ble_credit_nonneg", "credit >= 0");
                t.HasCheckConstraint("ck_ble_single_sided", "NOT (debit > 0 AND credit > 0)");
            });
            e.HasKey(x => x.LedgerSeq);
            e.Property(x => x.LedgerSeq).UseIdentityByDefaultColumn();
            e.Property(x => x.FundingSourceCode).HasMaxLength(20);
            e.Property(x => x.PapCode).HasMaxLength(40);
            e.Property(x => x.LocationCode).HasMaxLength(40);
            e.Property(x => x.ObjectAccountCode).HasMaxLength(20);
            e.Property(x => x.ExpenseClass).HasConversion<string>().HasMaxLength(8);
            e.Property(x => x.EntryType).HasConversion<string>().HasMaxLength(32);
            e.Property(x => x.Debit).HasPrecision(18, 2);
            e.Property(x => x.Credit).HasPrecision(18, 2);
            e.Property(x => x.VoucherDoctype).HasMaxLength(140);
            e.Property(x => x.VoucherNo).HasMaxLength(140);
            e.Property(x => x.AppropriationId).HasMaxLength(140);
            e.Property(x => x.AllotmentId).HasMaxLength(140);

            // RAOD/RBUD/RAPAL reporting query shapes.
            e.HasIndex(x => new { x.FiscalYear, x.ExpenseClass });
            e.HasIndex(x => new { x.FiscalYear, x.FundingSourceCode, x.ExpenseClass });
            e.HasIndex(x => new { x.AllotmentId, x.EntryType });
            e.HasIndex(x => new { x.VoucherDoctype, x.VoucherNo });

            e.HasOne<FundingSourceRow>()
                .WithMany()
                .HasForeignKey(x => x.FundingSourceCode)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // ── Funding Source ────────────────────────────────────────────────────
        modelBuilder.Entity<FundingSourceRow>(e =>
        {
            e.ToTable("funding_source");
            e.HasKey(x => x.Code);
            e.Property(x => x.Code).HasMaxLength(20);
            e.Property(x => x.Name).HasMaxLength(200);
            e.Property(x => x.ClusterCode).HasMaxLength(2);

            e.HasData(
                new FundingSourceRow { Code = "01101101", Name = "Regular Agency Fund", ClusterCode = FundCluster.RegularAgency.Code },
                new FundingSourceRow { Code = "05101101", Name = "Internally Generated Funds (STF)", ClusterCode = FundCluster.InternallyGenerated.Code });
        });

        // ── Voucher Counter ───────────────────────────────────────────────────
        modelBuilder.Entity<VoucherCounter>(e =>
        {
            e.ToTable("voucher_counter");
            e.HasKey(x => x.Series);
            e.Property(x => x.Series).HasMaxLength(64);
        });

        // ── PAP Code ──────────────────────────────────────────────────────────
        modelBuilder.Entity<PapCodeRow>(e =>
        {
            e.ToTable("pap_code");
            e.HasKey(x => x.Code);
            e.Property(x => x.Code).HasMaxLength(40);
            e.Property(x => x.Description).HasMaxLength(500);
            e.Property(x => x.ParentCode).HasMaxLength(40);
            e.HasIndex(x => x.ParentCode);
        });

        // ── Location Code ─────────────────────────────────────────────────────
        modelBuilder.Entity<LocationCodeRow>(e =>
        {
            e.ToTable("location_code");
            e.HasKey(x => x.PsgcCode);
            e.Property(x => x.PsgcCode).HasMaxLength(20);
            e.Property(x => x.LocationName).HasMaxLength(200);
            e.Property(x => x.Level).HasMaxLength(20);
            e.Property(x => x.ParentCode).HasMaxLength(20);
            e.HasIndex(x => x.ParentCode);
        });

        // ── Operational Fund ──────────────────────────────────────────────────
        modelBuilder.Entity<OperationalFundRow>(e =>
        {
            e.ToTable("operational_fund");
            e.HasKey(x => x.Code);
            e.Property(x => x.Code).HasMaxLength(40);
            e.Property(x => x.FundName).HasMaxLength(200);
            e.Property(x => x.FundType).HasMaxLength(40);
            e.Property(x => x.ParentClusterCode).HasMaxLength(20);
            e.Property(x => x.Description).HasMaxLength(500);
            e.HasIndex(x => x.FundType);
        });

        // ── Disbursement Voucher ───────────────────────────────────────────────
        modelBuilder.Entity<DisbursementVoucherRow>(e =>
        {
            e.ToTable("disbursement_voucher");
            e.HasKey(x => x.Name);
            e.Property(x => x.Name).HasMaxLength(140);
            e.Property(x => x.Encoder).HasMaxLength(140);
            e.Property(x => x.Amount).HasPrecision(18, 2);
            e.Property(x => x.FundingSourceCode).HasMaxLength(20);
            e.Property(x => x.Lifecycle).HasMaxLength(16);
            e.Property(x => x.Status).HasMaxLength(32);
            e.Property(x => x.ApprovedBy).HasMaxLength(140);
            e.Property(x => x.ApprovedForPaymentBy).HasMaxLength(140);
            e.Property(x => x.PapCode).HasMaxLength(40);
            e.Property(x => x.LocationCode).HasMaxLength(40);
            e.Property(x => x.ExpenseClass).HasConversion<string>().HasMaxLength(8);
            e.Property(x => x.ObjectAccountCode).HasMaxLength(20);
            e.HasIndex(x => x.Status);
            e.HasOne<FundingSourceRow>()
                .WithMany()
                .HasForeignKey(x => x.FundingSourceCode)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // ── Journal Entry ─────────────────────────────────────────────────────
        modelBuilder.Entity<JournalEntryRow>(e =>
        {
            e.ToTable("journal_entry");
            e.HasKey(x => x.Name);
            e.Property(x => x.Name).HasMaxLength(140);
            e.Property(x => x.Title).HasMaxLength(200);
            e.Property(x => x.FundCluster).HasMaxLength(2);
            e.Property(x => x.JeType).HasMaxLength(60);
            e.Property(x => x.ApprovalStatus).HasMaxLength(32);
            e.Property(x => x.TotalDebit).HasPrecision(18, 2);
            e.Property(x => x.TotalCredit).HasPrecision(18, 2);
            e.Property(x => x.ApprovedBy).HasMaxLength(140);
            e.Property(x => x.GlPostingReference).HasMaxLength(140);
            e.Property(x => x.UserRemark).HasMaxLength(2000);
            e.HasIndex(x => x.ApprovalStatus);
            e.HasIndex(x => new { x.FiscalYear, x.PostingDate });
        });

        modelBuilder.Entity<JeLineRow>(e =>
        {
            e.ToTable("je_line");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).UseIdentityByDefaultColumn();
            e.Property(x => x.ParentJeName).HasMaxLength(140);
            e.Property(x => x.Account).HasMaxLength(20);
            e.Property(x => x.AccountName).HasMaxLength(200);
            e.Property(x => x.Debit).HasPrecision(18, 2);
            e.Property(x => x.Credit).HasPrecision(18, 2);
            e.Property(x => x.Description).HasMaxLength(500);
            e.HasIndex(x => x.ParentJeName);
            e.HasOne(x => x.Parent)
                .WithMany(p => p.JeLines)
                .HasForeignKey(x => x.ParentJeName)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // ── Obligation Request (ORS/BURS) ──────────────────────────────────────
        modelBuilder.Entity<ObligationRequestRow>(e =>
        {
            e.ToTable("obligation_request");
            e.HasKey(x => x.Name);
            e.Property(x => x.Name).HasMaxLength(140);
            e.Property(x => x.RequestingUnit).HasMaxLength(200);
            e.Property(x => x.Purpose).HasMaxLength(1000);
            e.Property(x => x.Amount).HasPrecision(18, 2);
            e.Property(x => x.FundingSourceCode).HasMaxLength(20);
            e.Property(x => x.PapCode).HasMaxLength(40);
            e.Property(x => x.LocationCode).HasMaxLength(40);
            e.Property(x => x.ExpenseClass).HasMaxLength(8);
            e.Property(x => x.Status).HasMaxLength(32);
            e.Property(x => x.RequestingOfficeUser).HasMaxLength(140);
            e.Property(x => x.BudgetOfficerUser).HasMaxLength(140);
            e.Property(x => x.Remarks).HasMaxLength(2000);
            e.HasIndex(x => x.Status);
            e.HasIndex(x => new { x.FiscalYear, x.FundingSourceCode });
            e.HasOne<FundingSourceRow>()
                .WithMany()
                .HasForeignKey(x => x.FundingSourceCode)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<OrsLineItemRow>(e =>
        {
            e.ToTable("ors_line_item");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).UseIdentityByDefaultColumn();
            e.Property(x => x.ParentOrsName).HasMaxLength(140);
            e.Property(x => x.Particulars).HasMaxLength(500);
            e.Property(x => x.AllotmentId).HasMaxLength(140);
            e.Property(x => x.Amount).HasPrecision(18, 2);
            e.Property(x => x.PapCode).HasMaxLength(40);
            e.Property(x => x.LocationCode).HasMaxLength(40);
            e.Property(x => x.ExpenseClass).HasMaxLength(8);
            e.Property(x => x.Remarks).HasMaxLength(500);
            e.HasIndex(x => x.ParentOrsName);
            e.HasOne(x => x.Parent)
                .WithMany(p => p.LineItems)
                .HasForeignKey(x => x.ParentOrsName)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // ── Notice of Cash Allocation ─────────────────────────────────────────
        modelBuilder.Entity<NoticeOfCashAllocationRow>(e =>
        {
            e.ToTable("notice_of_cash_allocation");
            e.HasKey(x => x.NcaNumber);
            e.Property(x => x.NcaNumber).HasMaxLength(140);
            e.Property(x => x.FundingSourceCode).HasMaxLength(20);
            e.Property(x => x.Status).HasMaxLength(32);
            e.Property(x => x.NcaAmount).HasPrecision(18, 2);
            e.Property(x => x.UtilizedAmount).HasPrecision(18, 2);
            e.Property(x => x.Remarks).HasMaxLength(2000);
            e.HasIndex(x => x.Status);
            e.HasIndex(x => new { x.FiscalYear, x.FundingSourceCode });
            e.HasOne<FundingSourceRow>()
                .WithMany()
                .HasForeignKey(x => x.FundingSourceCode)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // ── Order of Payment ──────────────────────────────────────────────────
        modelBuilder.Entity<OrderOfPaymentRow>(e =>
        {
            e.ToTable("order_of_payment");
            e.HasKey(x => x.Name);
            e.Property(x => x.Name).HasMaxLength(140);
            e.Property(x => x.Customer).HasMaxLength(200);
            e.Property(x => x.Description).HasMaxLength(1000);
            e.Property(x => x.Amount).HasPrecision(18, 2);
            e.Property(x => x.FundCluster).HasMaxLength(2);
            e.Property(x => x.Status).HasMaxLength(32);
            e.Property(x => x.IssuedBy).HasMaxLength(140);
            e.Property(x => x.Remarks).HasMaxLength(2000);
            e.HasIndex(x => x.Status);
        });

        // ── Official Receipt ──────────────────────────────────────────────────
        modelBuilder.Entity<OfficialReceiptRow>(e =>
        {
            e.ToTable("official_receipt");
            e.HasKey(x => x.Name);
            e.Property(x => x.Name).HasMaxLength(140);
            e.Property(x => x.OrNumber).HasMaxLength(60);
            e.Property(x => x.OrderOfPaymentName).HasMaxLength(140);
            e.Property(x => x.Customer).HasMaxLength(200);
            e.Property(x => x.AmountPaid).HasPrecision(18, 2);
            e.Property(x => x.ModeOfPayment).HasMaxLength(40);
            e.Property(x => x.FundCluster).HasMaxLength(2);
            e.Property(x => x.CollectionStatus).HasMaxLength(32);
            e.Property(x => x.Remarks).HasMaxLength(2000);
            e.HasIndex(x => x.CollectionStatus);
            e.HasIndex(x => x.OrNumber).IsUnique();
        });

        // ── Report of Collections and Deposits ────────────────────────────────
        modelBuilder.Entity<ReportOfCollectionsRow>(e =>
        {
            e.ToTable("report_of_collections");
            e.HasKey(x => x.Name);
            e.Property(x => x.Name).HasMaxLength(140);
            e.Property(x => x.FundCluster).HasMaxLength(2);
            e.Property(x => x.CollectingOfficer).HasMaxLength(200);
            e.Property(x => x.DepositSlipNo).HasMaxLength(60);
            e.Property(x => x.DepositoryBank).HasMaxLength(200);
            e.Property(x => x.DepositAccountNumber).HasMaxLength(60);
            e.Property(x => x.TotalCollected).HasPrecision(18, 2);
            e.Property(x => x.TotalDeposited).HasPrecision(18, 2);
            e.Property(x => x.Status).HasMaxLength(32);
            e.Property(x => x.Remarks).HasMaxLength(2000);
            e.HasIndex(x => x.Status);
            e.HasIndex(x => x.ReportDate);
        });

        modelBuilder.Entity<RcdLineRow>(e =>
        {
            e.ToTable("rcd_line");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).UseIdentityByDefaultColumn();
            e.Property(x => x.ParentRcdName).HasMaxLength(140);
            e.Property(x => x.OfficialReceiptName).HasMaxLength(140);
            e.Property(x => x.OrNumber).HasMaxLength(60);
            e.Property(x => x.Payor).HasMaxLength(200);
            e.Property(x => x.ModeOfPayment).HasMaxLength(40);
            e.Property(x => x.AmountCollected).HasPrecision(18, 2);
            e.HasIndex(x => x.ParentRcdName);
            e.HasOne(x => x.Parent)
                .WithMany(p => p.Lines)
                .HasForeignKey(x => x.ParentRcdName)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // ── Cash Advance ──────────────────────────────────────────────────────
        modelBuilder.Entity<CashAdvanceRow>(e =>
        {
            e.ToTable("cash_advance");
            e.HasKey(x => x.Name);
            e.Property(x => x.Name).HasMaxLength(140);
            e.Property(x => x.Employee).HasMaxLength(140);
            e.Property(x => x.EmployeeName).HasMaxLength(200);
            e.Property(x => x.FundCluster).HasMaxLength(2);
            e.Property(x => x.Purpose).HasMaxLength(1000);
            e.Property(x => x.AdvanceAmount).HasPrecision(18, 2);
            e.Property(x => x.LiquidatedAmount).HasPrecision(18, 2);
            e.Property(x => x.UnliquidatedBalance).HasPrecision(18, 2);
            e.Property(x => x.Status).HasMaxLength(32);
            e.Property(x => x.GlPostingReference).HasMaxLength(140);
            e.Property(x => x.Remarks).HasMaxLength(2000);
            e.HasIndex(x => x.Status);
            e.HasIndex(x => x.Employee);
        });

        // ── Liquidation Report ────────────────────────────────────────────────
        modelBuilder.Entity<LiquidationReportRow>(e =>
        {
            e.ToTable("liquidation_report");
            e.HasKey(x => x.Name);
            e.Property(x => x.Name).HasMaxLength(140);
            e.Property(x => x.CashAdvanceName).HasMaxLength(140);
            e.Property(x => x.Employee).HasMaxLength(140);
            e.Property(x => x.EmployeeName).HasMaxLength(200);
            e.Property(x => x.FundCluster).HasMaxLength(2);
            e.Property(x => x.TotalLiquidated).HasPrecision(18, 2);
            e.Property(x => x.AdvanceAmount).HasPrecision(18, 2);
            e.Property(x => x.RefundDue).HasPrecision(18, 2);
            e.Property(x => x.ReimbursementDue).HasPrecision(18, 2);
            e.Property(x => x.Status).HasMaxLength(32);
            e.Property(x => x.GlPostingReference).HasMaxLength(140);
            e.Property(x => x.Remarks).HasMaxLength(2000);
            e.HasIndex(x => x.Status);
            e.HasOne<CashAdvanceRow>()
                .WithMany()
                .HasForeignKey(x => x.CashAdvanceName)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<LiquidationLineRow>(e =>
        {
            e.ToTable("liquidation_line");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).UseIdentityByDefaultColumn();
            e.Property(x => x.ParentLrName).HasMaxLength(140);
            e.Property(x => x.ExpenseType).HasMaxLength(100);
            e.Property(x => x.Description).HasMaxLength(500);
            e.Property(x => x.Amount).HasPrecision(18, 2);
            e.Property(x => x.ReceiptReference).HasMaxLength(140);
            e.Property(x => x.AccountCode).HasMaxLength(20);
            e.HasIndex(x => x.ParentLrName);
            e.HasOne(x => x.Parent)
                .WithMany(p => p.Lines)
                .HasForeignKey(x => x.ParentLrName)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // ── Regular Payroll Entry ─────────────────────────────────────────────
        modelBuilder.Entity<PayrollEntryRow>(e =>
        {
            e.ToTable("payroll_entry");
            e.HasKey(x => x.Name);
            e.Property(x => x.Name).HasMaxLength(140);
            e.Property(x => x.Title).HasMaxLength(200);
            e.Property(x => x.PayrollType).HasMaxLength(60);
            e.Property(x => x.PayrollPeriod).HasMaxLength(60);
            e.Property(x => x.FundCluster).HasMaxLength(2);
            e.Property(x => x.ImportStatus).HasMaxLength(32);
            e.Property(x => x.TotalGrossPay).HasPrecision(18, 2);
            e.Property(x => x.TotalNetPay).HasPrecision(18, 2);
            e.Property(x => x.TotalTaxWithheld).HasPrecision(18, 2);
            e.Property(x => x.TotalGsis).HasPrecision(18, 2);
            e.Property(x => x.TotalPagibig).HasPrecision(18, 2);
            e.Property(x => x.TotalPhilhealth).HasPrecision(18, 2);
            e.Property(x => x.TotalOtherDeductions).HasPrecision(18, 2);
            e.Property(x => x.Status).HasMaxLength(32);
            e.Property(x => x.GlPostingReference).HasMaxLength(140);
            e.Property(x => x.ValidationErrors).HasMaxLength(4000);
            e.Property(x => x.Remarks).HasMaxLength(2000);
            e.HasIndex(x => x.Status);
            e.HasIndex(x => x.PostingDate);
        });

        modelBuilder.Entity<PayrollLoanDeductionRow>(e =>
        {
            e.ToTable("payroll_loan_deduction");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).UseIdentityByDefaultColumn();
            e.Property(x => x.ParentPayrollName).HasMaxLength(140);
            e.Property(x => x.LoanType).HasMaxLength(60);
            e.Property(x => x.LoanReference).HasMaxLength(140);
            e.Property(x => x.EmployeeId).HasMaxLength(60);
            e.Property(x => x.EmployeeName).HasMaxLength(200);
            e.Property(x => x.Amount).HasPrecision(18, 2);
            e.HasIndex(x => x.ParentPayrollName);
            e.HasOne(x => x.Parent)
                .WithMany(p => p.LoanDeductions)
                .HasForeignKey(x => x.ParentPayrollName)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // ── JO/COS Payroll Entry ──────────────────────────────────────────────
        modelBuilder.Entity<JoCosPayrollEntryRow>(e =>
        {
            e.ToTable("jo_cos_payroll_entry");
            e.HasKey(x => x.Name);
            e.Property(x => x.Name).HasMaxLength(140);
            e.Property(x => x.EmployeeType).HasMaxLength(60);
            e.Property(x => x.PayrollPeriod).HasMaxLength(60);
            e.Property(x => x.FundCluster).HasMaxLength(2);
            e.Property(x => x.HrTransmittalReference).HasMaxLength(140);
            e.Property(x => x.TotalHours).HasPrecision(10, 2);
            e.Property(x => x.TotalDays).HasPrecision(10, 2);
            e.Property(x => x.TotalGross).HasPrecision(18, 2);
            e.Property(x => x.TotalNet).HasPrecision(18, 2);
            e.Property(x => x.ValidationRemarks).HasMaxLength(2000);
            e.Property(x => x.Status).HasMaxLength(32);
            e.Property(x => x.GlPostingReference).HasMaxLength(140);
            e.Property(x => x.Remarks).HasMaxLength(2000);
            e.HasIndex(x => x.Status);
        });

        modelBuilder.Entity<JoCosPayrollLineRow>(e =>
        {
            e.ToTable("jo_cos_payroll_line");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).UseIdentityByDefaultColumn();
            e.Property(x => x.ParentJoCosName).HasMaxLength(140);
            e.Property(x => x.EmployeeId).HasMaxLength(60);
            e.Property(x => x.EmployeeName).HasMaxLength(200);
            e.Property(x => x.EmploymentType).HasMaxLength(40);
            e.Property(x => x.AuthorizedHours).HasPrecision(10, 2);
            e.Property(x => x.ActualHours).HasPrecision(10, 2);
            e.Property(x => x.TardinessHours).HasPrecision(10, 2);
            e.Property(x => x.ComputedHours).HasPrecision(10, 2);
            e.Property(x => x.DailyRate).HasPrecision(18, 2);
            e.Property(x => x.GrossPay).HasPrecision(18, 2);
            e.Property(x => x.NetPay).HasPrecision(18, 2);
            e.Property(x => x.Remarks).HasMaxLength(500);
            e.HasIndex(x => x.ParentJoCosName);
            e.HasOne(x => x.Parent)
                .WithMany(p => p.Lines)
                .HasForeignKey(x => x.ParentJoCosName)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // ── Salary Tranche ────────────────────────────────────────────────────
        modelBuilder.Entity<SalaryTrancheRow>(e =>
        {
            e.ToTable("salary_tranche");
            e.HasKey(x => x.Name);
            e.Property(x => x.Name).HasMaxLength(140);
            e.Property(x => x.SslLaw).HasMaxLength(100);
            e.Property(x => x.DbmCircularReference).HasMaxLength(140);
            e.Property(x => x.MinSalary).HasPrecision(18, 2);
            e.Property(x => x.MaxSalary).HasPrecision(18, 2);
            e.Property(x => x.Status).HasMaxLength(32);
            e.Property(x => x.ImportStatus).HasMaxLength(32);
            e.Property(x => x.ValidationErrors).HasMaxLength(4000);
            e.Property(x => x.Remarks).HasMaxLength(2000);
            e.HasIndex(x => new { x.EffectiveYear, x.TrancheNumber });
        });

        modelBuilder.Entity<SalaryTrancheEntryRow>(e =>
        {
            e.ToTable("salary_tranche_entry");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).UseIdentityByDefaultColumn();
            e.Property(x => x.ParentTrancheName).HasMaxLength(140);
            e.Property(x => x.MonthlySalary).HasPrecision(18, 2);
            e.HasIndex(x => new { x.ParentTrancheName, x.SalaryGrade, x.Step }).IsUnique();
            e.HasOne(x => x.Parent)
                .WithMany(p => p.Entries)
                .HasForeignKey(x => x.ParentTrancheName)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // ── Employee Salary Grade ─────────────────────────────────────────────
        modelBuilder.Entity<EmployeeSalaryGradeRow>(e =>
        {
            e.ToTable("employee_salary_grade");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).UseIdentityByDefaultColumn();
            e.Property(x => x.EmployeeId).HasMaxLength(60);
            e.Property(x => x.EmployeeName).HasMaxLength(200);
            e.Property(x => x.MonthlySalary).HasPrecision(18, 2);
            e.HasIndex(x => new { x.EmployeeId, x.EffectiveDate });
        });

        // ── LDDAP-ADA ─────────────────────────────────────────────────────────
        modelBuilder.Entity<LddapAdaRow>(e =>
        {
            e.ToTable("lddap_ada");
            e.HasKey(x => x.Name);
            e.Property(x => x.Name).HasMaxLength(140);
            e.Property(x => x.FundCluster).HasMaxLength(2);
            e.Property(x => x.BankName).HasMaxLength(200);
            e.Property(x => x.BankAccountNumber).HasMaxLength(60);
            e.Property(x => x.TotalAmount).HasPrecision(18, 2);
            e.Property(x => x.ApprovalStatus).HasMaxLength(32);
            e.Property(x => x.Remarks).HasMaxLength(2000);
            e.HasIndex(x => x.ApprovalStatus);
        });

        modelBuilder.Entity<LddapAdaItemRow>(e =>
        {
            e.ToTable("lddap_ada_item");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).UseIdentityByDefaultColumn();
            e.Property(x => x.ParentLddapName).HasMaxLength(140);
            e.Property(x => x.DvReference).HasMaxLength(140);
            e.Property(x => x.PayeeName).HasMaxLength(200);
            e.Property(x => x.PayeeAccountNumber).HasMaxLength(60);
            e.Property(x => x.BankName).HasMaxLength(200);
            e.Property(x => x.NetAmount).HasPrecision(18, 2);
            e.HasIndex(x => x.ParentLddapName);
            e.HasOne<LddapAdaRow>()
                .WithMany()
                .HasForeignKey(x => x.ParentLddapName)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // ── DV Transmittal ────────────────────────────────────────────────────
        modelBuilder.Entity<DvTransmittalRow>(e =>
        {
            e.ToTable("dv_transmittal");
            e.HasKey(x => x.Name);
            e.Property(x => x.Name).HasMaxLength(140);
            e.Property(x => x.TransmittingOfficer).HasMaxLength(200);
            e.Property(x => x.ReceivingCashier).HasMaxLength(200);
            e.Property(x => x.AccountantName).HasMaxLength(200);
            e.Property(x => x.TotalAmount).HasPrecision(18, 2);
            e.Property(x => x.Status).HasMaxLength(32);
            e.Property(x => x.ReceivedByCashier).HasMaxLength(200);
            e.Property(x => x.Remarks).HasMaxLength(2000);
            e.HasIndex(x => x.Status);
        });

        modelBuilder.Entity<DvTransmittalItemRow>(e =>
        {
            e.ToTable("dv_transmittal_item");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).UseIdentityByDefaultColumn();
            e.Property(x => x.ParentTransmittalName).HasMaxLength(140);
            e.Property(x => x.DvReference).HasMaxLength(140);
            e.Property(x => x.DvAmount).HasPrecision(18, 2);
            e.Property(x => x.Remarks).HasMaxLength(500);
            e.HasIndex(x => x.ParentTransmittalName);
            e.HasOne<DvTransmittalRow>()
                .WithMany()
                .HasForeignKey(x => x.ParentTransmittalName)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // ── Audit Intake ──────────────────────────────────────────────────────
        modelBuilder.Entity<AuditIntakeRow>(e =>
        {
            e.ToTable("audit_intake");
            e.HasKey(x => x.Name);
            e.Property(x => x.Name).HasMaxLength(140);
            e.Property(x => x.DisbursementVoucherName).HasMaxLength(140);
            e.Property(x => x.AuditResult).HasMaxLength(32);
            e.Property(x => x.Findings).HasMaxLength(4000);
            e.Property(x => x.ReleasedTo).HasMaxLength(200);
            e.Property(x => x.Status).HasMaxLength(32);
            e.HasIndex(x => x.Status);
            e.HasIndex(x => x.DisbursementVoucherName);
        });

        // ── FINDES Export ─────────────────────────────────────────────────────
        modelBuilder.Entity<FindesExportRow>(e =>
        {
            e.ToTable("findes_export");
            e.HasKey(x => x.Name);
            e.Property(x => x.Name).HasMaxLength(140);
            e.Property(x => x.ExportBatch).HasMaxLength(140);
            e.Property(x => x.DvTotalAmount).HasPrecision(18, 2);
            e.Property(x => x.ExportTotalAmount).HasPrecision(18, 2);
            e.Property(x => x.Variance).HasPrecision(18, 2);
            e.Property(x => x.ApprovalStatus).HasMaxLength(32);
            e.Property(x => x.ReviewedBy).HasMaxLength(140);
            e.Property(x => x.GeneratedBy).HasMaxLength(140);
            e.Property(x => x.Remarks).HasMaxLength(2000);
            e.HasIndex(x => x.ApprovalStatus);
        });

        modelBuilder.Entity<FindesExportLineRow>(e =>
        {
            e.ToTable("findes_export_line");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).UseIdentityByDefaultColumn();
            e.Property(x => x.ParentExportName).HasMaxLength(140);
            e.Property(x => x.DvReference).HasMaxLength(140);
            e.HasIndex(x => x.ParentExportName);
            e.HasOne(x => x.Parent)
                .WithMany(p => p.Lines)
                .HasForeignKey(x => x.ParentExportName)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // ── Bank Collection Report ────────────────────────────────────────────
        modelBuilder.Entity<BankCollectionReportRow>(e =>
        {
            e.ToTable("bank_collection_report");
            e.HasKey(x => x.Name);
            e.Property(x => x.Name).HasMaxLength(140);
            e.Property(x => x.ReconciliationStatus).HasMaxLength(32);
            e.Property(x => x.TotalAmount).HasPrecision(18, 2);
            e.Property(x => x.Remarks).HasMaxLength(2000);
            e.HasIndex(x => x.ReconciliationStatus);
            e.HasIndex(x => x.ReportDate);
        });

        modelBuilder.Entity<BankCollectionLineRow>(e =>
        {
            e.ToTable("bank_collection_line");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).UseIdentityByDefaultColumn();
            e.Property(x => x.ParentReportName).HasMaxLength(140);
            e.Property(x => x.RefNo).HasMaxLength(140);
            e.Property(x => x.LbpRefNo).HasMaxLength(140);
            e.Property(x => x.Amount).HasPrecision(18, 2);
            e.Property(x => x.MatchedOrName).HasMaxLength(140);
            e.Property(x => x.Remarks).HasMaxLength(500);
            e.HasIndex(x => x.ParentReportName);
            e.HasOne(x => x.Parent)
                .WithMany(p => p.Lines)
                .HasForeignKey(x => x.ParentReportName)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // ── Push Token ────────────────────────────────────────────────────────
        modelBuilder.Entity<PushTokenRow>(e =>
        {
            e.ToTable("push_token");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).UseIdentityByDefaultColumn();
            e.Property(x => x.UserId).HasMaxLength(140);
            e.Property(x => x.Token).HasMaxLength(500);
            e.Property(x => x.Platform).HasMaxLength(20);
            e.HasIndex(x => x.UserId);
            e.HasIndex(x => x.Token);
        });

        // ── Routing Template ──────────────────────────────────────────────────
        modelBuilder.Entity<RoutingTemplateRow>(e =>
        {
            e.ToTable("routing_template");
            e.HasKey(x => x.Name);
            e.Property(x => x.Name).HasMaxLength(140);
            e.Property(x => x.TemplateName).HasMaxLength(200);
            e.Property(x => x.DocumentType).HasMaxLength(140);
            e.Property(x => x.Description).HasMaxLength(500);
            e.Property(x => x.MinAmount).HasPrecision(18, 2);
            e.Property(x => x.MaxAmount).HasPrecision(18, 2);
        });

        modelBuilder.Entity<RoutingTemplateStepRow>(e =>
        {
            e.ToTable("routing_template_step");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).UseIdentityByDefaultColumn();
            e.Property(x => x.ParentTemplateName).HasMaxLength(140);
            e.Property(x => x.OfficeName).HasMaxLength(200);
            e.Property(x => x.Role).HasMaxLength(140);
            e.Property(x => x.DurationDays).HasDefaultValue(1);
            e.Property(x => x.IsRequired).HasDefaultValue(true);
            e.HasOne(x => x.Parent)
                .WithMany(p => p.Steps)
                .HasForeignKey(x => x.ParentTemplateName)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(x => new { x.ParentTemplateName, x.StepOrder }).IsUnique();
        });

        // ── Routing Slip ──────────────────────────────────────────────────────
        modelBuilder.Entity<RoutingSlipRow>(e =>
        {
            e.ToTable("routing_slip");
            e.HasKey(x => x.Name);
            e.Property(x => x.Name).HasMaxLength(140);
            e.Property(x => x.RoutingTemplateName).HasMaxLength(140);
            e.Property(x => x.Status).HasMaxLength(32);
            e.Property(x => x.ReferenceDoctype).HasMaxLength(140);
            e.Property(x => x.ReferenceName).HasMaxLength(140);
            e.Property(x => x.CurrentOffice).HasMaxLength(200);
            e.Property(x => x.CurrentStep).HasDefaultValue(0);
            e.HasIndex(x => x.Status);
            e.HasIndex(x => new { x.ReferenceDoctype, x.ReferenceName });
        });

        modelBuilder.Entity<RoutingSlipStepRow>(e =>
        {
            e.ToTable("routing_slip_step");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).UseIdentityByDefaultColumn();
            e.Property(x => x.ParentSlipName).HasMaxLength(140);
            e.Property(x => x.OfficeName).HasMaxLength(200);
            e.Property(x => x.Status).HasMaxLength(32);
            e.Property(x => x.HandledBy).HasMaxLength(140);
            e.Property(x => x.Remarks).HasMaxLength(500);
            e.HasOne(x => x.Parent)
                .WithMany(p => p.Steps)
                .HasForeignKey(x => x.ParentSlipName)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(x => new { x.ParentSlipName, x.StepOrder }).IsUnique();
        });

        // ── Attachment Requirement ────────────────────────────────────────────
        modelBuilder.Entity<AttachmentRequirementRow>(e =>
        {
            e.ToTable("attachment_requirement");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).UseIdentityByDefaultColumn();
            e.Property(x => x.ReferenceDoctype).HasMaxLength(140);
            e.Property(x => x.WorkflowState).HasMaxLength(60);
            e.Property(x => x.RequirementCode).HasMaxLength(60);
            e.Property(x => x.RequirementLabel).HasMaxLength(200);
            e.Property(x => x.ValidationMode).HasMaxLength(60);
            e.Property(x => x.FilenameKeyword).HasMaxLength(100);
            e.Property(x => x.IsEnabled).HasDefaultValue(true);
            e.HasIndex(x => new { x.ReferenceDoctype, x.WorkflowState });
            e.HasIndex(x => new { x.ReferenceDoctype, x.RequirementCode }).IsUnique();
        });

        // ── COA Case ──────────────────────────────────────────────────────────
        modelBuilder.Entity<CoaCaseRow>(e =>
        {
            e.ToTable("coa_case");
            e.HasKey(x => x.Name);
            e.Property(x => x.Name).HasMaxLength(140);
            e.Property(x => x.NdNcReference).HasMaxLength(140);
            e.Property(x => x.NfdReference).HasMaxLength(140);
            e.Property(x => x.CoeReference).HasMaxLength(140);
            e.Property(x => x.LiableParty).HasMaxLength(200);
            e.Property(x => x.Amount).HasPrecision(18, 2);
            e.Property(x => x.SettlementMode).HasMaxLength(40);
            e.Property(x => x.OrReference).HasMaxLength(140);
            e.Property(x => x.Status).HasMaxLength(60);
            e.Property(x => x.Remarks).HasMaxLength(2000);
            e.HasIndex(x => x.Status);
        });

        // ── BIR 2307 ──────────────────────────────────────────────────────────
        modelBuilder.Entity<Bir2307Row>(e =>
        {
            e.ToTable("bir_2307");
            e.HasKey(x => x.Name);
            e.Property(x => x.Name).HasMaxLength(140);
            e.Property(x => x.DvReference).HasMaxLength(140);
            e.Property(x => x.PayeeName).HasMaxLength(200);
            e.Property(x => x.PayeeTin).HasMaxLength(15);
            e.Property(x => x.PayeeAddress).HasMaxLength(500);
            e.Property(x => x.IncomePaymentType).HasMaxLength(40);
            e.Property(x => x.GrossAmount).HasPrecision(18, 2);
            e.Property(x => x.EwtRate).HasPrecision(5, 2);
            e.Property(x => x.EwtAmount).HasPrecision(18, 2);
            e.Property(x => x.NetAmount).HasPrecision(18, 2);
            e.Property(x => x.ApprovalStatus).HasMaxLength(32);
            e.Property(x => x.ReviewedBy).HasMaxLength(140);
            e.Property(x => x.Remarks).HasMaxLength(2000);
            e.HasIndex(x => x.ApprovalStatus);
            e.HasIndex(x => x.DvReference);
        });

        // ── Withholding Tax Statement ──────────────────────────────────────────
        modelBuilder.Entity<WithholdingTaxStatementRow>(e =>
        {
            e.ToTable("withholding_tax_statement");
            e.HasKey(x => x.Name);
            e.Property(x => x.Name).HasMaxLength(140);
            e.Property(x => x.StatementType).HasMaxLength(32);
            e.Property(x => x.TaxPeriodMonth).HasMaxLength(20);
            e.Property(x => x.FundCluster).HasMaxLength(2);
            e.Property(x => x.FundingSourceCode).HasMaxLength(20);
            e.Property(x => x.PayeeName).HasMaxLength(200);
            e.Property(x => x.PayeeTin).HasMaxLength(15);
            e.Property(x => x.GrossAmount).HasPrecision(18, 2);
            e.Property(x => x.TotalTaxAmount).HasPrecision(18, 2);
            e.Property(x => x.NetAmount).HasPrecision(18, 2);
            e.Property(x => x.ApprovalStatus).HasMaxLength(32);
            e.Property(x => x.ReviewedBy).HasMaxLength(140);
            e.Property(x => x.GlPostingReference).HasMaxLength(140);
            e.Property(x => x.Remarks).HasMaxLength(2000);
            e.HasIndex(x => x.ApprovalStatus);
            e.HasIndex(x => x.PostingDate);
        });

        modelBuilder.Entity<WithholdingTaxLineRow>(e =>
        {
            e.ToTable("withholding_tax_line");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).UseIdentityByDefaultColumn();
            e.Property(x => x.ParentWhtName).HasMaxLength(140);
            e.Property(x => x.TaxType).HasMaxLength(60);
            e.Property(x => x.TaxClass).HasMaxLength(60);
            e.Property(x => x.AtcCode).HasMaxLength(20);
            e.Property(x => x.Rate).HasPrecision(5, 2);
            e.Property(x => x.TaxBase).HasPrecision(18, 2);
            e.Property(x => x.TaxAmount).HasPrecision(18, 2);
            e.Property(x => x.LiabilityAccount).HasMaxLength(20);
            e.Property(x => x.SourceDv).HasMaxLength(140);
            e.Property(x => x.Remarks).HasMaxLength(500);
            e.HasIndex(x => x.ParentWhtName);
            e.HasOne<WithholdingTaxStatementRow>()
                .WithMany()
                .HasForeignKey(x => x.ParentWhtName)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // ── State History ─────────────────────────────────────────────────────
        modelBuilder.Entity<StateHistoryRow>(e =>
        {
            e.ToTable("state_history");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).UseIdentityByDefaultColumn();
            e.Property(x => x.ReferenceDoctype).HasMaxLength(140);
            e.Property(x => x.ReferenceName).HasMaxLength(140);
            e.Property(x => x.FromState).HasMaxLength(60);
            e.Property(x => x.ToState).HasMaxLength(60);
            e.Property(x => x.Action).HasMaxLength(60);
            e.Property(x => x.ActingUser).HasMaxLength(140);
            e.Property(x => x.Remarks).HasMaxLength(2000);
            e.HasIndex(x => new { x.ReferenceDoctype, x.ReferenceName });
        });
    }
}
