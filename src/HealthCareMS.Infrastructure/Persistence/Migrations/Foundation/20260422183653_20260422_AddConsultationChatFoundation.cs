using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HealthCareMS.Infrastructure.Persistence.Migrations.Foundation
{
    /// <inheritdoc />
    public partial class _20260422_AddConsultationChatFoundation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ChatMessages",
                schema: "Consultation",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SessionId = table.Column<Guid>(type: "uuid", nullable: false),
                    SenderUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    SenderType = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    SenderDisplayName = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    MessageType = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "Text"),
                    MessageText = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    AttachmentFileName = table.Column<string>(type: "character varying(260)", maxLength: 260, nullable: true),
                    AttachmentContentType = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    AttachmentStoragePath = table.Column<string>(type: "character varying(600)", maxLength: 600, nullable: true),
                    AttachmentSizeBytes = table.Column<long>(type: "bigint", nullable: true),
                    SentAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    PatientReadAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DoctorReadAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChatMessages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ChatMessages_ConsultationSessions_SessionId",
                        column: x => x.SessionId,
                        principalSchema: "Consultation",
                        principalTable: "ConsultationSessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ChatMessages_SessionId_SenderType",
                schema: "Consultation",
                table: "ChatMessages",
                columns: new[] { "SessionId", "SenderType" });

            migrationBuilder.CreateIndex(
                name: "IX_ChatMessages_SessionId_SentAt",
                schema: "Consultation",
                table: "ChatMessages",
                columns: new[] { "SessionId", "SentAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ChatMessages",
                schema: "Consultation");
        }
    }
}
