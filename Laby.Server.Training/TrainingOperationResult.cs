using Microsoft.AspNetCore.Mvc;

namespace Laby.Server.Training;

public sealed class TrainingOperationResult<T>
{
    public int StatusCode { get; }

    public T? Value { get; }

    public ProblemDetails? Problem { get; }

    private TrainingOperationResult(int statusCode, T? value, ProblemDetails? problem)
    {
        StatusCode = statusCode;
        Value = value;
        Problem = problem;
    }

    public static TrainingOperationResult<T> Success(int statusCode, T? value = default) =>
        new(statusCode, value, null);

    public static TrainingOperationResult<T> Failure(int statusCode, string title, string detail) =>
        new(
            statusCode,
            default,
            new ProblemDetails
            {
                Status = statusCode,
                Title = title,
                Detail = detail
            }
        );
}
