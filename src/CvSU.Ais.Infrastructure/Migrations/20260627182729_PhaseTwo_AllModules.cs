using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace CvSU.Ais.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class PhaseTwo_AllModules : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "attachment_requirement",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    reference_doctype = table.Column<string>(type: "character varying(140)", maxLength: 140, nullable: false),
                    workflow_state = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: true),
                    requirement_code = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    requirement_label = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    validation_mode = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    filename_keyword = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    is_enabled = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_attachment_requirement", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "audit_intake",
                columns: table => new
                {
                    name = table.Column<string>(type: "character varying(140)", maxLength: 140, nullable: false),
                    disbursement_voucher_name = table.Column<string>(type: "character varying(140)", maxLength: 140, nullable: false),
                    received_timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    recorded_timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    audit_result = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    findings = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    released_timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    released_to = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_audit_intake", x => x.name);
                });

            migrationBuilder.CreateTable(
                name: "bank_collection_report",
                columns: table => new
                {
                    name = table.Column<string>(type: "character varying(140)", maxLength: 140, nullable: false),
                    report_date = table.Column<DateOnly>(type: "date", nullable: false),
                    reconciliation_status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    total_lines = table.Column<int>(type: "integer", nullable: false),
                    total_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    exceptions_count = table.Column<int>(type: "integer", nullable: false),
                    remarks = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_bank_collection_report", x => x.name);
                });

            migrationBuilder.CreateTable(
                name: "bir_2307",
                columns: table => new
                {
                    name = table.Column<string>(type: "character varying(140)", maxLength: 140, nullable: false),
                    dv_reference = table.Column<string>(type: "character varying(140)", maxLength: 140, nullable: false),
                    period_from = table.Column<DateOnly>(type: "date", nullable: false),
                    period_to = table.Column<DateOnly>(type: "date", nullable: false),
                    payee_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    payee_tin = table.Column<string>(type: "character varying(15)", maxLength: 15, nullable: false),
                    payee_address = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    income_payment_type = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    gross_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    ewt_rate = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    ewt_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    net_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    approval_status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    reviewed_by = table.Column<string>(type: "character varying(140)", maxLength: 140, nullable: true),
                    reviewed_on = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    remarks = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_bir_2307", x => x.name);
                });

            migrationBuilder.CreateTable(
                name: "cash_advance",
                columns: table => new
                {
                    name = table.Column<string>(type: "character varying(140)", maxLength: 140, nullable: false),
                    employee = table.Column<string>(type: "character varying(140)", maxLength: 140, nullable: false),
                    employee_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    posting_date = table.Column<DateOnly>(type: "date", nullable: false),
                    fund_cluster = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: true),
                    purpose = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    advance_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    liquidated_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    unliquidated_balance = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    due_date = table.Column<DateOnly>(type: "date", nullable: false),
                    status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    gl_posting_reference = table.Column<string>(type: "character varying(140)", maxLength: 140, nullable: true),
                    remarks = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_cash_advance", x => x.name);
                });

            migrationBuilder.CreateTable(
                name: "coa_case",
                columns: table => new
                {
                    name = table.Column<string>(type: "character varying(140)", maxLength: 140, nullable: false),
                    nd_nc_reference = table.Column<string>(type: "character varying(140)", maxLength: 140, nullable: false),
                    nfd_reference = table.Column<string>(type: "character varying(140)", maxLength: 140, nullable: true),
                    coe_reference = table.Column<string>(type: "character varying(140)", maxLength: 140, nullable: true),
                    liable_party = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    settlement_mode = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    or_reference = table.Column<string>(type: "character varying(140)", maxLength: 140, nullable: true),
                    status = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    remarks = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_coa_case", x => x.name);
                });

            migrationBuilder.CreateTable(
                name: "dv_transmittal",
                columns: table => new
                {
                    name = table.Column<string>(type: "character varying(140)", maxLength: 140, nullable: false),
                    transmittal_date = table.Column<DateOnly>(type: "date", nullable: false),
                    transmitting_officer = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    receiving_cashier = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    accountant_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    accountant_signature_confirmed = table.Column<bool>(type: "boolean", nullable: false),
                    total_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    total_dv_count = table.Column<int>(type: "integer", nullable: false),
                    status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    received_by_cashier = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    received_date = table.Column<DateOnly>(type: "date", nullable: true),
                    remarks = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_dv_transmittal", x => x.name);
                });

            migrationBuilder.CreateTable(
                name: "employee_salary_grade",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    employee_id = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    employee_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    salary_grade = table.Column<int>(type: "integer", nullable: false),
                    step = table.Column<int>(type: "integer", nullable: false),
                    monthly_salary = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    effective_date = table.Column<DateOnly>(type: "date", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_employee_salary_grade", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "findes_export",
                columns: table => new
                {
                    name = table.Column<string>(type: "character varying(140)", maxLength: 140, nullable: false),
                    export_batch = table.Column<string>(type: "character varying(140)", maxLength: 140, nullable: true),
                    export_date = table.Column<DateOnly>(type: "date", nullable: false),
                    dv_total_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    export_total_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    variance = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    variance_acceptable = table.Column<bool>(type: "boolean", nullable: false),
                    approval_status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    reviewed_by = table.Column<string>(type: "character varying(140)", maxLength: 140, nullable: true),
                    reviewed_on = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    generated_by = table.Column<string>(type: "character varying(140)", maxLength: 140, nullable: true),
                    generated_on = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    remarks = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_findes_export", x => x.name);
                });

            migrationBuilder.CreateTable(
                name: "jo_cos_payroll_entry",
                columns: table => new
                {
                    name = table.Column<string>(type: "character varying(140)", maxLength: 140, nullable: false),
                    employee_type = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    payroll_period = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    period_from = table.Column<DateOnly>(type: "date", nullable: true),
                    period_to = table.Column<DateOnly>(type: "date", nullable: true),
                    posting_date = table.Column<DateOnly>(type: "date", nullable: false),
                    fund_cluster = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: true),
                    hr_transmittal_reference = table.Column<string>(type: "character varying(140)", maxLength: 140, nullable: true),
                    hr_transmittal_received_date = table.Column<DateOnly>(type: "date", nullable: true),
                    total_hours = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    total_days = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    total_gross = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    total_net = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    hours_validated = table.Column<bool>(type: "boolean", nullable: false),
                    dtr_validated = table.Column<bool>(type: "boolean", nullable: false),
                    accomplishment_validated = table.Column<bool>(type: "boolean", nullable: false),
                    validation_remarks = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    gl_posting_reference = table.Column<string>(type: "character varying(140)", maxLength: 140, nullable: true),
                    remarks = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_jo_cos_payroll_entry", x => x.name);
                });

            migrationBuilder.CreateTable(
                name: "journal_entry",
                columns: table => new
                {
                    name = table.Column<string>(type: "character varying(140)", maxLength: 140, nullable: false),
                    title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    posting_date = table.Column<DateOnly>(type: "date", nullable: false),
                    fiscal_year = table.Column<int>(type: "integer", nullable: false),
                    fund_cluster = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: true),
                    je_type = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    approval_status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    total_debit = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    total_credit = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    approved_by = table.Column<string>(type: "character varying(140)", maxLength: 140, nullable: true),
                    approved_on = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    gl_posting_reference = table.Column<string>(type: "character varying(140)", maxLength: 140, nullable: true),
                    user_remark = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_journal_entry", x => x.name);
                });

            migrationBuilder.CreateTable(
                name: "lddap_ada",
                columns: table => new
                {
                    name = table.Column<string>(type: "character varying(140)", maxLength: 140, nullable: false),
                    period_from = table.Column<DateOnly>(type: "date", nullable: false),
                    period_to = table.Column<DateOnly>(type: "date", nullable: false),
                    fund_cluster = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: true),
                    bank_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    bank_account_number = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    total_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    total_payees = table.Column<int>(type: "integer", nullable: false),
                    approval_status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    transmitted_date = table.Column<DateOnly>(type: "date", nullable: true),
                    remarks = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_lddap_ada", x => x.name);
                });

            migrationBuilder.CreateTable(
                name: "location_code",
                columns: table => new
                {
                    psgc_code = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    location_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    level = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    parent_code = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    is_group = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_location_code", x => x.psgc_code);
                });

            migrationBuilder.CreateTable(
                name: "notice_of_cash_allocation",
                columns: table => new
                {
                    nca_number = table.Column<string>(type: "character varying(140)", maxLength: 140, nullable: false),
                    date_received = table.Column<DateOnly>(type: "date", nullable: false),
                    fiscal_year = table.Column<int>(type: "integer", nullable: false),
                    funding_source_code = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    validity_date = table.Column<DateOnly>(type: "date", nullable: false),
                    status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    nca_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    utilized_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    remarks = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_notice_of_cash_allocation", x => x.nca_number);
                    table.ForeignKey(
                        name: "fk_notice_of_cash_allocation_funding_source_funding_source_code",
                        column: x => x.funding_source_code,
                        principalTable: "funding_source",
                        principalColumn: "code",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "obligation_request",
                columns: table => new
                {
                    name = table.Column<string>(type: "character varying(140)", maxLength: 140, nullable: false),
                    posting_date = table.Column<DateOnly>(type: "date", nullable: false),
                    fiscal_year = table.Column<int>(type: "integer", nullable: false),
                    requesting_unit = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    purpose = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    funding_source_code = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    pap_code = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    location_code = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    expense_class = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: true),
                    status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    requesting_office_user = table.Column<string>(type: "character varying(140)", maxLength: 140, nullable: true),
                    budget_officer_user = table.Column<string>(type: "character varying(140)", maxLength: 140, nullable: true),
                    remarks = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_obligation_request", x => x.name);
                    table.ForeignKey(
                        name: "fk_obligation_request_funding_source_funding_source_code",
                        column: x => x.funding_source_code,
                        principalTable: "funding_source",
                        principalColumn: "code",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "official_receipt",
                columns: table => new
                {
                    name = table.Column<string>(type: "character varying(140)", maxLength: 140, nullable: false),
                    or_number = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    posting_date = table.Column<DateOnly>(type: "date", nullable: false),
                    order_of_payment_name = table.Column<string>(type: "character varying(140)", maxLength: 140, nullable: true),
                    customer = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    amount_paid = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    mode_of_payment = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    fund_cluster = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: true),
                    collection_status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    remarks = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_official_receipt", x => x.name);
                });

            migrationBuilder.CreateTable(
                name: "operational_fund",
                columns: table => new
                {
                    code = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    fund_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    fund_type = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    parent_cluster_code = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_operational_fund", x => x.code);
                });

            migrationBuilder.CreateTable(
                name: "order_of_payment",
                columns: table => new
                {
                    name = table.Column<string>(type: "character varying(140)", maxLength: 140, nullable: false),
                    order_date = table.Column<DateOnly>(type: "date", nullable: false),
                    customer = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    fund_cluster = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: true),
                    status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    issued_by = table.Column<string>(type: "character varying(140)", maxLength: 140, nullable: true),
                    remarks = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_order_of_payment", x => x.name);
                });

            migrationBuilder.CreateTable(
                name: "pap_code",
                columns: table => new
                {
                    code = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    parent_code = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    is_group = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_pap_code", x => x.code);
                });

            migrationBuilder.CreateTable(
                name: "payroll_entry",
                columns: table => new
                {
                    name = table.Column<string>(type: "character varying(140)", maxLength: 140, nullable: false),
                    title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    payroll_type = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    payroll_period = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    posting_date = table.Column<DateOnly>(type: "date", nullable: false),
                    fund_cluster = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: true),
                    import_status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    total_records = table.Column<int>(type: "integer", nullable: false),
                    total_gross_pay = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    total_net_pay = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    total_tax_withheld = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    total_gsis = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    total_pagibig = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    total_philhealth = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    total_other_deductions = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    gl_posting_reference = table.Column<string>(type: "character varying(140)", maxLength: 140, nullable: true),
                    validation_errors = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    remarks = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_payroll_entry", x => x.name);
                });

            migrationBuilder.CreateTable(
                name: "push_token",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    user_id = table.Column<string>(type: "character varying(140)", maxLength: 140, nullable: false),
                    token = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    platform = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    registered_on = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    last_used_on = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_push_token", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "report_of_collections",
                columns: table => new
                {
                    name = table.Column<string>(type: "character varying(140)", maxLength: 140, nullable: false),
                    report_date = table.Column<DateOnly>(type: "date", nullable: false),
                    fiscal_year = table.Column<int>(type: "integer", nullable: false),
                    fund_cluster = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: true),
                    collecting_officer = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    deposit_slip_no = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    deposit_date = table.Column<DateOnly>(type: "date", nullable: false),
                    depository_bank = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    deposit_account_number = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: true),
                    total_collected = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    total_deposited = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    remarks = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_report_of_collections", x => x.name);
                });

            migrationBuilder.CreateTable(
                name: "routing_slip",
                columns: table => new
                {
                    name = table.Column<string>(type: "character varying(140)", maxLength: 140, nullable: false),
                    routing_template_name = table.Column<string>(type: "character varying(140)", maxLength: 140, nullable: false),
                    status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    reference_doctype = table.Column<string>(type: "character varying(140)", maxLength: 140, nullable: true),
                    reference_name = table.Column<string>(type: "character varying(140)", maxLength: 140, nullable: true),
                    current_step = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    current_office = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    started_on = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    completed_on = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_routing_slip", x => x.name);
                });

            migrationBuilder.CreateTable(
                name: "routing_template",
                columns: table => new
                {
                    name = table.Column<string>(type: "character varying(140)", maxLength: 140, nullable: false),
                    template_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    document_type = table.Column<string>(type: "character varying(140)", maxLength: 140, nullable: true),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    min_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    max_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_routing_template", x => x.name);
                });

            migrationBuilder.CreateTable(
                name: "salary_tranche",
                columns: table => new
                {
                    name = table.Column<string>(type: "character varying(140)", maxLength: 140, nullable: false),
                    ssl_law = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    tranche_number = table.Column<int>(type: "integer", nullable: false),
                    effective_year = table.Column<int>(type: "integer", nullable: false),
                    effective_date = table.Column<DateOnly>(type: "date", nullable: true),
                    dbm_circular_reference = table.Column<string>(type: "character varying(140)", maxLength: 140, nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    total_entries = table.Column<int>(type: "integer", nullable: false),
                    min_salary = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    max_salary = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    import_status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    validation_errors = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    remarks = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_salary_tranche", x => x.name);
                });

            migrationBuilder.CreateTable(
                name: "state_history",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    reference_doctype = table.Column<string>(type: "character varying(140)", maxLength: 140, nullable: false),
                    reference_name = table.Column<string>(type: "character varying(140)", maxLength: 140, nullable: false),
                    from_state = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    to_state = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    action = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    acting_user = table.Column<string>(type: "character varying(140)", maxLength: 140, nullable: false),
                    timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    remarks = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_state_history", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "withholding_tax_statement",
                columns: table => new
                {
                    name = table.Column<string>(type: "character varying(140)", maxLength: 140, nullable: false),
                    statement_type = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    posting_date = table.Column<DateOnly>(type: "date", nullable: false),
                    tax_period_month = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    fund_cluster = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: true),
                    funding_source_code = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    payee_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    payee_tin = table.Column<string>(type: "character varying(15)", maxLength: 15, nullable: true),
                    gross_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    total_tax_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    net_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    approval_status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    reviewed_by = table.Column<string>(type: "character varying(140)", maxLength: 140, nullable: true),
                    reviewed_on = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    gl_posting_reference = table.Column<string>(type: "character varying(140)", maxLength: 140, nullable: true),
                    remarks = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_withholding_tax_statement", x => x.name);
                });

            migrationBuilder.CreateTable(
                name: "bank_collection_line",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    parent_report_name = table.Column<string>(type: "character varying(140)", maxLength: 140, nullable: false),
                    ref_no = table.Column<string>(type: "character varying(140)", maxLength: 140, nullable: false),
                    lbp_ref_no = table.Column<string>(type: "character varying(140)", maxLength: 140, nullable: true),
                    amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    is_matched = table.Column<bool>(type: "boolean", nullable: false),
                    matched_or_name = table.Column<string>(type: "character varying(140)", maxLength: 140, nullable: true),
                    remarks = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_bank_collection_line", x => x.id);
                    table.ForeignKey(
                        name: "fk_bank_collection_line_bank_collection_report_parent_report_n",
                        column: x => x.parent_report_name,
                        principalTable: "bank_collection_report",
                        principalColumn: "name",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "liquidation_report",
                columns: table => new
                {
                    name = table.Column<string>(type: "character varying(140)", maxLength: 140, nullable: false),
                    cash_advance_name = table.Column<string>(type: "character varying(140)", maxLength: 140, nullable: false),
                    employee = table.Column<string>(type: "character varying(140)", maxLength: 140, nullable: false),
                    employee_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    posting_date = table.Column<DateOnly>(type: "date", nullable: false),
                    fund_cluster = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: true),
                    total_liquidated = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    advance_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    refund_due = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    reimbursement_due = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    gl_posting_reference = table.Column<string>(type: "character varying(140)", maxLength: 140, nullable: true),
                    remarks = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_liquidation_report", x => x.name);
                    table.ForeignKey(
                        name: "fk_liquidation_report_cash_advance_cash_advance_name",
                        column: x => x.cash_advance_name,
                        principalTable: "cash_advance",
                        principalColumn: "name",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "dv_transmittal_item",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    parent_transmittal_name = table.Column<string>(type: "character varying(140)", maxLength: 140, nullable: false),
                    dv_reference = table.Column<string>(type: "character varying(140)", maxLength: 140, nullable: false),
                    dv_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    remarks = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_dv_transmittal_item", x => x.id);
                    table.ForeignKey(
                        name: "fk_dv_transmittal_item_dv_transmittal_parent_transmittal_name",
                        column: x => x.parent_transmittal_name,
                        principalTable: "dv_transmittal",
                        principalColumn: "name",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "findes_export_line",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    parent_export_name = table.Column<string>(type: "character varying(140)", maxLength: 140, nullable: false),
                    dv_reference = table.Column<string>(type: "character varying(140)", maxLength: 140, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_findes_export_line", x => x.id);
                    table.ForeignKey(
                        name: "fk_findes_export_line_findes_export_parent_export_name",
                        column: x => x.parent_export_name,
                        principalTable: "findes_export",
                        principalColumn: "name",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "jo_cos_payroll_line",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    parent_jo_cos_name = table.Column<string>(type: "character varying(140)", maxLength: 140, nullable: false),
                    employee_id = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    employee_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    employment_type = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    authorized_hours = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    actual_hours = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    tardiness_hours = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    computed_hours = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    daily_rate = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    gross_pay = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    net_pay = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    discrepancy_flag = table.Column<bool>(type: "boolean", nullable: false),
                    remarks = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_jo_cos_payroll_line", x => x.id);
                    table.ForeignKey(
                        name: "fk_jo_cos_payroll_line_jo_cos_payroll_entry_parent_jo_cos_name",
                        column: x => x.parent_jo_cos_name,
                        principalTable: "jo_cos_payroll_entry",
                        principalColumn: "name",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "je_line",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    parent_je_name = table.Column<string>(type: "character varying(140)", maxLength: 140, nullable: false),
                    account = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    account_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    debit = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    credit = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_je_line", x => x.id);
                    table.ForeignKey(
                        name: "fk_je_line_journal_entry_parent_je_name",
                        column: x => x.parent_je_name,
                        principalTable: "journal_entry",
                        principalColumn: "name",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "lddap_ada_item",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    parent_lddap_name = table.Column<string>(type: "character varying(140)", maxLength: 140, nullable: false),
                    dv_reference = table.Column<string>(type: "character varying(140)", maxLength: 140, nullable: false),
                    payee_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    payee_account_number = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: true),
                    bank_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    net_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_lddap_ada_item", x => x.id);
                    table.ForeignKey(
                        name: "fk_lddap_ada_item_lddap_ada_parent_lddap_name",
                        column: x => x.parent_lddap_name,
                        principalTable: "lddap_ada",
                        principalColumn: "name",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ors_line_item",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    parent_ors_name = table.Column<string>(type: "character varying(140)", maxLength: 140, nullable: false),
                    particulars = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    allotment_id = table.Column<string>(type: "character varying(140)", maxLength: 140, nullable: true),
                    amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    pap_code = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    location_code = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    expense_class = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: true),
                    remarks = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_ors_line_item", x => x.id);
                    table.ForeignKey(
                        name: "fk_ors_line_item_obligation_request_parent_ors_name",
                        column: x => x.parent_ors_name,
                        principalTable: "obligation_request",
                        principalColumn: "name",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "payroll_loan_deduction",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    parent_payroll_name = table.Column<string>(type: "character varying(140)", maxLength: 140, nullable: false),
                    loan_type = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    loan_reference = table.Column<string>(type: "character varying(140)", maxLength: 140, nullable: true),
                    employee_id = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    employee_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_payroll_loan_deduction", x => x.id);
                    table.ForeignKey(
                        name: "fk_payroll_loan_deduction_payroll_entry_parent_payroll_name",
                        column: x => x.parent_payroll_name,
                        principalTable: "payroll_entry",
                        principalColumn: "name",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "rcd_line",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    parent_rcd_name = table.Column<string>(type: "character varying(140)", maxLength: 140, nullable: false),
                    official_receipt_name = table.Column<string>(type: "character varying(140)", maxLength: 140, nullable: false),
                    or_number = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: true),
                    posting_date = table.Column<DateOnly>(type: "date", nullable: true),
                    payor = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    mode_of_payment = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    amount_collected = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_rcd_line", x => x.id);
                    table.ForeignKey(
                        name: "fk_rcd_line_report_of_collections_parent_rcd_name",
                        column: x => x.parent_rcd_name,
                        principalTable: "report_of_collections",
                        principalColumn: "name",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "routing_slip_step",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    parent_slip_name = table.Column<string>(type: "character varying(140)", maxLength: 140, nullable: false),
                    step_order = table.Column<int>(type: "integer", nullable: false),
                    office_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    started_on = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    completed_on = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    handled_by = table.Column<string>(type: "character varying(140)", maxLength: 140, nullable: true),
                    remarks = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_routing_slip_step", x => x.id);
                    table.ForeignKey(
                        name: "fk_routing_slip_step_routing_slip_parent_slip_name",
                        column: x => x.parent_slip_name,
                        principalTable: "routing_slip",
                        principalColumn: "name",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "routing_template_step",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    parent_template_name = table.Column<string>(type: "character varying(140)", maxLength: 140, nullable: false),
                    step_order = table.Column<int>(type: "integer", nullable: false),
                    office_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    role = table.Column<string>(type: "character varying(140)", maxLength: 140, nullable: true),
                    duration_days = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                    is_required = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_routing_template_step", x => x.id);
                    table.ForeignKey(
                        name: "fk_routing_template_step_routing_template_parent_template_name",
                        column: x => x.parent_template_name,
                        principalTable: "routing_template",
                        principalColumn: "name",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "salary_tranche_entry",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    parent_tranche_name = table.Column<string>(type: "character varying(140)", maxLength: 140, nullable: false),
                    salary_grade = table.Column<int>(type: "integer", nullable: false),
                    step = table.Column<int>(type: "integer", nullable: false),
                    monthly_salary = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_salary_tranche_entry", x => x.id);
                    table.ForeignKey(
                        name: "fk_salary_tranche_entry_salary_tranche_parent_tranche_name",
                        column: x => x.parent_tranche_name,
                        principalTable: "salary_tranche",
                        principalColumn: "name",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "withholding_tax_line",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    parent_wht_name = table.Column<string>(type: "character varying(140)", maxLength: 140, nullable: false),
                    tax_type = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    tax_class = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: true),
                    atc_code = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    rate = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    tax_base = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    tax_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    liability_account = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    source_dv = table.Column<string>(type: "character varying(140)", maxLength: 140, nullable: true),
                    remarks = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_withholding_tax_line", x => x.id);
                    table.ForeignKey(
                        name: "fk_withholding_tax_line_withholding_tax_statement_parent_wht_n",
                        column: x => x.parent_wht_name,
                        principalTable: "withholding_tax_statement",
                        principalColumn: "name",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "liquidation_line",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    parent_lr_name = table.Column<string>(type: "character varying(140)", maxLength: 140, nullable: false),
                    expense_type = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    receipt_reference = table.Column<string>(type: "character varying(140)", maxLength: 140, nullable: true),
                    receipt_date = table.Column<DateOnly>(type: "date", nullable: true),
                    account_code = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_liquidation_line", x => x.id);
                    table.ForeignKey(
                        name: "fk_liquidation_line_liquidation_report_parent_lr_name",
                        column: x => x.parent_lr_name,
                        principalTable: "liquidation_report",
                        principalColumn: "name",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_attachment_requirement_reference_doctype_requirement_code",
                table: "attachment_requirement",
                columns: new[] { "reference_doctype", "requirement_code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_attachment_requirement_reference_doctype_workflow_state",
                table: "attachment_requirement",
                columns: new[] { "reference_doctype", "workflow_state" });

            migrationBuilder.CreateIndex(
                name: "ix_audit_intake_disbursement_voucher_name",
                table: "audit_intake",
                column: "disbursement_voucher_name");

            migrationBuilder.CreateIndex(
                name: "ix_audit_intake_status",
                table: "audit_intake",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "ix_bank_collection_line_parent_report_name",
                table: "bank_collection_line",
                column: "parent_report_name");

            migrationBuilder.CreateIndex(
                name: "ix_bank_collection_report_reconciliation_status",
                table: "bank_collection_report",
                column: "reconciliation_status");

            migrationBuilder.CreateIndex(
                name: "ix_bank_collection_report_report_date",
                table: "bank_collection_report",
                column: "report_date");

            migrationBuilder.CreateIndex(
                name: "ix_bir_2307_approval_status",
                table: "bir_2307",
                column: "approval_status");

            migrationBuilder.CreateIndex(
                name: "ix_bir_2307_dv_reference",
                table: "bir_2307",
                column: "dv_reference");

            migrationBuilder.CreateIndex(
                name: "ix_cash_advance_employee",
                table: "cash_advance",
                column: "employee");

            migrationBuilder.CreateIndex(
                name: "ix_cash_advance_status",
                table: "cash_advance",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "ix_coa_case_status",
                table: "coa_case",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "ix_dv_transmittal_status",
                table: "dv_transmittal",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "ix_dv_transmittal_item_parent_transmittal_name",
                table: "dv_transmittal_item",
                column: "parent_transmittal_name");

            migrationBuilder.CreateIndex(
                name: "ix_employee_salary_grade_employee_id_effective_date",
                table: "employee_salary_grade",
                columns: new[] { "employee_id", "effective_date" });

            migrationBuilder.CreateIndex(
                name: "ix_findes_export_approval_status",
                table: "findes_export",
                column: "approval_status");

            migrationBuilder.CreateIndex(
                name: "ix_findes_export_line_parent_export_name",
                table: "findes_export_line",
                column: "parent_export_name");

            migrationBuilder.CreateIndex(
                name: "ix_je_line_parent_je_name",
                table: "je_line",
                column: "parent_je_name");

            migrationBuilder.CreateIndex(
                name: "ix_jo_cos_payroll_entry_status",
                table: "jo_cos_payroll_entry",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "ix_jo_cos_payroll_line_parent_jo_cos_name",
                table: "jo_cos_payroll_line",
                column: "parent_jo_cos_name");

            migrationBuilder.CreateIndex(
                name: "ix_journal_entry_approval_status",
                table: "journal_entry",
                column: "approval_status");

            migrationBuilder.CreateIndex(
                name: "ix_journal_entry_fiscal_year_posting_date",
                table: "journal_entry",
                columns: new[] { "fiscal_year", "posting_date" });

            migrationBuilder.CreateIndex(
                name: "ix_lddap_ada_approval_status",
                table: "lddap_ada",
                column: "approval_status");

            migrationBuilder.CreateIndex(
                name: "ix_lddap_ada_item_parent_lddap_name",
                table: "lddap_ada_item",
                column: "parent_lddap_name");

            migrationBuilder.CreateIndex(
                name: "ix_liquidation_line_parent_lr_name",
                table: "liquidation_line",
                column: "parent_lr_name");

            migrationBuilder.CreateIndex(
                name: "ix_liquidation_report_cash_advance_name",
                table: "liquidation_report",
                column: "cash_advance_name");

            migrationBuilder.CreateIndex(
                name: "ix_liquidation_report_status",
                table: "liquidation_report",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "ix_location_code_parent_code",
                table: "location_code",
                column: "parent_code");

            migrationBuilder.CreateIndex(
                name: "ix_notice_of_cash_allocation_fiscal_year_funding_source_code",
                table: "notice_of_cash_allocation",
                columns: new[] { "fiscal_year", "funding_source_code" });

            migrationBuilder.CreateIndex(
                name: "ix_notice_of_cash_allocation_funding_source_code",
                table: "notice_of_cash_allocation",
                column: "funding_source_code");

            migrationBuilder.CreateIndex(
                name: "ix_notice_of_cash_allocation_status",
                table: "notice_of_cash_allocation",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "ix_obligation_request_fiscal_year_funding_source_code",
                table: "obligation_request",
                columns: new[] { "fiscal_year", "funding_source_code" });

            migrationBuilder.CreateIndex(
                name: "ix_obligation_request_funding_source_code",
                table: "obligation_request",
                column: "funding_source_code");

            migrationBuilder.CreateIndex(
                name: "ix_obligation_request_status",
                table: "obligation_request",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "ix_official_receipt_collection_status",
                table: "official_receipt",
                column: "collection_status");

            migrationBuilder.CreateIndex(
                name: "ix_official_receipt_or_number",
                table: "official_receipt",
                column: "or_number",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_operational_fund_fund_type",
                table: "operational_fund",
                column: "fund_type");

            migrationBuilder.CreateIndex(
                name: "ix_order_of_payment_status",
                table: "order_of_payment",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "ix_ors_line_item_parent_ors_name",
                table: "ors_line_item",
                column: "parent_ors_name");

            migrationBuilder.CreateIndex(
                name: "ix_pap_code_parent_code",
                table: "pap_code",
                column: "parent_code");

            migrationBuilder.CreateIndex(
                name: "ix_payroll_entry_posting_date",
                table: "payroll_entry",
                column: "posting_date");

            migrationBuilder.CreateIndex(
                name: "ix_payroll_entry_status",
                table: "payroll_entry",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "ix_payroll_loan_deduction_parent_payroll_name",
                table: "payroll_loan_deduction",
                column: "parent_payroll_name");

            migrationBuilder.CreateIndex(
                name: "ix_push_token_token",
                table: "push_token",
                column: "token");

            migrationBuilder.CreateIndex(
                name: "ix_push_token_user_id",
                table: "push_token",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_rcd_line_parent_rcd_name",
                table: "rcd_line",
                column: "parent_rcd_name");

            migrationBuilder.CreateIndex(
                name: "ix_report_of_collections_report_date",
                table: "report_of_collections",
                column: "report_date");

            migrationBuilder.CreateIndex(
                name: "ix_report_of_collections_status",
                table: "report_of_collections",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "ix_routing_slip_reference_doctype_reference_name",
                table: "routing_slip",
                columns: new[] { "reference_doctype", "reference_name" });

            migrationBuilder.CreateIndex(
                name: "ix_routing_slip_status",
                table: "routing_slip",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "ix_routing_slip_step_parent_slip_name_step_order",
                table: "routing_slip_step",
                columns: new[] { "parent_slip_name", "step_order" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_routing_template_step_parent_template_name_step_order",
                table: "routing_template_step",
                columns: new[] { "parent_template_name", "step_order" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_salary_tranche_effective_year_tranche_number",
                table: "salary_tranche",
                columns: new[] { "effective_year", "tranche_number" });

            migrationBuilder.CreateIndex(
                name: "ix_salary_tranche_entry_parent_tranche_name_salary_grade_step",
                table: "salary_tranche_entry",
                columns: new[] { "parent_tranche_name", "salary_grade", "step" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_state_history_reference_doctype_reference_name",
                table: "state_history",
                columns: new[] { "reference_doctype", "reference_name" });

            migrationBuilder.CreateIndex(
                name: "ix_withholding_tax_line_parent_wht_name",
                table: "withholding_tax_line",
                column: "parent_wht_name");

            migrationBuilder.CreateIndex(
                name: "ix_withholding_tax_statement_approval_status",
                table: "withholding_tax_statement",
                column: "approval_status");

            migrationBuilder.CreateIndex(
                name: "ix_withholding_tax_statement_posting_date",
                table: "withholding_tax_statement",
                column: "posting_date");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "attachment_requirement");

            migrationBuilder.DropTable(
                name: "audit_intake");

            migrationBuilder.DropTable(
                name: "bank_collection_line");

            migrationBuilder.DropTable(
                name: "bir_2307");

            migrationBuilder.DropTable(
                name: "coa_case");

            migrationBuilder.DropTable(
                name: "dv_transmittal_item");

            migrationBuilder.DropTable(
                name: "employee_salary_grade");

            migrationBuilder.DropTable(
                name: "findes_export_line");

            migrationBuilder.DropTable(
                name: "je_line");

            migrationBuilder.DropTable(
                name: "jo_cos_payroll_line");

            migrationBuilder.DropTable(
                name: "lddap_ada_item");

            migrationBuilder.DropTable(
                name: "liquidation_line");

            migrationBuilder.DropTable(
                name: "location_code");

            migrationBuilder.DropTable(
                name: "notice_of_cash_allocation");

            migrationBuilder.DropTable(
                name: "official_receipt");

            migrationBuilder.DropTable(
                name: "operational_fund");

            migrationBuilder.DropTable(
                name: "order_of_payment");

            migrationBuilder.DropTable(
                name: "ors_line_item");

            migrationBuilder.DropTable(
                name: "pap_code");

            migrationBuilder.DropTable(
                name: "payroll_loan_deduction");

            migrationBuilder.DropTable(
                name: "push_token");

            migrationBuilder.DropTable(
                name: "rcd_line");

            migrationBuilder.DropTable(
                name: "routing_slip_step");

            migrationBuilder.DropTable(
                name: "routing_template_step");

            migrationBuilder.DropTable(
                name: "salary_tranche_entry");

            migrationBuilder.DropTable(
                name: "state_history");

            migrationBuilder.DropTable(
                name: "withholding_tax_line");

            migrationBuilder.DropTable(
                name: "bank_collection_report");

            migrationBuilder.DropTable(
                name: "dv_transmittal");

            migrationBuilder.DropTable(
                name: "findes_export");

            migrationBuilder.DropTable(
                name: "journal_entry");

            migrationBuilder.DropTable(
                name: "jo_cos_payroll_entry");

            migrationBuilder.DropTable(
                name: "lddap_ada");

            migrationBuilder.DropTable(
                name: "liquidation_report");

            migrationBuilder.DropTable(
                name: "obligation_request");

            migrationBuilder.DropTable(
                name: "payroll_entry");

            migrationBuilder.DropTable(
                name: "report_of_collections");

            migrationBuilder.DropTable(
                name: "routing_slip");

            migrationBuilder.DropTable(
                name: "routing_template");

            migrationBuilder.DropTable(
                name: "salary_tranche");

            migrationBuilder.DropTable(
                name: "withholding_tax_statement");

            migrationBuilder.DropTable(
                name: "cash_advance");
        }
    }
}
