using CvSU.Ais.Infrastructure.Numbering;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace CvSU.Ais.Integration.Tests;

[Collection("postgres")]
public class GaplessNumberingTests(PostgresFixture fixture)
{
    [Fact]
    public async Task Issues_sequential_padded_numbers()
    {
        await using var db = fixture.CreateContext();
        await using var transaction = await db.Database.BeginTransactionAsync();
        var numbers = new GaplessVoucherNumberService(db);

        var first = await numbers.NextAsync("TEST-GAP");
        var second = await numbers.NextAsync("TEST-GAP");
        await transaction.CommitAsync();

        Assert.Equal("TEST-GAP-00001", first);
        Assert.Equal("TEST-GAP-00002", second);
    }

    [Fact]
    public async Task A_rolled_back_transaction_does_not_burn_a_number()
    {
        // Issue a number, then roll back — the counter must not advance. This is the
        // exact failure mode a bare Postgres sequence would have (it leaves a gap).
        await using (var db = fixture.CreateContext())
        await using (var transaction = await db.Database.BeginTransactionAsync())
        {
            var numbers = new GaplessVoucherNumberService(db);
            await numbers.NextAsync("TEST-ROLLBACK");
            await transaction.RollbackAsync();
        }

        await using (var db = fixture.CreateContext())
        await using (var transaction = await db.Database.BeginTransactionAsync())
        {
            var numbers = new GaplessVoucherNumberService(db);
            var afterRollback = await numbers.NextAsync("TEST-ROLLBACK");
            await transaction.CommitAsync();

            Assert.Equal("TEST-ROLLBACK-00001", afterRollback); // not 00002 — no gap
        }
    }
}
