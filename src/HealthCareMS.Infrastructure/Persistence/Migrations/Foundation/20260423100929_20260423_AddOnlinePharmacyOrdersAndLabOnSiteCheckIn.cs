using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HealthCareMS.Infrastructure.Persistence.Migrations.Foundation
{
    /// <inheritdoc />
    public partial class _20260423_AddOnlinePharmacyOrdersAndLabOnSiteCheckIn : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "BarcodeLabelGeneratedAt",
                schema: "Lab",
                table: "SampleBookings",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "CheckedInAt",
                schema: "Lab",
                table: "SampleBookings",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "FastingVerified",
                schema: "Lab",
                table: "SampleBookings",
                type: "boolean",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TokenNumber",
                schema: "Lab",
                table: "SampleBookings",
                type: "character varying(35)",
                maxLength: 35,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "PharmacyOrders",
                schema: "Pharmacy",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true),
                    OrderNumber = table.Column<string>(type: "character varying(35)", maxLength: 35, nullable: false),
                    PatientId = table.Column<Guid>(type: "uuid", nullable: false),
                    PrescriptionId = table.Column<Guid>(type: "uuid", nullable: true),
                    Status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    OrderedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ReviewedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ConfirmedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DeliveryAgentUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    AssignedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DispatchedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DeliveredAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DeliveryAddress = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    DeliveryWindowStart = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DeliveryWindowEnd = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    PrescriptionUploadFileName = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    PrescriptionUploadContentType = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    PrescriptionUploadContent = table.Column<byte[]>(type: "bytea", nullable: true),
                    PatientNotes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    PharmacistNotes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    SubTotal = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    DeliveryFee = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
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
                    table.PrimaryKey("PK_PharmacyOrders", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PharmacyOrders_Patients_PatientId",
                        column: x => x.PatientId,
                        principalSchema: "Patient",
                        principalTable: "Patients",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PharmacyOrders_Prescriptions_PrescriptionId",
                        column: x => x.PrescriptionId,
                        principalSchema: "Consultation",
                        principalTable: "Prescriptions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PharmacyOrders_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalSchema: "Identity",
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PharmacyOrders_Users_DeliveryAgentUserId",
                        column: x => x.DeliveryAgentUserId,
                        principalSchema: "Identity",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PharmacyOrderItems",
                schema: "Pharmacy",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PharmacyOrderId = table.Column<Guid>(type: "uuid", nullable: false),
                    MedicineId = table.Column<Guid>(type: "uuid", nullable: false),
                    MedicineName = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    Quantity = table.Column<int>(type: "integer", nullable: false),
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
                    table.PrimaryKey("PK_PharmacyOrderItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PharmacyOrderItems_Medicines_MedicineId",
                        column: x => x.MedicineId,
                        principalSchema: "Pharmacy",
                        principalTable: "Medicines",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PharmacyOrderItems_PharmacyOrders_PharmacyOrderId",
                        column: x => x.PharmacyOrderId,
                        principalSchema: "Pharmacy",
                        principalTable: "PharmacyOrders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SampleBookings_TokenNumber",
                schema: "Lab",
                table: "SampleBookings",
                column: "TokenNumber");

            migrationBuilder.CreateIndex(
                name: "IX_PharmacyOrderItems_MedicineId",
                schema: "Pharmacy",
                table: "PharmacyOrderItems",
                column: "MedicineId");

            migrationBuilder.CreateIndex(
                name: "IX_PharmacyOrderItems_PharmacyOrderId_MedicineId",
                schema: "Pharmacy",
                table: "PharmacyOrderItems",
                columns: new[] { "PharmacyOrderId", "MedicineId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PharmacyOrders_DeliveryAgentUserId",
                schema: "Pharmacy",
                table: "PharmacyOrders",
                column: "DeliveryAgentUserId");

            migrationBuilder.CreateIndex(
                name: "IX_PharmacyOrders_OrderNumber",
                schema: "Pharmacy",
                table: "PharmacyOrders",
                column: "OrderNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PharmacyOrders_PatientId_OrderedAt",
                schema: "Pharmacy",
                table: "PharmacyOrders",
                columns: new[] { "PatientId", "OrderedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_PharmacyOrders_PrescriptionId",
                schema: "Pharmacy",
                table: "PharmacyOrders",
                column: "PrescriptionId");

            migrationBuilder.CreateIndex(
                name: "IX_PharmacyOrders_Status_OrderedAt",
                schema: "Pharmacy",
                table: "PharmacyOrders",
                columns: new[] { "Status", "OrderedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_PharmacyOrders_TenantId",
                schema: "Pharmacy",
                table: "PharmacyOrders",
                column: "TenantId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PharmacyOrderItems",
                schema: "Pharmacy");

            migrationBuilder.DropTable(
                name: "PharmacyOrders",
                schema: "Pharmacy");

            migrationBuilder.DropIndex(
                name: "IX_SampleBookings_TokenNumber",
                schema: "Lab",
                table: "SampleBookings");

            migrationBuilder.DropColumn(
                name: "BarcodeLabelGeneratedAt",
                schema: "Lab",
                table: "SampleBookings");

            migrationBuilder.DropColumn(
                name: "CheckedInAt",
                schema: "Lab",
                table: "SampleBookings");

            migrationBuilder.DropColumn(
                name: "FastingVerified",
                schema: "Lab",
                table: "SampleBookings");

            migrationBuilder.DropColumn(
                name: "TokenNumber",
                schema: "Lab",
                table: "SampleBookings");
        }
    }
}
