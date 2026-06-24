using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace CvSU.Ais.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "funding_source",
                columns: table => new
                {
                    Code = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    ClusterCode = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_funding_source", x => x.Code);
                });

            migrationBuilder.CreateTable(
                name: "gl_entry",
                columns: table => new
                {
                    LedgerSeq = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PostingDate = table.Column<DateOnly>(type: "date", nullable: false),
                    FiscalYear = table.Column<int>(type: "integer", nullable: false),
                    Account = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Debit = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    Credit = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    VoucherDoctype = table.Column<string>(type: "character varying(140)", maxLength: 140, nullable: false),
                    VoucherNo = table.Column<string>(type: "character varying(140)", maxLength: 140, nullable: false),
                    Remarks = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_gl_entry", x => x.LedgerSeq);
                    table.CheckConstraint("ck_gl_credit_nonneg", "credit >= 0");
                    table.CheckConstraint("ck_gl_debit_nonneg", "debit >= 0");
                    table.CheckConstraint("ck_gl_single_sided", "NOT (debit > 0 AND credit > 0)");
                });

            migrationBuilder.CreateTable(
                name: "voucher_counter",
                columns: table => new
                {
                    Series = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Current = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_voucher_counter", x => x.Series);
                });

            migrationBuilder.CreateTable(
                name: "budget_ledger_entry",
                columns: table => new
                {
                    LedgerSeq = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PostingDate = table.Column<DateOnly>(type: "date", nullable: false),
                    FiscalYear = table.Column<int>(type: "integer", nullable: false),
                    FundingSourceCode = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    PapCode = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    LocationCode = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    ExpenseClass = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: false),
                    ObjectAccountCode = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    EntryType = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Debit = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    Credit = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    VoucherDoctype = table.Column<string>(type: "character varying(140)", maxLength: 140, nullable: false),
                    VoucherNo = table.Column<string>(type: "character varying(140)", maxLength: 140, nullable: false),
                    AppropriationId = table.Column<string>(type: "character varying(140)", maxLength: 140, nullable: true),
                    AllotmentId = table.Column<string>(type: "character varying(140)", maxLength: 140, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_budget_ledger_entry", x => x.LedgerSeq);
                    table.CheckConstraint("ck_ble_credit_nonneg", "credit >= 0");
                    table.CheckConstraint("ck_ble_debit_nonneg", "debit >= 0");
                    table.CheckConstraint("ck_ble_single_sided", "NOT (debit > 0 AND credit > 0)");
                    table.ForeignKey(
                        name: "FK_budget_ledger_entry_funding_source_FundingSourceCode",
                        column: x => x.FundingSourceCode,
                        principalTable: "funding_source",
                        principalColumn: "Code",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                table: "funding_source",
                columns: new[] { "Code", "ClusterCode", "Name" },
                values: new object[,]
                {
                    { "01101101", "01", "Regular Agency Fund" },
                    { "05101101", "05", "Internally Generated Funds (STF)" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_budget_ledger_entry_AllotmentId_EntryType",
                table: "budget_ledger_entry",
                columns: new[] { "AllotmentId", "EntryType" });

            migrationBuilder.CreateIndex(
                name: "IX_budget_ledger_entry_FiscalYear_ExpenseClass",
                table: "budget_ledger_entry",
                columns: new[] { "FiscalYear", "ExpenseClass" });

            migrationBuilder.CreateIndex(
                name: "IX_budget_ledger_entry_FiscalYear_FundingSourceCode_ExpenseCla~",
                table: "budget_ledger_entry",
                columns: new[] { "FiscalYear", "FundingSourceCode", "ExpenseClass" });

            migrationBuilder.CreateIndex(
                name: "IX_budget_ledger_entry_FundingSourceCode",
                table: "budget_ledger_entry",
                column: "FundingSourceCode");

            migrationBuilder.CreateIndex(
                name: "IX_budget_ledger_entry_VoucherDoctype_VoucherNo",
                table: "budget_ledger_entry",
                columns: new[] { "VoucherDoctype", "VoucherNo" });

            migrationBuilder.CreateIndex(
                name: "IX_gl_entry_FiscalYear_PostingDate",
                table: "gl_entry",
                columns: new[] { "FiscalYear", "PostingDate" });

            migrationBuilder.CreateIndex(
                name: "IX_gl_entry_VoucherDoctype_VoucherNo",
                table: "gl_entry",
                columns: new[] { "VoucherDoctype", "VoucherNo" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "budget_ledger_entry");

            migrationBuilder.DropTable(
                name: "gl_entry");

            migrationBuilder.DropTable(
                name: "voucher_counter");

            migrationBuilder.DropTable(
                name: "funding_source");
        }
    }
}
