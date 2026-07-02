using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Kredar.API.Migrations
{
    /// <inheritdoc />
    public partial class AddTransactionDetails : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Description",
                table: "Transactions",
                newName: "PaymentReference");

            migrationBuilder.AddColumn<decimal>(
                name: "AmountReceived",
                table: "Transactions",
                type: "numeric",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "ExpectedAmount",
                table: "Transactions",
                type: "numeric",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "Fee",
                table: "Transactions",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "Narration",
                table: "Transactions",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PaymentMethod",
                table: "Transactions",
                type: "text",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AmountReceived",
                table: "Transactions");

            migrationBuilder.DropColumn(
                name: "ExpectedAmount",
                table: "Transactions");

            migrationBuilder.DropColumn(
                name: "Fee",
                table: "Transactions");

            migrationBuilder.DropColumn(
                name: "Narration",
                table: "Transactions");

            migrationBuilder.DropColumn(
                name: "PaymentMethod",
                table: "Transactions");

            migrationBuilder.RenameColumn(
                name: "PaymentReference",
                table: "Transactions",
                newName: "Description");
        }
    }
}
