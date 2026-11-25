using System.Collections.Immutable;

namespace Minizon.Catalog.Domain.Common;

/// <summary>
/// Either a value or a non-empty list of domain errors. Create it through <see cref="Result"/>.
/// </summary>
public sealed class Result<T>
    where T : notnull
{
    private readonly T? _value;

    internal Result(T? value, ImmutableList<DomainError> errors)
    {
        _value = value;
        Errors = errors;
    }

    public bool IsSuccess => Errors.Count == 0;

    public bool IsFailure => !IsSuccess;

    /// <summary>The value of a successful result.</summary>
    /// <exception cref="InvalidOperationException">The result is a failure.</exception>
    public T Value => IsSuccess
        ? _value!
        : throw new InvalidOperationException("Cannot access the value of a failed result.");

    /// <summary>The errors of a failed result; empty on success.</summary>
    public IReadOnlyList<DomainError> Errors { get; }
}
