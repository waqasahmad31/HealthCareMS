namespace HealthCareMS.Shared.Common;

public class Result
{
    protected Result(bool isSuccess, Error error)
    {
        if (isSuccess && error != Error.None)
        {
            throw new InvalidOperationException("Successful results cannot carry an error.");
        }

        if (!isSuccess && error == Error.None)
        {
            throw new InvalidOperationException("Failed results must carry an error.");
        }

        IsSuccess = isSuccess;
        Error = error;
    }

    public bool IsSuccess { get; }

    public bool IsFailure => !IsSuccess;

    public Error Error { get; }

    public static Result Success()
    {
        return new(true, Error.None);
    }

    public static Result Failure(Error error)
    {
        return new(false, error);
    }
}

public sealed class Result<T> : Result
{
    private readonly T? value;

    private Result(T value)
        : base(true, Error.None)
    {
        this.value = value;
    }

    private Result(Error error)
        : base(false, error)
    {
    }

    public T Value => IsSuccess
        ? value!
        : throw new InvalidOperationException("Cannot access the value of a failed result.");

    public static Result<T> Success(T value)
    {
        return new(value);
    }

    public static new Result<T> Failure(Error error)
    {
        return new(error);
    }
}
