using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CvSU.Ais.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCertificationAuditTrail : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "accountant_signed_at",
                table: "disbursement_voucher",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "accountant_signed_by",
                table: "disbursement_voucher",
                type: "character varying(140)",
                maxLength: 140,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "budget_certified_at",
                table: "disbursement_voucher",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "budget_certified_by",
                table: "disbursement_voucher",
                type: "character varying(140)",
                maxLength: 140,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "end_user_confirmed_at",
                table: "disbursement_voucher",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "end_user_confirmed_by",
                table: "disbursement_voucher",
                type: "character varying(140)",
                maxLength: 140,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "internal_audit_confirmed_at",
                table: "disbursement_voucher",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "internal_audit_confirmed_by",
                table: "disbursement_voucher",
                type: "character varying(140)",
                maxLength: 140,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "supply_property_signed_off_at",
                table: "disbursement_voucher",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "supply_property_signed_off_by",
                table: "disbursement_voucher",
                type: "character varying(140)",
                maxLength: 140,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "accountant_signed_at",
                table: "disbursement_voucher");

            migrationBuilder.DropColumn(
                name: "accountant_signed_by",
                table: "disbursement_voucher");

            migrationBuilder.DropColumn(
                name: "budget_certified_at",
                table: "disbursement_voucher");

            migrationBuilder.DropColumn(
                name: "budget_certified_by",
                table: "disbursement_voucher");

            migrationBuilder.DropColumn(
                name: "end_user_confirmed_at",
                table: "disbursement_voucher");

            migrationBuilder.DropColumn(
                name: "end_user_confirmed_by",
                table: "disbursement_voucher");

            migrationBuilder.DropColumn(
                name: "internal_audit_confirmed_at",
                table: "disbursement_voucher");

            migrationBuilder.DropColumn(
                name: "internal_audit_confirmed_by",
                table: "disbursement_voucher");

            migrationBuilder.DropColumn(
                name: "supply_property_signed_off_at",
                table: "disbursement_voucher");

            migrationBuilder.DropColumn(
                name: "supply_property_signed_off_by",
                table: "disbursement_voucher");
        }
    }
}
