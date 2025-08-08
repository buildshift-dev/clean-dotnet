using System;
using System.Collections.Generic;
using System.Linq;

namespace SharedKernel.Common
{
    /// <summary>
    /// Represents the result of an operation with support for success/failure states.
    /// </summary>
    public class Result
    {
        protected Result(bool isSuccess, string error)
        {
            if (isSuccess && !string.IsNullOrEmpty(error))
                throw new InvalidOperationException("Success result cannot have an error message");
            if (!isSuccess && string.IsNullOrEmpty(error))
                throw new InvalidOperationException("Failure result must have an error message");

            IsSuccess = isSuccess;
            Error = error;
        }

        public bool IsSuccess { get; }
        public bool IsFailure => !IsSuccess;
        public string Error { get; }

        public static Result Success() => new(true, string.Empty);
        public static Result Failure(string error) => new(false, error);

        public static Result<T> Success<T>(T value) => Result<T>.Success(value);
        public static Result<T> Failure<T>(string error) => Result<T>.Failure(error);

        public static Result Combine(params Result[] results)
        {
            var failures = results.Where(r => r.IsFailure).ToList();

            if (failures.Any())
            {
                var errors = string.Join("; ", failures.Select(f => f.Error));
                return Failure(errors);
            }

            return Success();
        }
    }

    /// <summary>
    /// Represents the result of an operation that returns a value.
    /// </summary>
    public class Result<T> : Result
    {
        private readonly T? _value;

        protected Result(T? value, bool isSuccess, string error)
            : base(isSuccess, error)
        {
            _value = value;
        }

        public T Value
        {
            get
            {
                if (IsFailure)
                    throw new InvalidOperationException("Cannot access value of a failure result");

                return _value!;
            }
        }

        public static Result<T> Success(T value) => new(value, true, string.Empty);
        public static new Result<T> Failure(string error) => new(default, false, error);

        public Result<TNew> Map<TNew>(Func<T, TNew> mapper)
        {
            return IsSuccess
                ? Result<TNew>.Success(mapper(Value))
                : Result<TNew>.Failure(Error);
        }

        public async Task<Result<TNew>> MapAsync<TNew>(Func<T, Task<TNew>> mapper)
        {
            return IsSuccess
                ? Result<TNew>.Success(await mapper(Value))
                : Result<TNew>.Failure(Error);
        }

        public Result<T> Ensure(Func<T, bool> predicate, string error)
        {
            if (IsFailure)
                return this;

            return predicate(Value)
                ? this
                : Failure(error);
        }

        public Result<TNew> Bind<TNew>(Func<T, Result<TNew>> func)
        {
            return IsSuccess
                ? func(Value)
                : Result<TNew>.Failure(Error);
        }

        public async Task<Result<TNew>> BindAsync<TNew>(Func<T, Task<Result<TNew>>> func)
        {
            return IsSuccess
                ? await func(Value)
                : Result<TNew>.Failure(Error);
        }

        public T GetValueOrDefault(T defaultValue = default!)
        {
            return IsSuccess ? Value : defaultValue;
        }
    }
}