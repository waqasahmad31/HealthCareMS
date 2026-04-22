using HealthCareMS.Domain.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HealthCareMS.Infrastructure.Persistence.Configurations.Identity;

public sealed class TenantConfiguration : IEntityTypeConfiguration<Tenant>
{
    public void Configure(EntityTypeBuilder<Tenant> builder)
    {
        builder.ToTable("Tenants", DatabaseSchemas.Identity);
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Name).HasMaxLength(200).IsRequired();
        builder.Property(x => x.TenantType).HasConversion<string>().HasMaxLength(50).IsRequired();
        builder.Property(x => x.LicenseNumber).HasMaxLength(100);
        builder.Property(x => x.OwnerName).HasMaxLength(200).IsRequired();
        builder.Property(x => x.OwnerCnic).HasMaxLength(15);
        builder.Property(x => x.Phone).HasMaxLength(20).IsRequired();
        builder.Property(x => x.Email).HasMaxLength(255).IsRequired();
        builder.Property(x => x.Address).HasColumnType("jsonb").HasDefaultValueSql("'{}'::jsonb").IsRequired();
        builder.Property(x => x.SubscriptionPlan).HasConversion<string>().HasMaxLength(50).IsRequired();
        builder.Property(x => x.EnabledModules).HasColumnType("jsonb").HasDefaultValueSql("'{}'::jsonb").IsRequired();
        builder.Property(x => x.PaymentGateways).HasColumnType("jsonb").HasDefaultValueSql("'{}'::jsonb").IsRequired();

        builder.HasIndex(x => x.Email).IsUnique();
        builder.HasIndex(x => x.LicenseNumber).IsUnique();
        builder.HasQueryFilter(x => !x.IsDeleted);

        builder
            .HasOne(x => x.CreatedBySuperAdmin)
            .WithMany()
            .HasForeignKey(x => x.CreatedBySuperAdminId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class ApplicationUserConfiguration : IEntityTypeConfiguration<ApplicationUser>
{
    public void Configure(EntityTypeBuilder<ApplicationUser> builder)
    {
        builder.ToTable("Users", DatabaseSchemas.Identity);
        builder.HasKey(x => x.Id);

        builder.Property(x => x.FirstName).HasMaxLength(100).IsRequired();
        builder.Property(x => x.LastName).HasMaxLength(100).IsRequired();
        builder.Property(x => x.Email).HasMaxLength(255).IsRequired();
        builder.Property(x => x.PhoneNumber).HasMaxLength(20);
        builder.Property(x => x.PasswordHash).IsRequired();
        builder.Property(x => x.RefreshToken);
        builder.Property(x => x.TwoFactorSecret);

        builder.Ignore(x => x.FullName);

        builder.HasIndex(x => x.Email).IsUnique();
        builder.HasIndex(x => x.TenantId);
        builder.HasQueryFilter(x => !x.IsDeleted);

        builder
            .HasOne(x => x.Tenant)
            .WithMany(x => x.Users)
            .HasForeignKey(x => x.TenantId)
            .OnDelete(DeleteBehavior.Restrict);

        builder
            .HasOne(x => x.Role)
            .WithMany(x => x.Users)
            .HasForeignKey(x => x.RoleId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class RoleConfiguration : IEntityTypeConfiguration<Role>
{
    public void Configure(EntityTypeBuilder<Role> builder)
    {
        builder.ToTable("Roles", DatabaseSchemas.Identity);
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Name).HasMaxLength(100).IsRequired();
        builder.HasIndex(x => new { x.TenantId, x.Name }).IsUnique();
        builder.HasQueryFilter(x => !x.IsDeleted);

        builder
            .HasOne(x => x.Tenant)
            .WithMany(x => x.Roles)
            .HasForeignKey(x => x.TenantId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class PermissionConfiguration : IEntityTypeConfiguration<Permission>
{
    public void Configure(EntityTypeBuilder<Permission> builder)
    {
        builder.ToTable("Permissions", DatabaseSchemas.Identity);
        builder.HasKey(x => x.Id);

        builder.Property(x => x.PermissionKey).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Module).HasMaxLength(100).IsRequired();
        builder.Property(x => x.Action).HasMaxLength(50).IsRequired();
        builder.Property(x => x.Description).IsRequired();

        builder.HasIndex(x => x.PermissionKey).IsUnique();
        builder.HasIndex(x => x.Module);
        builder.HasQueryFilter(x => !x.IsDeleted);
    }
}

public sealed class RolePermissionConfiguration : IEntityTypeConfiguration<RolePermission>
{
    public void Configure(EntityTypeBuilder<RolePermission> builder)
    {
        builder.ToTable("RolePermissions", DatabaseSchemas.Identity);
        builder.HasKey(x => new { x.RoleId, x.PermissionId });

        builder
            .HasOne(x => x.Role)
            .WithMany(x => x.RolePermissions)
            .HasForeignKey(x => x.RoleId)
            .OnDelete(DeleteBehavior.Cascade);

        builder
            .HasOne(x => x.Permission)
            .WithMany(x => x.RolePermissions)
            .HasForeignKey(x => x.PermissionId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class UserPermissionConfiguration : IEntityTypeConfiguration<UserPermission>
{
    public void Configure(EntityTypeBuilder<UserPermission> builder)
    {
        builder.ToTable("UserPermissions", DatabaseSchemas.Identity);
        builder.HasKey(x => new { x.UserId, x.PermissionId });

        builder
            .HasOne(x => x.User)
            .WithMany(x => x.UserPermissions)
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder
            .HasOne(x => x.Permission)
            .WithMany(x => x.UserPermissions)
            .HasForeignKey(x => x.PermissionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder
            .HasOne(x => x.GrantedByUser)
            .WithMany()
            .HasForeignKey(x => x.GrantedByUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class SystemSettingConfiguration : IEntityTypeConfiguration<SystemSetting>
{
    public void Configure(EntityTypeBuilder<SystemSetting> builder)
    {
        builder.ToTable("SystemSettings", DatabaseSchemas.Identity);
        builder.HasKey(x => x.Id);

        builder.Property(x => x.SettingKey).HasMaxLength(120).IsRequired();
        builder.Property(x => x.GroupName).HasMaxLength(80).IsRequired();
        builder.Property(x => x.DisplayName).HasMaxLength(160).IsRequired();
        builder.Property(x => x.Value).HasMaxLength(2000).IsRequired();
        builder.Property(x => x.ValueType).HasMaxLength(40).HasDefaultValue("String").IsRequired();
        builder.Property(x => x.Description).HasMaxLength(500);
        builder.Property(x => x.IsEditable).HasDefaultValue(true);

        builder.HasIndex(x => x.SettingKey).IsUnique();
        builder.HasIndex(x => new { x.GroupName, x.DisplayName });
        builder.HasQueryFilter(x => !x.IsDeleted);
    }
}
