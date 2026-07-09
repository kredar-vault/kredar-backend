using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Kredar.API.Migrations
{
    /// <inheritdoc />
    public partial class AddBillingAndMissingFeatures : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "InviteToken",
                table: "TeamMembers",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "InviteTokenExpiry",
                table: "TeamMembers",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "TeamMembers",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "BusinessKybStatus",
                table: "OnboardingApplications",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "DevAccountName",
                table: "OnboardingApplications",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DevAccountNumber",
                table: "OnboardingApplications",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DevAddress",
                table: "OnboardingApplications",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DevBankCode",
                table: "OnboardingApplications",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DevBankName",
                table: "OnboardingApplications",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DevCountry",
                table: "OnboardingApplications",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DevDateOfBirth",
                table: "OnboardingApplications",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DevFullName",
                table: "OnboardingApplications",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DevGovIdNumber",
                table: "OnboardingApplications",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DevGovIdType",
                table: "OnboardingApplications",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeveloperKycStatus",
                table: "OnboardingApplications",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "PortfolioUrl",
                table: "OnboardingApplications",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ProjectDescription",
                table: "OnboardingApplications",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "SubMerchantId",
                table: "DedicatedAccounts",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "BillingSchedules",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    AccountRef = table.Column<string>(type: "text", nullable: false),
                    Reference = table.Column<string>(type: "text", nullable: true),
                    Description = table.Column<string>(type: "text", nullable: true),
                    Interval = table.Column<string>(type: "text", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    NextAmountKobo = table.Column<long>(type: "bigint", nullable: false),
                    DueOffsetDays = table.Column<int>(type: "integer", nullable: false),
                    PeriodsGenerated = table.Column<int>(type: "integer", nullable: false),
                    CarryCreditKobo = table.Column<long>(type: "bigint", nullable: false),
                    CurrentPeriodEndUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BillingSchedules", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "OnboardingDocuments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OnboardingApplicationId = table.Column<Guid>(type: "uuid", nullable: false),
                    DocumentType = table.Column<string>(type: "text", nullable: false),
                    FileName = table.Column<string>(type: "text", nullable: false),
                    FilePath = table.Column<string>(type: "text", nullable: false),
                    ContentType = table.Column<string>(type: "text", nullable: false),
                    UploadedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OnboardingDocuments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OnboardingDocuments_OnboardingApplications_OnboardingApplic~",
                        column: x => x.OnboardingApplicationId,
                        principalTable: "OnboardingApplications",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "BillingPeriods",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ScheduleId = table.Column<Guid>(type: "uuid", nullable: false),
                    Sequence = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    ExpectedAmountKobo = table.Column<long>(type: "bigint", nullable: false),
                    AmountAttributedKobo = table.Column<long>(type: "bigint", nullable: false),
                    PeriodStartUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    PeriodEndUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DueDateUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    PaidAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BillingPeriods", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BillingPeriods_BillingSchedules_ScheduleId",
                        column: x => x.ScheduleId,
                        principalTable: "BillingSchedules",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BillingPeriods_ScheduleId",
                table: "BillingPeriods",
                column: "ScheduleId");

            migrationBuilder.CreateIndex(
                name: "IX_BillingSchedules_TenantId_Reference",
                table: "BillingSchedules",
                columns: new[] { "TenantId", "Reference" },
                unique: true,
                filter: "\"Reference\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_OnboardingDocuments_OnboardingApplicationId",
                table: "OnboardingDocuments",
                column: "OnboardingApplicationId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BillingPeriods");

            migrationBuilder.DropTable(
                name: "OnboardingDocuments");

            migrationBuilder.DropTable(
                name: "BillingSchedules");

            migrationBuilder.DropColumn(
                name: "InviteToken",
                table: "TeamMembers");

            migrationBuilder.DropColumn(
                name: "InviteTokenExpiry",
                table: "TeamMembers");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "TeamMembers");

            migrationBuilder.DropColumn(
                name: "BusinessKybStatus",
                table: "OnboardingApplications");

            migrationBuilder.DropColumn(
                name: "DevAccountName",
                table: "OnboardingApplications");

            migrationBuilder.DropColumn(
                name: "DevAccountNumber",
                table: "OnboardingApplications");

            migrationBuilder.DropColumn(
                name: "DevAddress",
                table: "OnboardingApplications");

            migrationBuilder.DropColumn(
                name: "DevBankCode",
                table: "OnboardingApplications");

            migrationBuilder.DropColumn(
                name: "DevBankName",
                table: "OnboardingApplications");

            migrationBuilder.DropColumn(
                name: "DevCountry",
                table: "OnboardingApplications");

            migrationBuilder.DropColumn(
                name: "DevDateOfBirth",
                table: "OnboardingApplications");

            migrationBuilder.DropColumn(
                name: "DevFullName",
                table: "OnboardingApplications");

            migrationBuilder.DropColumn(
                name: "DevGovIdNumber",
                table: "OnboardingApplications");

            migrationBuilder.DropColumn(
                name: "DevGovIdType",
                table: "OnboardingApplications");

            migrationBuilder.DropColumn(
                name: "DeveloperKycStatus",
                table: "OnboardingApplications");

            migrationBuilder.DropColumn(
                name: "PortfolioUrl",
                table: "OnboardingApplications");

            migrationBuilder.DropColumn(
                name: "ProjectDescription",
                table: "OnboardingApplications");

            migrationBuilder.DropColumn(
                name: "SubMerchantId",
                table: "DedicatedAccounts");
        }
    }
}
