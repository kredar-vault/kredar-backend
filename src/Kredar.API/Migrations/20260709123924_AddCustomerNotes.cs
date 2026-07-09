using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Kredar.API.Migrations
{
    /// <inheritdoc />
    public partial class AddCustomerNotes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CustomerNotes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    CustomerId = table.Column<Guid>(type: "uuid", nullable: false),
                    Content = table.Column<string>(type: "text", nullable: false),
                    CreatedByEmail = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CustomerNotes", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CustomerNotes_TenantId_CustomerId",
                table: "CustomerNotes",
                columns: new[] { "TenantId", "CustomerId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CustomerNotes");
        }
    }
}
