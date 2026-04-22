using System.Text.Json;
using HealthCareMS.Application.Abstractions.Tenancy;
using HealthCareMS.Application.Tenants;
using HealthCareMS.Domain.Identity;
using HealthCareMS.Infrastructure.Persistence;
using HealthCareMS.Shared.Common;
using Microsoft.EntityFrameworkCore;

namespace HealthCareMS.Infrastructure.Tenants;

public sealed class TenantService(HealthCareDbContext dbContext, ICurrentUser currentUser) : ITenantService
{
    public async Task<Result<TenantResponse>> CreateAsync(CreateTenantRequest request, CancellationToken cancellationToken)
    {
        var validationErrors = Validate(request);
        if (validationErrors.Count > 0)
        {
            return Result<TenantResponse>.Failure(Error.Validation(validationErrors));
        }

        if (!Enum.TryParse<TenantType>(request.TenantType, ignoreCase: true, out var tenantType))
        {
            return Result<TenantResponse>.Failure(new Error("TENANT_TYPE_INVALID", "TenantType must be Pharmacy, Lab, Clinic, or Hospital."));
        }

        if (!Enum.TryParse<SubscriptionPlan>(request.SubscriptionPlan, ignoreCase: true, out var subscriptionPlan))
        {
            return Result<TenantResponse>.Failure(new Error("TENANT_PLAN_INVALID", "SubscriptionPlan must be Basic, Standard, or Premium."));
        }

        var email = request.Email.Trim().ToLowerInvariant();
        var emailExists = await dbContext.Tenants.AnyAsync(x => x.Email == email, cancellationToken);
        if (emailExists)
        {
            return Result<TenantResponse>.Failure(new Error("TENANT_EMAIL_EXISTS", "A tenant with this email already exists."));
        }

        if (!string.IsNullOrWhiteSpace(request.LicenseNumber))
        {
            var licenseExists = await dbContext.Tenants.AnyAsync(x => x.LicenseNumber == request.LicenseNumber.Trim(), cancellationToken);
            if (licenseExists)
            {
                return Result<TenantResponse>.Failure(new Error("TENANT_LICENSE_EXISTS", "A tenant with this license number already exists."));
            }
        }

        var tenant = new Tenant
        {
            Name = request.Name.Trim(),
            TenantType = tenantType,
            LicenseNumber = string.IsNullOrWhiteSpace(request.LicenseNumber) ? null : request.LicenseNumber.Trim(),
            OwnerName = request.OwnerName.Trim(),
            OwnerCnic = string.IsNullOrWhiteSpace(request.OwnerCnic) ? null : request.OwnerCnic.Trim(),
            Phone = request.Phone.Trim(),
            Email = email,
            Address = request.Address,
            SubscriptionPlan = subscriptionPlan,
            MaxUsers = request.MaxUsers,
            EnabledModules = request.EnabledModules,
            PaymentGateways = request.PaymentGateways,
            CreatedBySuperAdminId = currentUser.UserId
        };

        dbContext.Tenants.Add(tenant);
        await dbContext.SaveChangesAsync(cancellationToken);

        return Result<TenantResponse>.Success(Map(tenant));
    }

    public async Task<IReadOnlyList<TenantResponse>> GetAllAsync(CancellationToken cancellationToken)
    {
        var tenants = await dbContext.Tenants
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync(cancellationToken);

        return tenants.Select(Map).ToList();
    }

    private static TenantResponse Map(Tenant tenant)
    {
        return new TenantResponse(
            tenant.Id,
            tenant.Name,
            tenant.TenantType.ToString(),
            tenant.LicenseNumber,
            tenant.OwnerName,
            tenant.Phone,
            tenant.Email,
            tenant.IsActive,
            tenant.SubscriptionPlan.ToString(),
            tenant.MaxUsers,
            tenant.CreatedAt);
    }

    private static List<ValidationError> Validate(CreateTenantRequest request)
    {
        var errors = new List<ValidationError>();

        Required(request.Name, nameof(request.Name), errors);
        Required(request.TenantType, nameof(request.TenantType), errors);
        Required(request.OwnerName, nameof(request.OwnerName), errors);
        Required(request.Phone, nameof(request.Phone), errors);
        Required(request.Email, nameof(request.Email), errors);
        Required(request.Address, nameof(request.Address), errors);
        Required(request.SubscriptionPlan, nameof(request.SubscriptionPlan), errors);

        if (request.MaxUsers < 1)
        {
            errors.Add(new ValidationError(nameof(request.MaxUsers), "MaxUsers must be greater than zero."));
        }

        ValidateJson(request.Address, nameof(request.Address), errors);
        ValidateJson(request.EnabledModules, nameof(request.EnabledModules), errors);
        ValidateJson(request.PaymentGateways, nameof(request.PaymentGateways), errors);

        return errors;
    }

    private static void Required(string? value, string field, List<ValidationError> errors)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            errors.Add(new ValidationError(field, $"{field} is required."));
        }
    }

    private static void ValidateJson(string value, string field, List<ValidationError> errors)
    {
        try
        {
            using var _ = JsonDocument.Parse(value);
        }
        catch (JsonException)
        {
            errors.Add(new ValidationError(field, $"{field} must be valid JSON."));
        }
    }
}
