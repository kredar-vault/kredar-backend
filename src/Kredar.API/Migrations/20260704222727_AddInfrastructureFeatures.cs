using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Kredar.API.Migrations
{
    /// <inheritdoc />
    public partial class AddInfrastructureFeatures : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsSuspended",
                table: "Tenants",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "MfaEnabled",
                table: "AdminUsers",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "TotpSecret",
                table: "AdminUsers",
                type: "text",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "AdminAuditLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AdminId = table.Column<Guid>(type: "uuid", nullable: false),
                    AdminEmail = table.Column<string>(type: "text", nullable: false),
                    Action = table.Column<string>(type: "text", nullable: false),
                    TargetTenantId = table.Column<Guid>(type: "uuid", nullable: true),
                    Detail = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AdminAuditLogs", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AdminAuditLogs_CreatedAt",
                table: "AdminAuditLogs",
                column: "CreatedAt");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AdminAuditLogs");

            migrationBuilder.DropColumn(
                name: "IsSuspended",
                table: "Tenants");

            migrationBuilder.DropColumn(
                name: "MfaEnabled",
                table: "AdminUsers");

            migrationBuilder.DropColumn(
                name: "TotpSecret",
                table: "AdminUsers");
        }
    }
}
