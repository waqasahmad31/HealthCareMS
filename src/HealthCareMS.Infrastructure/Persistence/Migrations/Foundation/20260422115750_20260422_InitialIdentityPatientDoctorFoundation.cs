using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HealthCareMS.Infrastructure.Persistence.Migrations.Foundation
{
    /// <inheritdoc />
    public partial class _20260422_InitialIdentityPatientDoctorFoundation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "Doctor");

            migrationBuilder.EnsureSchema(
                name: "Patient");

            migrationBuilder.EnsureSchema(
                name: "Identity");

            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:PostgresExtension:pgcrypto", ",,");

            migrationBuilder.CreateTable(
                name: "Permissions",
                schema: "Identity",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PermissionKey = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Module = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Action = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Permissions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Doctors",
                schema: "Doctor",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true),
                    PmdcRegistrationNumber = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Specialization = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    Qualification = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    Biography = table.Column<string>(type: "text", nullable: true),
                    City = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    ConsultationFee = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    IsVerified = table.Column<bool>(type: "boolean", nullable: false),
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
                    table.PrimaryKey("PK_Doctors", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DoctorSchedules",
                schema: "Doctor",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DoctorId = table.Column<Guid>(type: "uuid", nullable: false),
                    DayOfWeek = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    StartTime = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    EndTime = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    SlotDurationMinutes = table.Column<short>(type: "smallint", nullable: false),
                    IsOnlineAvailable = table.Column<bool>(type: "boolean", nullable: false),
                    IsOnSiteAvailable = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DoctorSchedules", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DoctorSchedules_Doctors_DoctorId",
                        column: x => x.DoctorId,
                        principalSchema: "Doctor",
                        principalTable: "Doctors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MedicalHistories",
                schema: "Patient",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PatientId = table.Column<Guid>(type: "uuid", nullable: false),
                    Allergies = table.Column<string>(type: "jsonb", nullable: false, defaultValueSql: "'[]'::jsonb"),
                    ChronicDiseases = table.Column<string>(type: "jsonb", nullable: false, defaultValueSql: "'[]'::jsonb"),
                    CurrentMedications = table.Column<string>(type: "jsonb", nullable: false, defaultValueSql: "'[]'::jsonb"),
                    PastSurgeries = table.Column<string>(type: "jsonb", nullable: false, defaultValueSql: "'[]'::jsonb"),
                    FamilyHistory = table.Column<string>(type: "jsonb", nullable: false, defaultValueSql: "'[]'::jsonb"),
                    SmokingStatus = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false, defaultValue: "Non-Smoker"),
                    AlcoholStatus = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false, defaultValue: "Non-User"),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MedicalHistories", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Patients",
                schema: "Patient",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    FirstName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    LastName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Cnic = table.Column<string>(type: "character varying(15)", maxLength: 15, nullable: true),
                    DateOfBirth = table.Column<DateOnly>(type: "date", nullable: false),
                    Gender = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    BloodGroup = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    ProfilePictureUrl = table.Column<string>(type: "text", nullable: true),
                    Phone = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    AlternatePhone = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    AddressStreet = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    AddressCity = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    AddressProvince = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    AddressPostalCode = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    EmergencyContactName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    EmergencyContactPhone = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    EmergencyContactRelation = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    InsuranceProvider = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    InsurancePolicyNo = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
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
                    table.PrimaryKey("PK_Patients", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RolePermissions",
                schema: "Identity",
                columns: table => new
                {
                    RoleId = table.Column<Guid>(type: "uuid", nullable: false),
                    PermissionId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RolePermissions", x => new { x.RoleId, x.PermissionId });
                    table.ForeignKey(
                        name: "FK_RolePermissions_Permissions_PermissionId",
                        column: x => x.PermissionId,
                        principalSchema: "Identity",
                        principalTable: "Permissions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Roles",
                schema: "Identity",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    IsSystemRole = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Roles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Tenants",
                schema: "Identity",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    TenantType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    LicenseNumber = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    OwnerName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    OwnerCnic = table.Column<string>(type: "character varying(15)", maxLength: 15, nullable: true),
                    Phone = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Email = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    Address = table.Column<string>(type: "jsonb", nullable: false, defaultValueSql: "'{}'::jsonb"),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    SubscriptionPlan = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    MaxUsers = table.Column<int>(type: "integer", nullable: false),
                    EnabledModules = table.Column<string>(type: "jsonb", nullable: false, defaultValueSql: "'{}'::jsonb"),
                    PaymentGateways = table.Column<string>(type: "jsonb", nullable: false, defaultValueSql: "'{}'::jsonb"),
                    CreatedBySuperAdminId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Tenants", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                schema: "Identity",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true),
                    RoleId = table.Column<Guid>(type: "uuid", nullable: false),
                    FirstName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    LastName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Email = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    PhoneNumber = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    PasswordHash = table.Column<string>(type: "text", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    IsEmailVerified = table.Column<bool>(type: "boolean", nullable: false),
                    FailedLoginCount = table.Column<short>(type: "smallint", nullable: false),
                    LockoutUntil = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    LastLoginAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    RefreshToken = table.Column<string>(type: "text", nullable: true),
                    RefreshTokenExpiry = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    TwoFactorEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    TwoFactorSecret = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Users_Roles_RoleId",
                        column: x => x.RoleId,
                        principalSchema: "Identity",
                        principalTable: "Roles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Users_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalSchema: "Identity",
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "UserPermissions",
                schema: "Identity",
                columns: table => new
                {
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    PermissionId = table.Column<Guid>(type: "uuid", nullable: false),
                    IsGranted = table.Column<bool>(type: "boolean", nullable: false),
                    GrantedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    GrantedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserPermissions", x => new { x.UserId, x.PermissionId });
                    table.ForeignKey(
                        name: "FK_UserPermissions_Permissions_PermissionId",
                        column: x => x.PermissionId,
                        principalSchema: "Identity",
                        principalTable: "Permissions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserPermissions_Users_GrantedByUserId",
                        column: x => x.GrantedByUserId,
                        principalSchema: "Identity",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_UserPermissions_Users_UserId",
                        column: x => x.UserId,
                        principalSchema: "Identity",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Doctors_PmdcRegistrationNumber",
                schema: "Doctor",
                table: "Doctors",
                column: "PmdcRegistrationNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Doctors_Specialization_City",
                schema: "Doctor",
                table: "Doctors",
                columns: new[] { "Specialization", "City" });

            migrationBuilder.CreateIndex(
                name: "IX_Doctors_TenantId",
                schema: "Doctor",
                table: "Doctors",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_Doctors_UserId",
                schema: "Doctor",
                table: "Doctors",
                column: "UserId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DoctorSchedules_DoctorId_DayOfWeek",
                schema: "Doctor",
                table: "DoctorSchedules",
                columns: new[] { "DoctorId", "DayOfWeek" });

            migrationBuilder.CreateIndex(
                name: "IX_MedicalHistories_PatientId",
                schema: "Patient",
                table: "MedicalHistories",
                column: "PatientId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Patients_Cnic",
                schema: "Patient",
                table: "Patients",
                column: "Cnic",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Patients_UserId",
                schema: "Patient",
                table: "Patients",
                column: "UserId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Permissions_Module",
                schema: "Identity",
                table: "Permissions",
                column: "Module");

            migrationBuilder.CreateIndex(
                name: "IX_Permissions_PermissionKey",
                schema: "Identity",
                table: "Permissions",
                column: "PermissionKey",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RolePermissions_PermissionId",
                schema: "Identity",
                table: "RolePermissions",
                column: "PermissionId");

            migrationBuilder.CreateIndex(
                name: "IX_Roles_TenantId_Name",
                schema: "Identity",
                table: "Roles",
                columns: new[] { "TenantId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Tenants_CreatedBySuperAdminId",
                schema: "Identity",
                table: "Tenants",
                column: "CreatedBySuperAdminId");

            migrationBuilder.CreateIndex(
                name: "IX_Tenants_Email",
                schema: "Identity",
                table: "Tenants",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Tenants_LicenseNumber",
                schema: "Identity",
                table: "Tenants",
                column: "LicenseNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserPermissions_GrantedByUserId",
                schema: "Identity",
                table: "UserPermissions",
                column: "GrantedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_UserPermissions_PermissionId",
                schema: "Identity",
                table: "UserPermissions",
                column: "PermissionId");

            migrationBuilder.CreateIndex(
                name: "IX_Users_Email",
                schema: "Identity",
                table: "Users",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_RoleId",
                schema: "Identity",
                table: "Users",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "IX_Users_TenantId",
                schema: "Identity",
                table: "Users",
                column: "TenantId");

            migrationBuilder.AddForeignKey(
                name: "FK_Doctors_Tenants_TenantId",
                schema: "Doctor",
                table: "Doctors",
                column: "TenantId",
                principalSchema: "Identity",
                principalTable: "Tenants",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Doctors_Users_UserId",
                schema: "Doctor",
                table: "Doctors",
                column: "UserId",
                principalSchema: "Identity",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_MedicalHistories_Patients_PatientId",
                schema: "Patient",
                table: "MedicalHistories",
                column: "PatientId",
                principalSchema: "Patient",
                principalTable: "Patients",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Patients_Users_UserId",
                schema: "Patient",
                table: "Patients",
                column: "UserId",
                principalSchema: "Identity",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_RolePermissions_Roles_RoleId",
                schema: "Identity",
                table: "RolePermissions",
                column: "RoleId",
                principalSchema: "Identity",
                principalTable: "Roles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Roles_Tenants_TenantId",
                schema: "Identity",
                table: "Roles",
                column: "TenantId",
                principalSchema: "Identity",
                principalTable: "Tenants",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Tenants_Users_CreatedBySuperAdminId",
                schema: "Identity",
                table: "Tenants",
                column: "CreatedBySuperAdminId",
                principalSchema: "Identity",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Roles_Tenants_TenantId",
                schema: "Identity",
                table: "Roles");

            migrationBuilder.DropForeignKey(
                name: "FK_Users_Tenants_TenantId",
                schema: "Identity",
                table: "Users");

            migrationBuilder.DropTable(
                name: "DoctorSchedules",
                schema: "Doctor");

            migrationBuilder.DropTable(
                name: "MedicalHistories",
                schema: "Patient");

            migrationBuilder.DropTable(
                name: "RolePermissions",
                schema: "Identity");

            migrationBuilder.DropTable(
                name: "UserPermissions",
                schema: "Identity");

            migrationBuilder.DropTable(
                name: "Doctors",
                schema: "Doctor");

            migrationBuilder.DropTable(
                name: "Patients",
                schema: "Patient");

            migrationBuilder.DropTable(
                name: "Permissions",
                schema: "Identity");

            migrationBuilder.DropTable(
                name: "Tenants",
                schema: "Identity");

            migrationBuilder.DropTable(
                name: "Users",
                schema: "Identity");

            migrationBuilder.DropTable(
                name: "Roles",
                schema: "Identity");
        }
    }
}
