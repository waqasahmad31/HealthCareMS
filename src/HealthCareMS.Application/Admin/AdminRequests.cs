namespace HealthCareMS.Application.Admin;

public sealed record UpdateDoctorAdminStatusRequest(bool IsVerified, bool IsActive);

public sealed record UpdateSystemSettingRequest(string Value);
