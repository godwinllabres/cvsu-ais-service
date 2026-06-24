using CvSU.Ais.Domain.Common;
using CvSU.Ais.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Xunit;

namespace CvSU.Ais.Integration.Tests;

/// <summary>Proves the ledger guarantees against a real database — where the
/// CHECK constraints and the immutability interceptor actually bite.</summary>
[Collection("postgres")]
public class LedgerPersistenceTests(PostgresFixture fixture)
{
    [Fact]
    public async Task Check_constraint_rejects_a_double_sided_gl_row()
    {
        await using var db = fixture.CreateContext();

        // Raw insert bypasses the domain guards — this exercises the DB itself.
        var ex = await Assert.ThrowsAsync<PostgresException>(() =>
            db.Database.ExecuteSqlRawAsync(
                """
                INSERT INTO gl_entry (posting_date, fiscal_year, account, debit, credit, voucher_doctype, voucher_no)
                VALUES ('2026-06-24', 2026, '50203010', 100, 100, 'AIS Journal Entry', 'JE-CHK-1')
                """));

        Assert.Equal("23514", ex.SqlState); // check_violation
        Assert.Contains("ck_gl_single_sided", ex.Message);
    }

    [Fact]
    public async Task Check_constraint_rejects_a_negative_amount()
    {
        await using var db = fixture.CreateContext();

        var ex = await Assert.ThrowsAsync<PostgresException>(() =>
            db.Database.ExecuteSqlRawAsync(
                """
                INSERT INTO gl_entry (posting_date, fiscal_year, account, debit, credit, voucher_doctype, voucher_no)
                VALUES ('2026-06-24', 2026, '50203010', -50, 0, 'AIS Journal Entry', 'JE-CHK-2')
                """));

        Assert.Equal("23514", ex.SqlState);
    }

    [Fact]
    public async Task Immutability_interceptor_blocks_update_of_a_posted_ledger_row()
    {
        await using var db = fixture.CreateContext();

        var row = new GeneralLedgerRow
        {
            PostingDate = new DateOnly(2026, 6, 24),
            FiscalYear = 2026,
            Account = "50203010",
            Debit = 100m,
            Credit = 0m,
            VoucherDoctype = "AIS Journal Entry",
            VoucherNo = "JE-IMM-1",
        };
        db.Add(row);
        await db.SaveChangesAsync();

        // Mutate a persisted ledger row and try to save again.
        row.Account = "99999999";
        await Assert.ThrowsAsync<LedgerImmutabilityException>(() => db.SaveChangesAsync());
    }
}
