using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HealthCareMS.Infrastructure.Persistence.Migrations.Foundation
{
    /// <inheritdoc />
    public partial class _20260422_AddConsultationSessionFoundation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ConsultationSessions",
                schema: "Consultation",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AppointmentId = table.Column<Guid>(type: "uuid", nullable: false),
                    PatientId = table.Column<Guid>(type: "uuid", nullable: false),
                    DoctorId = table.Column<Guid>(type: "uuid", nullable: false),
                    ChannelName = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    MeetingLink = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false, defaultValue: "Waiting"),
                    PatientJoinedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DoctorJoinedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    StartedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    EndedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    LastTokenIssuedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConsultationSessions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ConsultationSessions_Appointments_AppointmentId",
                        column: x => x.AppointmentId,
                        principalSchema: "Appointment",
                        principalTable: "Appointments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ConsultationSessions_Doctors_DoctorId",
                        column: x => x.DoctorId,
                        principalSchema: "Doctor",
                        principalTable: "Doctors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ConsultationSessions_Patients_PatientId",
                        column: x => x.PatientId,
                        principalSchema: "Patient",
                        principalTable: "Patients",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ConsultationSessions_AppointmentId",
                schema: "Consultation",
                table: "ConsultationSessions",
                column: "AppointmentId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ConsultationSessions_ChannelName",
                schema: "Consultation",
                table: "ConsultationSessions",
                column: "ChannelName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ConsultationSessions_DoctorId_Status",
                schema: "Consultation",
                table: "ConsultationSessions",
                columns: new[] { "DoctorId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_ConsultationSessions_PatientId_Status",
                schema: "Consultation",
                table: "ConsultationSessions",
                columns: new[] { "PatientId", "Status" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ConsultationSessions",
                schema: "Consultation");
        }
    }
}
