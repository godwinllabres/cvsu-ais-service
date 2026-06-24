using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CvSU.Ais.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddDvBudgetLine : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "expense_class",
                table: "disbursement_voucher",
                type: "character varying(8)",
                maxLength: 8,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "location_code",
                table: "disbursement_voucher",
                type: "character varying(40)",
                maxLength: 40,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "object_account_code",
                table: "disbursement_voucher",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "pap_code",
                table: "disbursement_voucher",
                type: "character varying(40)",
                maxLength: 40,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "expense_class",
                table: "disbursement_voucher");

            migrationBuilder.DropColumn(
                name: "location_code",
                table: "disbursement_voucher");

            migrationBuilder.DropColumn(
                name: "object_account_code",
                table: "disbursement_voucher");

            migrationBuilder.DropColumn(
                name: "pap_code",
                table: "disbursement_voucher");
        }
    }
}
