namespace Vexis.Foundation;

public readonly record struct Result(bool IsSuccess, string? Error)
{
    public static Result Success() => new(true, null);
    public static Result Failure(string error) => new(false, error);

    public void ThrowIfFailure()
    {
        if (!IsSuccess)
        {
            throw new InvalidOperationException(Error ?? "The operation failed.");
        }
    }
}

public readonly record struct Result<T>(bool IsSuccess, T? Value, string? Error)
{
    public static Result<T> Success(T value) => new(true, value, null);
    public static Result<T> Failure(string error) => new(false, default, error);
}
