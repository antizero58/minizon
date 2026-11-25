using System.Collections;
using Minizon.Catalog.Domain.Common;

namespace Minizon.Catalog.UnitTests.Common;

public class ResultTests
{
    private static readonly DomainError SomeError = new("test.field.invalid", "Invalid.");
    private static readonly DomainError OtherError = new("test.other.invalid", "Other.");

    [Fact]
    public void Success_HasValueAndNoErrors()
    {
        var result = Result.Success("value");

        Assert.True(result.IsSuccess);
        Assert.False(result.IsFailure);
        Assert.Equal("value", result.Value);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void Failure_KeepsErrorsInOrder()
    {
        var result = Result.Failure<string>([SomeError, OtherError]);

        Assert.True(result.IsFailure);
        Assert.False(result.IsSuccess);
        Assert.Equal([SomeError, OtherError], result.Errors);
    }

    [Fact]
    public void Value_OfFailure_ThrowsInvalidOperationException()
    {
        var result = Result.Failure<string>([SomeError]);

        Assert.Throws<InvalidOperationException>(() => result.Value);
    }

    [Fact]
    public void Failure_WithEmptyErrors_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => Result.Failure<string>([]));
    }

    [Fact]
    public void Failure_WithNullErrors_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => Result.Failure<string>(null!));
    }

    [Fact]
    public void Failure_IsNotAffectedByLaterChangesToSourceList()
    {
        var errors = new List<DomainError> { SomeError };

        var result = Result.Failure<string>(errors);
        errors.Add(OtherError);

        Assert.Equal([SomeError], result.Errors);
    }

    [Fact]
    public void Success_WithNullValue_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => Result.Success<string>(null!));
    }

    [Fact]
    public void Failure_ErrorsCannotBeChangedThroughAList()
    {
        var result = Result.Failure<string>([SomeError]);

        if (result.Errors is IList<DomainError> errors)
        {
            Assert.Throws<NotSupportedException>(() => { errors[0] = OtherError; });
            Assert.Throws<NotSupportedException>(() => errors.Add(OtherError));
            Assert.Throws<NotSupportedException>(() => errors.Clear());
        }

        Assert.Equal(SomeError, Assert.Single(result.Errors));
    }

    [Fact]
    public void Failure_ErrorsCannotBeChangedThroughSynchronizationRoot()
    {
        var result = Result.Failure<string>([SomeError]);

        if (result.Errors is ICollection collection && collection.SyncRoot is DomainError[] errors)
        {
            errors[0] = OtherError;
        }

        Assert.Equal(SomeError, Assert.Single(result.Errors));
    }
}
