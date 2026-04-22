using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HealthCareMS.Infrastructure.Persistence.Migrations.Foundation
{
    /// <inheritdoc />
    public partial class _20260422_AddAdminOperationsFoundation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SystemSettings",
                schema: "Identity",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SettingKey = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    GroupName = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    DisplayName = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    Value = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    ValueType = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false, defaultValue: "String"),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    IsSensitive = table.Column<bool>(type: "boolean", nullable: false),
                    IsEditable = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SystemSettings", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Doctors_City_IsActive",
                schema: "Doctor",
                table: "Doctors",
                columns: new[] { "City", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_Doctors_IsVerified_IsActive",
                schema: "Doctor",
                table: "Doctors",
                columns: new[] { "IsVerified", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_Appointments_ScheduledAt_Status",
                schema: "Appointment",
                table: "Appointments",
                columns: new[] { "ScheduledAt", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_Appointments_Status_ScheduledAt",
                schema: "Appointment",
                table: "Appointments",
                columns: new[] { "Status", "ScheduledAt" });

            migrationBuilder.CreateIndex(
                name: "IX_SystemSettings_GroupName_DisplayName",
                schema: "Identity",
                table: "SystemSettings",
                columns: new[] { "GroupName", "DisplayName" });

            migrationBuilder.CreateIndex(
                name: "IX_SystemSettings_SettingKey",
                schema: "Identity",
                table: "SystemSettings",
                column: "SettingKey",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SystemSettings",
                schema: "Identity");

            migrationBuilder.DropIndex(
                name: "IX_Doctors_City_IsActive",
                schema: "Doctor",
                table: "Doctors");

            migrationBuilder.DropIndex(
                name: "IX_Doctors_IsVerified_IsActive",
                schema: "Doctor",
                table: "Doctors");

            migrationBuilder.DropIndex(
                name: "IX_Appointments_ScheduledAt_Status",
                schema: "Appointment",
                table: "Appointments");

            migrationBuilder.DropIndex(
                name: "IX_Appointments_Status_ScheduledAt",
                schema: "Appointment",
                table: "Appointments");
        }
    }
}
