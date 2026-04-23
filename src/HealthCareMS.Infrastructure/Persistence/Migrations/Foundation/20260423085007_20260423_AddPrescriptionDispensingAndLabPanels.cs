using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HealthCareMS.Infrastructure.Persistence.Migrations.Foundation
{
    /// <inheritdoc />
    public partial class _20260423_AddPrescriptionDispensingAndLabPanels : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "LabPanels",
                schema: "Lab",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true),
                    PanelCode = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    PanelName = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: false),
                    Category = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    Price = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LabPanels", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LabPanels_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalSchema: "Identity",
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PrescriptionDispenses",
                schema: "Pharmacy",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true),
                    DispenseNumber = table.Column<string>(type: "character varying(35)", maxLength: 35, nullable: false),
                    ReceiptNumber = table.Column<string>(type: "character varying(35)", maxLength: 35, nullable: false),
                    PrescriptionId = table.Column<Guid>(type: "uuid", nullable: false),
                    PatientId = table.Column<Guid>(type: "uuid", nullable: false),
                    DoctorId = table.Column<Guid>(type: "uuid", nullable: false),
                    VerificationCode = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    DispensedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    SubTotal = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    TotalAmount = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    Notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PrescriptionDispenses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PrescriptionDispenses_Doctors_DoctorId",
                        column: x => x.DoctorId,
                        principalSchema: "Doctor",
                        principalTable: "Doctors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PrescriptionDispenses_Patients_PatientId",
                        column: x => x.PatientId,
                        principalSchema: "Patient",
                        principalTable: "Patients",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PrescriptionDispenses_Prescriptions_PrescriptionId",
                        column: x => x.PrescriptionId,
                        principalSchema: "Consultation",
                        principalTable: "Prescriptions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PrescriptionDispenses_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalSchema: "Identity",
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "LabPanelItems",
                schema: "Lab",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    LabPanelId = table.Column<Guid>(type: "uuid", nullable: false),
                    LabTestId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LabPanelItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LabPanelItems_LabPanels_LabPanelId",
                        column: x => x.LabPanelId,
                        principalSchema: "Lab",
                        principalTable: "LabPanels",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_LabPanelItems_LabTests_LabTestId",
                        column: x => x.LabTestId,
                        principalSchema: "Lab",
                        principalTable: "LabTests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PrescriptionDispenseItems",
                schema: "Pharmacy",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PrescriptionDispenseId = table.Column<Guid>(type: "uuid", nullable: false),
                    PrescriptionItemId = table.Column<Guid>(type: "uuid", nullable: false),
                    MedicineId = table.Column<Guid>(type: "uuid", nullable: false),
                    PrescribedMedicineName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    DispensedMedicineName = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    QuantityPrescribed = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    QuantityDispensed = table.Column<int>(type: "integer", nullable: false),
                    UnitPrice = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    LineTotal = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PrescriptionDispenseItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PrescriptionDispenseItems_Medicines_MedicineId",
                        column: x => x.MedicineId,
                        principalSchema: "Pharmacy",
                        principalTable: "Medicines",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PrescriptionDispenseItems_PrescriptionDispenses_Prescriptio~",
                        column: x => x.PrescriptionDispenseId,
                        principalSchema: "Pharmacy",
                        principalTable: "PrescriptionDispenses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PrescriptionDispenseItems_PrescriptionItems_PrescriptionIte~",
                        column: x => x.PrescriptionItemId,
                        principalSchema: "Consultation",
                        principalTable: "PrescriptionItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PrescriptionDispenseBatches",
                schema: "Pharmacy",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PrescriptionDispenseItemId = table.Column<Guid>(type: "uuid", nullable: false),
                    StockBatchId = table.Column<Guid>(type: "uuid", nullable: false),
                    BatchNumber = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    QuantityDispensed = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PrescriptionDispenseBatches", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PrescriptionDispenseBatches_PrescriptionDispenseItems_Presc~",
                        column: x => x.PrescriptionDispenseItemId,
                        principalSchema: "Pharmacy",
                        principalTable: "PrescriptionDispenseItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PrescriptionDispenseBatches_StockBatches_StockBatchId",
                        column: x => x.StockBatchId,
                        principalSchema: "Pharmacy",
                        principalTable: "StockBatches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_LabPanelItems_LabPanelId_LabTestId",
                schema: "Lab",
                table: "LabPanelItems",
                columns: new[] { "LabPanelId", "LabTestId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LabPanelItems_LabTestId",
                schema: "Lab",
                table: "LabPanelItems",
                column: "LabTestId");

            migrationBuilder.CreateIndex(
                name: "IX_LabPanels_PanelCode",
                schema: "Lab",
                table: "LabPanels",
                column: "PanelCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LabPanels_PanelName_Category",
                schema: "Lab",
                table: "LabPanels",
                columns: new[] { "PanelName", "Category" });

            migrationBuilder.CreateIndex(
                name: "IX_LabPanels_TenantId",
                schema: "Lab",
                table: "LabPanels",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_PrescriptionDispenseBatches_PrescriptionDispenseItemId_Stoc~",
                schema: "Pharmacy",
                table: "PrescriptionDispenseBatches",
                columns: new[] { "PrescriptionDispenseItemId", "StockBatchId" });

            migrationBuilder.CreateIndex(
                name: "IX_PrescriptionDispenseBatches_StockBatchId",
                schema: "Pharmacy",
                table: "PrescriptionDispenseBatches",
                column: "StockBatchId");

            migrationBuilder.CreateIndex(
                name: "IX_PrescriptionDispenseItems_MedicineId",
                schema: "Pharmacy",
                table: "PrescriptionDispenseItems",
                column: "MedicineId");

            migrationBuilder.CreateIndex(
                name: "IX_PrescriptionDispenseItems_PrescriptionDispenseId_Prescripti~",
                schema: "Pharmacy",
                table: "PrescriptionDispenseItems",
                columns: new[] { "PrescriptionDispenseId", "PrescriptionItemId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PrescriptionDispenseItems_PrescriptionItemId",
                schema: "Pharmacy",
                table: "PrescriptionDispenseItems",
                column: "PrescriptionItemId");

            migrationBuilder.CreateIndex(
                name: "IX_PrescriptionDispenses_DispenseNumber",
                schema: "Pharmacy",
                table: "PrescriptionDispenses",
                column: "DispenseNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PrescriptionDispenses_DoctorId",
                schema: "Pharmacy",
                table: "PrescriptionDispenses",
                column: "DoctorId");

            migrationBuilder.CreateIndex(
                name: "IX_PrescriptionDispenses_PatientId_DispensedAt",
                schema: "Pharmacy",
                table: "PrescriptionDispenses",
                columns: new[] { "PatientId", "DispensedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_PrescriptionDispenses_PrescriptionId_Status",
                schema: "Pharmacy",
                table: "PrescriptionDispenses",
                columns: new[] { "PrescriptionId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_PrescriptionDispenses_ReceiptNumber",
                schema: "Pharmacy",
                table: "PrescriptionDispenses",
                column: "ReceiptNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PrescriptionDispenses_TenantId",
                schema: "Pharmacy",
                table: "PrescriptionDispenses",
                column: "TenantId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LabPanelItems",
                schema: "Lab");

            migrationBuilder.DropTable(
                name: "PrescriptionDispenseBatches",
                schema: "Pharmacy");

            migrationBuilder.DropTable(
                name: "LabPanels",
                schema: "Lab");

            migrationBuilder.DropTable(
                name: "PrescriptionDispenseItems",
                schema: "Pharmacy");

            migrationBuilder.DropTable(
                name: "PrescriptionDispenses",
                schema: "Pharmacy");
        }
    }
}
