using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HealthCareMS.Infrastructure.Persistence.Migrations.Foundation
{
    /// <inheritdoc />
    public partial class _20260422_AddEPrescriptionFoundation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DigitalSignature",
                schema: "Consultation",
                table: "Prescriptions",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "VerificationCode",
                schema: "Consultation",
                table: "Prescriptions",
                type: "character varying(80)",
                maxLength: 80,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "DrapMedicines",
                schema: "Consultation",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DrapRegistrationNumber = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    BrandName = table.Column<string>(type: "character varying(180)", maxLength: 180, nullable: false),
                    GenericName = table.Column<string>(type: "character varying(180)", maxLength: 180, nullable: false),
                    Strength = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    DosageForm = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    Manufacturer = table.Column<string>(type: "character varying(180)", maxLength: 180, nullable: true),
                    AllergenKeywords = table.Column<string>(type: "character varying(600)", maxLength: 600, nullable: false),
                    IsBanned = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DrapMedicines", x => x.Id);
                });

            migrationBuilder.Sql("""
                UPDATE "Consultation"."Prescriptions"
                SET "VerificationCode" = upper(substring(md5("Id"::text || '|' || "PrescriptionNumber") from 1 for 24)),
                    "DigitalSignature" = upper(md5("PrescriptionNumber" || '|' || "DoctorId"::text || '|' || "PatientId"::text || '|' || "IssuedAt"::text) || md5("Id"::text || '|' || "PrescriptionNumber"))
                WHERE "VerificationCode" = '' OR "DigitalSignature" = '';
                """);

            migrationBuilder.CreateIndex(
                name: "IX_Prescriptions_VerificationCode",
                schema: "Consultation",
                table: "Prescriptions",
                column: "VerificationCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DrapMedicines_BrandName_GenericName",
                schema: "Consultation",
                table: "DrapMedicines",
                columns: new[] { "BrandName", "GenericName" });

            migrationBuilder.CreateIndex(
                name: "IX_DrapMedicines_DrapRegistrationNumber",
                schema: "Consultation",
                table: "DrapMedicines",
                column: "DrapRegistrationNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DrapMedicines_IsBanned",
                schema: "Consultation",
                table: "DrapMedicines",
                column: "IsBanned");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DrapMedicines",
                schema: "Consultation");

            migrationBuilder.DropIndex(
                name: "IX_Prescriptions_VerificationCode",
                schema: "Consultation",
                table: "Prescriptions");

            migrationBuilder.DropColumn(
                name: "DigitalSignature",
                schema: "Consultation",
                table: "Prescriptions");

            migrationBuilder.DropColumn(
                name: "VerificationCode",
                schema: "Consultation",
                table: "Prescriptions");
        }
    }
}
