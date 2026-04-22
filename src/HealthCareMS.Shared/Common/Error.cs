namespace HealthCareMS.Shared.Common;

public sealed record ValidationError(string Field, string Message);

public sealed record Error(string Code, string Message, IReadOnlyList<ValidationError>? Details = null)
{
    public static readonly Error None = new(string.Empty, string.Empty);

    public static Error Validation(IReadOnlyList<ValidationError> details)
    {
        return new("VALIDATION_ERROR", "One or more validation errors occurred.", details);
    }
}
