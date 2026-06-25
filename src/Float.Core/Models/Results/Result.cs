using System;
using Float.Core.Models.Results.Errors;

namespace Float.Core.Models.Results;

public class Result
{
    protected Result(bool isSuccess, Error error)
    {
        if (isSuccess && error != Error.Empty
            || !isSuccess && error == Error.Empty)
        {
            throw new ArgumentException("Invalid error provided", nameof(error));
        }

        IsSuccess = isSuccess;
        Failure = error;
    }

    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;
    public Error Failure { get; }

    public static Result WithSuccess() => new(true, Error.Empty);
    public static Result WithFailure(Error error) => new(false, error);

    public static Result<TValue> WithSuccess<TValue>(TValue value) => Result<TValue>.WithSuccess(value);
    public static Result<TValue> WithFailure<TValue>(Error error) => Result<TValue>.WithFailure(error);

    public TOut Match<TOut>(Func<TOut> onSuccess, Func<Error, TOut> onFailure)
        => IsSuccess ? onSuccess() : onFailure(Failure);

    public void Match(Action onSuccess, Action onFailure)
    {
        if (IsSuccess) onSuccess();
        else onFailure();
    }

    public void Match(Action onSuccess, Action<Error> onFailure)
    {
        if (IsSuccess) onSuccess();
        else onFailure(Failure);
    }
}

public class Result<TValue> : Result
{
    private Result(TValue value) : base(true, Error.Empty)
    {
        if (value is null) throw new ArgumentNullException(nameof(value));
        Value = value;
    }

    private Result(Error error) : base(false, error)
        => Value = default;

    private TValue? Value { get; }

    public TValue GetValueOrThrow()
        => IsSuccess ? Value! : throw new InvalidOperationException(Failure.Message ?? "Operation failed");

    public TValue? GetValueOrDefault() => Value;

    public static Result<TValue> WithSuccess(TValue value) => new(value);
    public static new Result<TValue> WithFailure(Error error) => new(error);

    public void Match(Action<TValue> onSuccess, Action<Error> onFailure)
    {
        if (IsSuccess) onSuccess(Value!);
        else onFailure(Failure);
    }

    public TOut Match<TOut>(Func<TValue, TOut> onSuccess, Func<Error, TOut> onFailure)
        => IsSuccess ? onSuccess(Value!) : onFailure(Failure);
}