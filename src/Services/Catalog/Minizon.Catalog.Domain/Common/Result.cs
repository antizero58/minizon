namespace Minizon.Catalog.Domain.Common;

/// <summary>
/// Factories for <see cref="Result{T}"/>. Kept outside the generic type to avoid static members on it (CA1000).
/// </summary>
public static class Result
{
    public static Result<T> Success<T>(T value)
        where T : notnull
    {
        ArgumentNullException.ThrowIfNull(value);
        return new Result<T>(value, []);
    }

    public static Result<T> Failure<T>(IReadOnlyList<DomainError> errors)
        where T : notnull
    {
        ArgumentNullException.ThrowIfNull(errors);
        if (errors.Count == 0)
        {
            throw new ArgumentException("A failure must contain at least one error.", nameof(errors));
        }

        // Copy so that later changes to the caller's list cannot alter the result.
        return new Result<T>(default, [.. errors]);
    }
}
