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
                    code = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    cluster_code = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_funding_source", x => x.code);
                });

            migrationBuilder.CreateTable(
                name: "gl_entry",
                columns: table => new
                {
                    ledger_seq = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    posting_date = table.Column<DateOnly>(type: "date", nullable: false),
                    fiscal_year = table.Column<int>(type: "integer", nullable: false),
                    account = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    debit = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    credit = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    voucher_doctype = table.Column<string>(type: "character varying(140)", maxLength: 140, nullable: false),
                    voucher_no = table.Column<string>(type: "character varying(140)", maxLength: 140, nullable: false),
                    remarks = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_gl_entry", x => x.ledger_seq);
                    table.CheckConstraint("ck_gl_credit_nonneg", "credit >= 0");
                    table.CheckConstraint("ck_gl_debit_nonneg", "debit >= 0");
                    table.CheckConstraint("ck_gl_single_sided", "NOT (debit > 0 AND credit > 0)");
                });

            migrationBuilder.CreateTable(
                name: "voucher_counter",
                columns: table => new
                {
                    series = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    current = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_voucher_counter", x => x.series);
                });

            migrationBuilder.CreateTable(
                name: "budget_ledger_entry",
                columns: table => new
                {
                    ledger_seq = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    posting_date = table.Column<DateOnly>(type: "date", nullable: false),
                    fiscal_year = table.Column<int>(type: "integer", nullable: false),
                    funding_source_code = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    pap_code = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    location_code = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    expense_class = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: false),
                    object_account_code = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    entry_type = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    debit = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    credit = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    voucher_doctype = table.Column<string>(type: "character varying(140)", maxLength: 140, nullable: false),
                    voucher_no = table.Column<string>(type: "character varying(140)", maxLength: 140, nullable: false),
                    appropriation_id = table.Column<string>(type: "character varying(140)", maxLength: 140, nullable: true),
                    allotment_id = table.Column<string>(type: "character varying(140)", maxLength: 140, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_budget_ledger_entry", x => x.ledger_seq);
                    table.CheckConstraint("ck_ble_credit_nonneg", "credit >= 0");
                    table.CheckConstraint("ck_ble_debit_nonneg", "debit >= 0");
                    table.CheckConstraint("ck_ble_single_sided", "NOT (debit > 0 AND credit > 0)");
                    table.ForeignKey(
                        name: "fk_budget_ledger_entry_funding_source_funding_source_code",
                        column: x => x.funding_source_code,
                        principalTable: "funding_source",
                        principalColumn: "code",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "disbursement_voucher",
                columns: table => new
                {
                    name = table.Column<string>(type: "character varying(140)", maxLength: 140, nullable: false),
                    encoder = table.Column<string>(type: "character varying(140)", maxLength: 140, nullable: false),
                    amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    funding_source_code = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    lifecycle = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    approved_by = table.Column<string>(type: "character varying(140)", maxLength: 140, nullable: true),
                    approved_for_payment_by = table.Column<string>(type: "character varying(140)", maxLength: 140, nullable: true),
                    budget_certified = table.Column<bool>(type: "boolean", nullable: false),
                    internal_audit_confirmed = table.Column<bool>(type: "boolean", nullable: false),
                    end_user_confirmed = table.Column<bool>(type: "boolean", nullable: false),
                    accountant_signed = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_disbursement_voucher", x => x.name);
                    table.ForeignKey(
                        name: "fk_disbursement_voucher_funding_source_funding_source_code",
                        column: x => x.funding_source_code,
                        principalTable: "funding_source",
                        principalColumn: "code",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                table: "funding_source",
                columns: new[] { "code", "cluster_code", "name" },
                values: new object[,]
                {
                    { "01101101", "01", "Regular Agency Fund" },
                    { "05101101", "05", "Internally Generated Funds (STF)" }
                });

            migrationBuilder.CreateIndex(
                name: "ix_budget_ledger_entry_allotment_id_entry_type",
                table: "budget_ledger_entry",
                columns: new[] { "allotment_id", "entry_type" });

            migrationBuilder.CreateIndex(
                name: "ix_budget_ledger_entry_fiscal_year_expense_class",
                table: "budget_ledger_entry",
                columns: new[] { "fiscal_year", "expense_class" });

            migrationBuilder.CreateIndex(
                name: "ix_budget_ledger_entry_fiscal_year_funding_source_code_expense",
                table: "budget_ledger_entry",
                columns: new[] { "fiscal_year", "funding_source_code", "expense_class" });

            migrationBuilder.CreateIndex(
                name: "ix_budget_ledger_entry_funding_source_code",
                table: "budget_ledger_entry",
                column: "funding_source_code");

            migrationBuilder.CreateIndex(
                name: "ix_budget_ledger_entry_voucher_doctype_voucher_no",
                table: "budget_ledger_entry",
                columns: new[] { "voucher_doctype", "voucher_no" });

            migrationBuilder.CreateIndex(
                name: "ix_disbursement_voucher_funding_source_code",
                table: "disbursement_voucher",
                column: "funding_source_code");

            migrationBuilder.CreateIndex(
                name: "ix_disbursement_voucher_status",
                table: "disbursement_voucher",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "ix_gl_entry_fiscal_year_posting_date",
                table: "gl_entry",
                columns: new[] { "fiscal_year", "posting_date" });

            migrationBuilder.CreateIndex(
                name: "ix_gl_entry_voucher_doctype_voucher_no",
                table: "gl_entry",
                columns: new[] { "voucher_doctype", "voucher_no" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "budget_ledger_entry");

            migrationBuilder.DropTable(
                name: "disbursement_voucher");

            migrationBuilder.DropTable(
                name: "gl_entry");

            migrationBuilder.DropTable(
                name: "voucher_counter");

            migrationBuilder.DropTable(
                name: "funding_source");
        }
    }
}
