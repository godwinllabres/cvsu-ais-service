using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CvSU.Ais.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddOfficialReceiptFeeTypeCreditAccount : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "cost_center",
                table: "official_receipt",
                type: "character varying(40)",
                maxLength: 40,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "credit_account",
                table: "official_receipt",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "fee_type",
                table: "official_receipt",
                type: "character varying(16)",
                maxLength: 16,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "cost_center",
                table: "official_receipt");

            migrationBuilder.DropColumn(
                name: "credit_account",
                table: "official_receipt");

            migrationBuilder.DropColumn(
                name: "fee_type",
                table: "official_receipt");
        }
    }
}
