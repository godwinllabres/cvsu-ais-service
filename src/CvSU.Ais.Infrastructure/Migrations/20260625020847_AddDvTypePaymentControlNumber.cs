using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CvSU.Ais.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddDvTypePaymentControlNumber : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "control_number",
                table: "disbursement_voucher",
                type: "character varying(40)",
                maxLength: 40,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "dv_type",
                table: "disbursement_voucher",
                type: "character varying(16)",
                maxLength: 16,
                nullable: false,
                // A valid enum name so any pre-existing rows parse back cleanly.
                defaultValue: "Others");

            migrationBuilder.AddColumn<string>(
                name: "payment_method",
                table: "disbursement_voucher",
                type: "character varying(16)",
                maxLength: 16,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "payment_reference",
                table: "disbursement_voucher",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "supply_property_signed_off",
                table: "disbursement_voucher",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateIndex(
                name: "ix_disbursement_voucher_control_number",
                table: "disbursement_voucher",
                column: "control_number",
                unique: true,
                filter: "control_number IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_disbursement_voucher_payment_method_payment_reference",
                table: "disbursement_voucher",
                columns: new[] { "payment_method", "payment_reference" },
                unique: true,
                filter: "payment_reference IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_disbursement_voucher_control_number",
                table: "disbursement_voucher");

            migrationBuilder.DropIndex(
                name: "ix_disbursement_voucher_payment_method_payment_reference",
                table: "disbursement_voucher");

            migrationBuilder.DropColumn(
                name: "control_number",
                table: "disbursement_voucher");

            migrationBuilder.DropColumn(
                name: "dv_type",
                table: "disbursement_voucher");

            migrationBuilder.DropColumn(
                name: "payment_method",
                table: "disbursement_voucher");

            migrationBuilder.DropColumn(
                name: "payment_reference",
                table: "disbursement_voucher");

            migrationBuilder.DropColumn(
                name: "supply_property_signed_off",
                table: "disbursement_voucher");
        }
    }
}
