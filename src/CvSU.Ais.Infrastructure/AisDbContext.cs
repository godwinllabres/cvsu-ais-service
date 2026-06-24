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
    public DbSet<GeneralLedgerRow> GeneralLedger => Set<GeneralLedgerRow>();
    public DbSet<BudgetLedgerRow> BudgetLedger => Set<BudgetLedgerRow>();
    public DbSet<FundingSourceRow> FundingSources => Set<FundingSourceRow>();
    public DbSet<VoucherCounter> VoucherCounters => Set<VoucherCounter>();
    public DbSet<DisbursementVoucherRow> DisbursementVouchers => Set<DisbursementVoucherRow>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
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

        modelBuilder.Entity<VoucherCounter>(e =>
        {
            e.ToTable("voucher_counter");
            e.HasKey(x => x.Series);
            e.Property(x => x.Series).HasMaxLength(64);
        });

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
    }
}
