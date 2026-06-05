namespace Conduit.Application.Common;

public enum ErrorKind
{
    None,
    Validation,
    Conflict,
    Unauthorized,
    NotFound,
    Forbidden
}

public sealed class Result<T>
{
    public ErrorKind Kind { get; init; }

    public T? Value { get; init; }

    public IReadOnlyDictionary<string, string[]> Errors { get; init; } =
        new Dictionary<string, string[]>();

    public bool IsSuccess => Kind == ErrorKind.None;

    public static Result<T> Ok(T value) =>
        new() { Kind = ErrorKind.None, Value = value };

    public static Result<T> Fail(
        ErrorKind kind,
        IReadOnlyDictionary<string, string[]> errors) =>
        new() { Kind = kind, Errors = errors };

    public static Result<T> Validation(string field, string message) =>
        Fail(ErrorKind.Validation, new Dictionary<string, string[]>
        {
            [field] = [message]
        });

    public static Result<T> Conflict(string field, string message) =>
        Fail(ErrorKind.Conflict, new Dictionary<string, string[]>
        {
            [field] = [message]
        });
}
