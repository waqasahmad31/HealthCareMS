using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HealthCareMS.Infrastructure.Persistence.Migrations.Foundation
{
    /// <inheritdoc />
    public partial class _20260423_AddConsultationReviewsAndPharmacyStockManagement : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "AverageRating",
                schema: "Doctor",
                table: "Doctors",
                type: "numeric(3,2)",
                precision: 3,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "RatingCount",
                schema: "Doctor",
                table: "Doctors",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "DoctorReviews",
                schema: "Doctor",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AppointmentId = table.Column<Guid>(type: "uuid", nullable: false),
                    PatientId = table.Column<Guid>(type: "uuid", nullable: false),
                    DoctorId = table.Column<Guid>(type: "uuid", nullable: false),
                    Rating = table.Column<byte>(type: "smallint", nullable: false),
                    ReviewText = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    IsRecommended = table.Column<bool>(type: "boolean", nullable: false),
                    ReviewedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DoctorReviews", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DoctorReviews_Appointments_AppointmentId",
                        column: x => x.AppointmentId,
                        principalSchema: "Appointment",
                        principalTable: "Appointments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DoctorReviews_Doctors_DoctorId",
                        column: x => x.DoctorId,
                        principalSchema: "Doctor",
                        principalTable: "Doctors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DoctorReviews_Patients_PatientId",
                        column: x => x.PatientId,
                        principalSchema: "Patient",
                        principalTable: "Patients",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "StockAdjustments",
                schema: "Pharmacy",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true),
                    MedicineId = table.Column<Guid>(type: "uuid", nullable: false),
                    StockBatchId = table.Column<Guid>(type: "uuid", nullable: false),
                    AdjustmentType = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    QuantityDelta = table.Column<int>(type: "integer", nullable: false),
                    PreviousQuantity = table.Column<int>(type: "integer", nullable: false),
                    NewQuantity = table.Column<int>(type: "integer", nullable: false),
                    Reason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    AdjustedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StockAdjustments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StockAdjustments_Medicines_MedicineId",
                        column: x => x.MedicineId,
                        principalSchema: "Pharmacy",
                        principalTable: "Medicines",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StockAdjustments_StockBatches_StockBatchId",
                        column: x => x.StockBatchId,
                        principalSchema: "Pharmacy",
                        principalTable: "StockBatches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StockAdjustments_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalSchema: "Identity",
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "StockAlerts",
                schema: "Pharmacy",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true),
                    MedicineId = table.Column<Guid>(type: "uuid", nullable: false),
                    StockBatchId = table.Column<Guid>(type: "uuid", nullable: true),
                    AlertType = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Severity = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    Message = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    ThresholdQuantity = table.Column<int>(type: "integer", nullable: true),
                    QuantityOnHand = table.Column<int>(type: "integer", nullable: true),
                    ExpiryDate = table.Column<DateOnly>(type: "date", nullable: true),
                    DetectedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ResolvedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StockAlerts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StockAlerts_Medicines_MedicineId",
                        column: x => x.MedicineId,
                        principalSchema: "Pharmacy",
                        principalTable: "Medicines",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StockAlerts_StockBatches_StockBatchId",
                        column: x => x.StockBatchId,
                        principalSchema: "Pharmacy",
                        principalTable: "StockBatches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StockAlerts_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalSchema: "Identity",
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DoctorReviews_AppointmentId",
                schema: "Doctor",
                table: "DoctorReviews",
                column: "AppointmentId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DoctorReviews_DoctorId_ReviewedAt",
                schema: "Doctor",
                table: "DoctorReviews",
                columns: new[] { "DoctorId", "ReviewedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_DoctorReviews_PatientId",
                schema: "Doctor",
                table: "DoctorReviews",
                column: "PatientId");

            migrationBuilder.CreateIndex(
                name: "IX_StockAdjustments_MedicineId_AdjustedAt",
                schema: "Pharmacy",
                table: "StockAdjustments",
                columns: new[] { "MedicineId", "AdjustedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_StockAdjustments_StockBatchId_AdjustedAt",
                schema: "Pharmacy",
                table: "StockAdjustments",
                columns: new[] { "StockBatchId", "AdjustedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_StockAdjustments_TenantId",
                schema: "Pharmacy",
                table: "StockAdjustments",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_StockAlerts_MedicineId_AlertType_Status",
                schema: "Pharmacy",
                table: "StockAlerts",
                columns: new[] { "MedicineId", "AlertType", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_StockAlerts_StockBatchId_AlertType_Status",
                schema: "Pharmacy",
                table: "StockAlerts",
                columns: new[] { "StockBatchId", "AlertType", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_StockAlerts_TenantId",
                schema: "Pharmacy",
                table: "StockAlerts",
                column: "TenantId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DoctorReviews",
                schema: "Doctor");

            migrationBuilder.DropTable(
                name: "StockAdjustments",
                schema: "Pharmacy");

            migrationBuilder.DropTable(
                name: "StockAlerts",
                schema: "Pharmacy");

            migrationBuilder.DropColumn(
                name: "AverageRating",
                schema: "Doctor",
                table: "Doctors");

            migrationBuilder.DropColumn(
                name: "RatingCount",
                schema: "Doctor",
                table: "Doctors");
        }
    }
}
