using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HealthCareMS.Infrastructure.Persistence.Migrations.Foundation
{
    /// <inheritdoc />
    public partial class _20260423_AddLabOrderingAndPharmacyCatalogueFoundation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "Lab");

            migrationBuilder.EnsureSchema(
                name: "Pharmacy");

            migrationBuilder.CreateTable(
                name: "LabTests",
                schema: "Lab",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true),
                    TestCode = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    TestName = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: false),
                    Category = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    SampleType = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    TurnaroundHours = table.Column<short>(type: "smallint", nullable: false),
                    FastingHours = table.Column<short>(type: "smallint", nullable: true),
                    PreparationInstructions = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    Price = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    IsHomeCollectionAvailable = table.Column<bool>(type: "boolean", nullable: false),
                    HomeCollectionExtra = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
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
                    table.PrimaryKey("PK_LabTests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LabTests_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalSchema: "Identity",
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Medicines",
                schema: "Pharmacy",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true),
                    GenericName = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    BrandName = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    DosageForm = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    Strength = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    DrapRegistrationNumber = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Manufacturer = table.Column<string>(type: "character varying(180)", maxLength: 180, nullable: true),
                    UnitPrice = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    UnitCostPrice = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    IsControlled = table.Column<bool>(type: "boolean", nullable: false),
                    ReorderLevel = table.Column<int>(type: "integer", nullable: false),
                    Barcode = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
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
                    table.PrimaryKey("PK_Medicines", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Medicines_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalSchema: "Identity",
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SampleBookings",
                schema: "Lab",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BookingNumber = table.Column<string>(type: "character varying(35)", maxLength: 35, nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true),
                    PatientId = table.Column<Guid>(type: "uuid", nullable: false),
                    AppointmentId = table.Column<Guid>(type: "uuid", nullable: true),
                    PrescriptionId = table.Column<Guid>(type: "uuid", nullable: true),
                    CollectionType = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    CollectionScheduledAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CollectionAddress = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    SampleBarcode = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    Notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    SubTotal = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    HomeCollectionFee = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    TotalAmount = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SampleBookings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SampleBookings_Appointments_AppointmentId",
                        column: x => x.AppointmentId,
                        principalSchema: "Appointment",
                        principalTable: "Appointments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SampleBookings_Patients_PatientId",
                        column: x => x.PatientId,
                        principalSchema: "Patient",
                        principalTable: "Patients",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SampleBookings_Prescriptions_PrescriptionId",
                        column: x => x.PrescriptionId,
                        principalSchema: "Consultation",
                        principalTable: "Prescriptions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SampleBookings_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalSchema: "Identity",
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Suppliers",
                schema: "Pharmacy",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    ContactPerson = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: true),
                    Phone = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    Email = table.Column<string>(type: "character varying(254)", maxLength: 254, nullable: true),
                    Address = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
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
                    table.PrimaryKey("PK_Suppliers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Suppliers_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalSchema: "Identity",
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "BookingItems",
                schema: "Lab",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BookingId = table.Column<Guid>(type: "uuid", nullable: false),
                    LabTestId = table.Column<Guid>(type: "uuid", nullable: false),
                    Price = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BookingItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BookingItems_LabTests_LabTestId",
                        column: x => x.LabTestId,
                        principalSchema: "Lab",
                        principalTable: "LabTests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_BookingItems_SampleBookings_BookingId",
                        column: x => x.BookingId,
                        principalSchema: "Lab",
                        principalTable: "SampleBookings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "StockBatches",
                schema: "Pharmacy",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true),
                    MedicineId = table.Column<Guid>(type: "uuid", nullable: false),
                    SupplierId = table.Column<Guid>(type: "uuid", nullable: true),
                    BatchNumber = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    ManufacturedDate = table.Column<DateOnly>(type: "date", nullable: true),
                    ExpiryDate = table.Column<DateOnly>(type: "date", nullable: false),
                    QuantityOnHand = table.Column<int>(type: "integer", nullable: false),
                    UnitCostPrice = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    ReceivedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StockBatches", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StockBatches_Medicines_MedicineId",
                        column: x => x.MedicineId,
                        principalSchema: "Pharmacy",
                        principalTable: "Medicines",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_StockBatches_Suppliers_SupplierId",
                        column: x => x.SupplierId,
                        principalSchema: "Pharmacy",
                        principalTable: "Suppliers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StockBatches_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalSchema: "Identity",
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BookingItems_BookingId_LabTestId",
                schema: "Lab",
                table: "BookingItems",
                columns: new[] { "BookingId", "LabTestId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BookingItems_LabTestId",
                schema: "Lab",
                table: "BookingItems",
                column: "LabTestId");

            migrationBuilder.CreateIndex(
                name: "IX_LabTests_TenantId",
                schema: "Lab",
                table: "LabTests",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_LabTests_TestCode",
                schema: "Lab",
                table: "LabTests",
                column: "TestCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LabTests_TestName_Category",
                schema: "Lab",
                table: "LabTests",
                columns: new[] { "TestName", "Category" });

            migrationBuilder.CreateIndex(
                name: "IX_Medicines_Barcode",
                schema: "Pharmacy",
                table: "Medicines",
                column: "Barcode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Medicines_BrandName_GenericName",
                schema: "Pharmacy",
                table: "Medicines",
                columns: new[] { "BrandName", "GenericName" });

            migrationBuilder.CreateIndex(
                name: "IX_Medicines_DrapRegistrationNumber",
                schema: "Pharmacy",
                table: "Medicines",
                column: "DrapRegistrationNumber");

            migrationBuilder.CreateIndex(
                name: "IX_Medicines_TenantId",
                schema: "Pharmacy",
                table: "Medicines",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_SampleBookings_AppointmentId",
                schema: "Lab",
                table: "SampleBookings",
                column: "AppointmentId");

            migrationBuilder.CreateIndex(
                name: "IX_SampleBookings_BookingNumber",
                schema: "Lab",
                table: "SampleBookings",
                column: "BookingNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SampleBookings_PatientId",
                schema: "Lab",
                table: "SampleBookings",
                column: "PatientId");

            migrationBuilder.CreateIndex(
                name: "IX_SampleBookings_PrescriptionId",
                schema: "Lab",
                table: "SampleBookings",
                column: "PrescriptionId");

            migrationBuilder.CreateIndex(
                name: "IX_SampleBookings_SampleBarcode",
                schema: "Lab",
                table: "SampleBookings",
                column: "SampleBarcode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SampleBookings_TenantId",
                schema: "Lab",
                table: "SampleBookings",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_StockBatches_ExpiryDate",
                schema: "Pharmacy",
                table: "StockBatches",
                column: "ExpiryDate");

            migrationBuilder.CreateIndex(
                name: "IX_StockBatches_MedicineId_BatchNumber",
                schema: "Pharmacy",
                table: "StockBatches",
                columns: new[] { "MedicineId", "BatchNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StockBatches_SupplierId",
                schema: "Pharmacy",
                table: "StockBatches",
                column: "SupplierId");

            migrationBuilder.CreateIndex(
                name: "IX_StockBatches_TenantId",
                schema: "Pharmacy",
                table: "StockBatches",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_Suppliers_Name",
                schema: "Pharmacy",
                table: "Suppliers",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_Suppliers_TenantId",
                schema: "Pharmacy",
                table: "Suppliers",
                column: "TenantId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BookingItems",
                schema: "Lab");

            migrationBuilder.DropTable(
                name: "StockBatches",
                schema: "Pharmacy");

            migrationBuilder.DropTable(
                name: "LabTests",
                schema: "Lab");

            migrationBuilder.DropTable(
                name: "SampleBookings",
                schema: "Lab");

            migrationBuilder.DropTable(
                name: "Medicines",
                schema: "Pharmacy");

            migrationBuilder.DropTable(
                name: "Suppliers",
                schema: "Pharmacy");
        }
    }
}
