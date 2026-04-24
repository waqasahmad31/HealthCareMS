using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HealthCareMS.Infrastructure.Persistence.Migrations.Foundation
{
    /// <inheritdoc />
    public partial class _20260423_AddDynamicNavigationAssignmentsAndApplicationLinks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CssClass",
                schema: "Identity",
                table: "NavigationIcons",
                type: "character varying(160)",
                maxLength: 160,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LabelEn",
                schema: "Identity",
                table: "NavigationIcons",
                type: "character varying(160)",
                maxLength: 160,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "LabelUr",
                schema: "Identity",
                table: "NavigationIcons",
                type: "character varying(160)",
                maxLength: 160,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "UserNavigationAssignments",
                schema: "Identity",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    NavigationItemId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserNavigationAssignments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserNavigationAssignments_NavigationItems_NavigationItemId",
                        column: x => x.NavigationItemId,
                        principalSchema: "Identity",
                        principalTable: "NavigationItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserNavigationAssignments_Users_UserId",
                        column: x => x.UserId,
                        principalSchema: "Identity",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_UserNavigationAssignments_NavigationItemId",
                schema: "Identity",
                table: "UserNavigationAssignments",
                column: "NavigationItemId");

            migrationBuilder.CreateIndex(
                name: "IX_UserNavigationAssignments_UserId",
                schema: "Identity",
                table: "UserNavigationAssignments",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_UserNavigationAssignments_UserId_NavigationItemId_IsDeleted",
                schema: "Identity",
                table: "UserNavigationAssignments",
                columns: new[] { "UserId", "NavigationItemId", "IsDeleted" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UserNavigationAssignments",
                schema: "Identity");

            migrationBuilder.DropColumn(
                name: "CssClass",
                schema: "Identity",
                table: "NavigationIcons");

            migrationBuilder.DropColumn(
                name: "LabelEn",
                schema: "Identity",
                table: "NavigationIcons");

            migrationBuilder.DropColumn(
                name: "LabelUr",
                schema: "Identity",
                table: "NavigationIcons");
        }
    }
}
