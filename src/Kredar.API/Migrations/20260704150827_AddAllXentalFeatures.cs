using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Kredar.API.Migrations
{
    /// <inheritdoc />
    public partial class AddAllXentalFeatures : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PasswordResetToken",
                table: "Tenants",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "PasswordResetTokenExpiry",
                table: "Tenants",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "AdminUsers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Email = table.Column<string>(type: "text", nullable: false),
                    PasswordHash = table.Column<string>(type: "text", nullable: false),
                    Role = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AdminUsers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ApiKeys",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    Label = table.Column<string>(type: "text", nullable: false),
                    ClientId = table.Column<string>(type: "text", nullable: false),
                    SecretHash = table.Column<string>(type: "text", nullable: false),
                    Mode = table.Column<string>(type: "text", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    LastUsedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ApiKeys", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CheckoutSessions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    DedicatedAccountId = table.Column<Guid>(type: "uuid", nullable: false),
                    Token = table.Column<string>(type: "text", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CheckoutSessions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "EscrowHolds",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    DedicatedAccountId = table.Column<Guid>(type: "uuid", nullable: false),
                    AmountNaira = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    State = table.Column<string>(type: "text", nullable: false),
                    ReleaseCondition = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ReleasedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EscrowHolds", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MoneyRules",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    Trigger = table.Column<string>(type: "text", nullable: false),
                    Action = table.Column<string>(type: "text", nullable: false),
                    ThresholdNaira = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    Priority = table.Column<int>(type: "integer", nullable: false),
                    Enabled = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MoneyRules", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "OnboardingApplications",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    Tier = table.Column<string>(type: "text", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    LegalName = table.Column<string>(type: "text", nullable: true),
                    RegistrationNumber = table.Column<string>(type: "text", nullable: true),
                    BusinessType = table.Column<string>(type: "text", nullable: true),
                    Industry = table.Column<string>(type: "text", nullable: true),
                    Country = table.Column<string>(type: "text", nullable: true),
                    Address = table.Column<string>(type: "text", nullable: true),
                    ContactPhone = table.Column<string>(type: "text", nullable: true),
                    Website = table.Column<string>(type: "text", nullable: true),
                    SettlementBankName = table.Column<string>(type: "text", nullable: true),
                    SettlementBankCode = table.Column<string>(type: "text", nullable: true),
                    SettlementAccountName = table.Column<string>(type: "text", nullable: true),
                    SettlementAccountNumber = table.Column<string>(type: "text", nullable: true),
                    DecisionReason = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    SubmittedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DecidedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OnboardingApplications", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OnboardingApplications_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SettlementConfigs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    SettlementAccountNumber = table.Column<string>(type: "text", nullable: true),
                    SettlementBankCode = table.Column<string>(type: "text", nullable: true),
                    SettlementAccountName = table.Column<string>(type: "text", nullable: true),
                    AutoSettle = table.Column<bool>(type: "boolean", nullable: false),
                    MinPayoutNaira = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SettlementConfigs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SettlementSplits",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    BeneficiaryName = table.Column<string>(type: "text", nullable: false),
                    AccountNumber = table.Column<string>(type: "text", nullable: false),
                    BankCode = table.Column<string>(type: "text", nullable: false),
                    Basis = table.Column<string>(type: "text", nullable: false),
                    ShareBps = table.Column<int>(type: "integer", nullable: false),
                    FlatNaira = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    Priority = table.Column<int>(type: "integer", nullable: false),
                    Enabled = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SettlementSplits", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SubMerchants",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Reference = table.Column<string>(type: "text", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    SettlementBankName = table.Column<string>(type: "text", nullable: true),
                    SettlementBankCode = table.Column<string>(type: "text", nullable: true),
                    SettlementAccountNumber = table.Column<string>(type: "text", nullable: true),
                    SettlementAccountName = table.Column<string>(type: "text", nullable: true),
                    PlatformFeeBps = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SubMerchants", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AdminUsers_Email",
                table: "AdminUsers",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ApiKeys_ClientId",
                table: "ApiKeys",
                column: "ClientId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CheckoutSessions_Token",
                table: "CheckoutSessions",
                column: "Token",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OnboardingApplications_TenantId",
                table: "OnboardingApplications",
                column: "TenantId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SettlementConfigs_TenantId",
                table: "SettlementConfigs",
                column: "TenantId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SubMerchants_TenantId_Reference",
                table: "SubMerchants",
                columns: new[] { "TenantId", "Reference" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AdminUsers");

            migrationBuilder.DropTable(
                name: "ApiKeys");

            migrationBuilder.DropTable(
                name: "CheckoutSessions");

            migrationBuilder.DropTable(
                name: "EscrowHolds");

            migrationBuilder.DropTable(
                name: "MoneyRules");

            migrationBuilder.DropTable(
                name: "OnboardingApplications");

            migrationBuilder.DropTable(
                name: "SettlementConfigs");

            migrationBuilder.DropTable(
                name: "SettlementSplits");

            migrationBuilder.DropTable(
                name: "SubMerchants");

            migrationBuilder.DropColumn(
                name: "PasswordResetToken",
                table: "Tenants");

            migrationBuilder.DropColumn(
                name: "PasswordResetTokenExpiry",
                table: "Tenants");
        }
    }
}
