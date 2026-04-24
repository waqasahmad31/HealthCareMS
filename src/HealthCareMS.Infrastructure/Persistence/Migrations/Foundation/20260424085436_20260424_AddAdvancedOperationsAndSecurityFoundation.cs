using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HealthCareMS.Infrastructure.Persistence.Migrations.Foundation
{
    /// <inheritdoc />
    public partial class _20260424_AddAdvancedOperationsAndSecurityFoundation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "Payment");

            migrationBuilder.AddColumn<Guid>(
                name: "CollectionAgentUserId",
                schema: "Lab",
                table: "SampleBookings",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "CollectionAssignedAt",
                schema: "Lab",
                table: "SampleBookings",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "CollectionStartedAt",
                schema: "Lab",
                table: "SampleBookings",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CollectionStatusNotes",
                schema: "Lab",
                table: "SampleBookings",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "CollectionWindowEndAt",
                schema: "Lab",
                table: "SampleBookings",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "ReportGeneratedAt",
                schema: "Lab",
                table: "SampleBookings",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ReportVerificationCode",
                schema: "Lab",
                table: "SampleBookings",
                type: "character varying(80)",
                maxLength: 80,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "ResultsReleasedAt",
                schema: "Lab",
                table: "SampleBookings",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "SampleCollectedAt",
                schema: "Lab",
                table: "SampleBookings",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "LabTestResults",
                schema: "Lab",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    LabSampleBookingId = table.Column<Guid>(type: "uuid", nullable: false),
                    LabBookingItemId = table.Column<Guid>(type: "uuid", nullable: false),
                    LabTestId = table.Column<Guid>(type: "uuid", nullable: false),
                    ResultNumber = table.Column<string>(type: "character varying(35)", maxLength: 35, nullable: false),
                    Status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    ParametersJson = table.Column<string>(type: "jsonb", nullable: false, defaultValueSql: "'[]'::jsonb"),
                    Summary = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    IsAbnormal = table.Column<bool>(type: "boolean", nullable: false),
                    HasCriticalValue = table.Column<bool>(type: "boolean", nullable: false),
                    CriticalValueSummary = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    AutoValidatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    EnteredByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    EnteredAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    TechnicianValidatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    TechnicianValidatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ManagerValidatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    ManagerValidatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ReleasedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    ReleasedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CriticalAlertSentAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CriticalAlertAcknowledgedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    CriticalAlertAcknowledgedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    AddendumNotes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    AddendumByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    AddendumAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LabTestResults", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LabTestResults_BookingItems_LabBookingItemId",
                        column: x => x.LabBookingItemId,
                        principalSchema: "Lab",
                        principalTable: "BookingItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_LabTestResults_LabTests_LabTestId",
                        column: x => x.LabTestId,
                        principalSchema: "Lab",
                        principalTable: "LabTests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_LabTestResults_SampleBookings_LabSampleBookingId",
                        column: x => x.LabSampleBookingId,
                        principalSchema: "Lab",
                        principalTable: "SampleBookings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_LabTestResults_Users_AddendumByUserId",
                        column: x => x.AddendumByUserId,
                        principalSchema: "Identity",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_LabTestResults_Users_CriticalAlertAcknowledgedByUserId",
                        column: x => x.CriticalAlertAcknowledgedByUserId,
                        principalSchema: "Identity",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_LabTestResults_Users_EnteredByUserId",
                        column: x => x.EnteredByUserId,
                        principalSchema: "Identity",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_LabTestResults_Users_ManagerValidatedByUserId",
                        column: x => x.ManagerValidatedByUserId,
                        principalSchema: "Identity",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_LabTestResults_Users_ReleasedByUserId",
                        column: x => x.ReleasedByUserId,
                        principalSchema: "Identity",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_LabTestResults_Users_TechnicianValidatedByUserId",
                        column: x => x.TechnicianValidatedByUserId,
                        principalSchema: "Identity",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PaymentTransactions",
                schema: "Payment",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true),
                    PharmacyOrderId = table.Column<Guid>(type: "uuid", nullable: true),
                    ReferenceType = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    ReferenceId = table.Column<Guid>(type: "uuid", nullable: false),
                    PaymentNumber = table.Column<string>(type: "character varying(35)", maxLength: 35, nullable: false),
                    Gateway = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    Status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    Amount = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    Currency = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    SessionToken = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    CheckoutUrl = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    ExternalReference = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    ExpiresAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    PaidAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    FailedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    FailureCode = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    FailureMessage = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    MetadataJson = table.Column<string>(type: "jsonb", nullable: false, defaultValueSql: "'{}'::jsonb"),
                    LastWebhookPayload = table.Column<string>(type: "jsonb", nullable: true),
                    LastWebhookReceivedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PaymentTransactions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PaymentTransactions_PharmacyOrders_PharmacyOrderId",
                        column: x => x.PharmacyOrderId,
                        principalSchema: "Pharmacy",
                        principalTable: "PharmacyOrders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PaymentTransactions_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalSchema: "Identity",
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "UserAuthSessions",
                schema: "Identity",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    RefreshTokenHash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    IssuedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ExpiresAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    RevokedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    LastSeenAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    IpAddress = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    UserAgent = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    DeviceLabel = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserAuthSessions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserAuthSessions_Users_UserId",
                        column: x => x.UserId,
                        principalSchema: "Identity",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserLoginActivities",
                schema: "Identity",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: true),
                    Email = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    IsSuccessful = table.Column<bool>(type: "boolean", nullable: false),
                    AttemptedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    IpAddress = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    UserAgent = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    FailureReason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserLoginActivities", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserLoginActivities_Users_UserId",
                        column: x => x.UserId,
                        principalSchema: "Identity",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PaymentInvoices",
                schema: "Payment",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true),
                    PaymentTransactionId = table.Column<Guid>(type: "uuid", nullable: false),
                    PharmacyOrderId = table.Column<Guid>(type: "uuid", nullable: true),
                    InvoiceNumber = table.Column<string>(type: "character varying(35)", maxLength: 35, nullable: false),
                    Status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    BillingName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    BillingEmail = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    BillingPhone = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    BillingAddress = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    SubTotal = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    DeliveryFee = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    TaxAmount = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    DiscountAmount = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    TotalAmount = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    Currency = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    IssuedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    PaidAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    RefundedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
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
                    table.PrimaryKey("PK_PaymentInvoices", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PaymentInvoices_PaymentTransactions_PaymentTransactionId",
                        column: x => x.PaymentTransactionId,
                        principalSchema: "Payment",
                        principalTable: "PaymentTransactions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PaymentInvoices_PharmacyOrders_PharmacyOrderId",
                        column: x => x.PharmacyOrderId,
                        principalSchema: "Pharmacy",
                        principalTable: "PharmacyOrders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PaymentInvoices_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalSchema: "Identity",
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PaymentRefunds",
                schema: "Payment",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PaymentTransactionId = table.Column<Guid>(type: "uuid", nullable: false),
                    PaymentInvoiceId = table.Column<Guid>(type: "uuid", nullable: true),
                    RefundNumber = table.Column<string>(type: "character varying(35)", maxLength: 35, nullable: false),
                    Status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    Amount = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    Currency = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    Reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    ExternalReference = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    RequestedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CompletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    RequestedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PaymentRefunds", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PaymentRefunds_PaymentInvoices_PaymentInvoiceId",
                        column: x => x.PaymentInvoiceId,
                        principalSchema: "Payment",
                        principalTable: "PaymentInvoices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PaymentRefunds_PaymentTransactions_PaymentTransactionId",
                        column: x => x.PaymentTransactionId,
                        principalSchema: "Payment",
                        principalTable: "PaymentTransactions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PaymentRefunds_Users_RequestedByUserId",
                        column: x => x.RequestedByUserId,
                        principalSchema: "Identity",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SampleBookings_CollectionAgentUserId",
                schema: "Lab",
                table: "SampleBookings",
                column: "CollectionAgentUserId");

            migrationBuilder.CreateIndex(
                name: "IX_SampleBookings_ReportVerificationCode",
                schema: "Lab",
                table: "SampleBookings",
                column: "ReportVerificationCode");

            migrationBuilder.CreateIndex(
                name: "IX_LabTestResults_AddendumByUserId",
                schema: "Lab",
                table: "LabTestResults",
                column: "AddendumByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_LabTestResults_CriticalAlertAcknowledgedByUserId",
                schema: "Lab",
                table: "LabTestResults",
                column: "CriticalAlertAcknowledgedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_LabTestResults_EnteredByUserId",
                schema: "Lab",
                table: "LabTestResults",
                column: "EnteredByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_LabTestResults_LabBookingItemId",
                schema: "Lab",
                table: "LabTestResults",
                column: "LabBookingItemId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LabTestResults_LabSampleBookingId",
                schema: "Lab",
                table: "LabTestResults",
                column: "LabSampleBookingId");

            migrationBuilder.CreateIndex(
                name: "IX_LabTestResults_LabTestId",
                schema: "Lab",
                table: "LabTestResults",
                column: "LabTestId");

            migrationBuilder.CreateIndex(
                name: "IX_LabTestResults_ManagerValidatedByUserId",
                schema: "Lab",
                table: "LabTestResults",
                column: "ManagerValidatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_LabTestResults_ReleasedByUserId",
                schema: "Lab",
                table: "LabTestResults",
                column: "ReleasedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_LabTestResults_ResultNumber",
                schema: "Lab",
                table: "LabTestResults",
                column: "ResultNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LabTestResults_Status_HasCriticalValue_IsAbnormal",
                schema: "Lab",
                table: "LabTestResults",
                columns: new[] { "Status", "HasCriticalValue", "IsAbnormal" });

            migrationBuilder.CreateIndex(
                name: "IX_LabTestResults_TechnicianValidatedByUserId",
                schema: "Lab",
                table: "LabTestResults",
                column: "TechnicianValidatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentInvoices_InvoiceNumber",
                schema: "Payment",
                table: "PaymentInvoices",
                column: "InvoiceNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PaymentInvoices_PaymentTransactionId",
                schema: "Payment",
                table: "PaymentInvoices",
                column: "PaymentTransactionId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PaymentInvoices_PharmacyOrderId",
                schema: "Payment",
                table: "PaymentInvoices",
                column: "PharmacyOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentInvoices_TenantId",
                schema: "Payment",
                table: "PaymentInvoices",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentRefunds_PaymentInvoiceId",
                schema: "Payment",
                table: "PaymentRefunds",
                column: "PaymentInvoiceId");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentRefunds_PaymentTransactionId",
                schema: "Payment",
                table: "PaymentRefunds",
                column: "PaymentTransactionId");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentRefunds_RefundNumber",
                schema: "Payment",
                table: "PaymentRefunds",
                column: "RefundNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PaymentRefunds_RequestedByUserId",
                schema: "Payment",
                table: "PaymentRefunds",
                column: "RequestedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentRefunds_Status_RequestedAt",
                schema: "Payment",
                table: "PaymentRefunds",
                columns: new[] { "Status", "RequestedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_PaymentTransactions_PaymentNumber",
                schema: "Payment",
                table: "PaymentTransactions",
                column: "PaymentNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PaymentTransactions_PharmacyOrderId",
                schema: "Payment",
                table: "PaymentTransactions",
                column: "PharmacyOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentTransactions_ReferenceType_ReferenceId_Status",
                schema: "Payment",
                table: "PaymentTransactions",
                columns: new[] { "ReferenceType", "ReferenceId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_PaymentTransactions_SessionToken",
                schema: "Payment",
                table: "PaymentTransactions",
                column: "SessionToken",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PaymentTransactions_TenantId",
                schema: "Payment",
                table: "PaymentTransactions",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_UserAuthSessions_RefreshTokenHash",
                schema: "Identity",
                table: "UserAuthSessions",
                column: "RefreshTokenHash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserAuthSessions_UserId",
                schema: "Identity",
                table: "UserAuthSessions",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_UserAuthSessions_UserId_ExpiresAt",
                schema: "Identity",
                table: "UserAuthSessions",
                columns: new[] { "UserId", "ExpiresAt" });

            migrationBuilder.CreateIndex(
                name: "IX_UserLoginActivities_Email_AttemptedAt",
                schema: "Identity",
                table: "UserLoginActivities",
                columns: new[] { "Email", "AttemptedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_UserLoginActivities_UserId",
                schema: "Identity",
                table: "UserLoginActivities",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_SampleBookings_Users_CollectionAgentUserId",
                schema: "Lab",
                table: "SampleBookings",
                column: "CollectionAgentUserId",
                principalSchema: "Identity",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SampleBookings_Users_CollectionAgentUserId",
                schema: "Lab",
                table: "SampleBookings");

            migrationBuilder.DropTable(
                name: "LabTestResults",
                schema: "Lab");

            migrationBuilder.DropTable(
                name: "PaymentRefunds",
                schema: "Payment");

            migrationBuilder.DropTable(
                name: "UserAuthSessions",
                schema: "Identity");

            migrationBuilder.DropTable(
                name: "UserLoginActivities",
                schema: "Identity");

            migrationBuilder.DropTable(
                name: "PaymentInvoices",
                schema: "Payment");

            migrationBuilder.DropTable(
                name: "PaymentTransactions",
                schema: "Payment");

            migrationBuilder.DropIndex(
                name: "IX_SampleBookings_CollectionAgentUserId",
                schema: "Lab",
                table: "SampleBookings");

            migrationBuilder.DropIndex(
                name: "IX_SampleBookings_ReportVerificationCode",
                schema: "Lab",
                table: "SampleBookings");

            migrationBuilder.DropColumn(
                name: "CollectionAgentUserId",
                schema: "Lab",
                table: "SampleBookings");

            migrationBuilder.DropColumn(
                name: "CollectionAssignedAt",
                schema: "Lab",
                table: "SampleBookings");

            migrationBuilder.DropColumn(
                name: "CollectionStartedAt",
                schema: "Lab",
                table: "SampleBookings");

            migrationBuilder.DropColumn(
                name: "CollectionStatusNotes",
                schema: "Lab",
                table: "SampleBookings");

            migrationBuilder.DropColumn(
                name: "CollectionWindowEndAt",
                schema: "Lab",
                table: "SampleBookings");

            migrationBuilder.DropColumn(
                name: "ReportGeneratedAt",
                schema: "Lab",
                table: "SampleBookings");

            migrationBuilder.DropColumn(
                name: "ReportVerificationCode",
                schema: "Lab",
                table: "SampleBookings");

            migrationBuilder.DropColumn(
                name: "ResultsReleasedAt",
                schema: "Lab",
                table: "SampleBookings");

            migrationBuilder.DropColumn(
                name: "SampleCollectedAt",
                schema: "Lab",
                table: "SampleBookings");
        }
    }
}
