namespace FinPay.Application.Common;

/// <summary>
/// Standard generic API response wrapper.
/// </summary>
/// <typeparam name="T">Payload type.</typeparam>
public record ApiResponse<T>
{
    public bool Success { get; init; }
    public string Message { get; init; } = string.Empty;
    public T? Data { get; init; }
    public string? ErrorCode { get; init; }
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;

    public static ApiResponse<T> Ok(T data, string message = "Success") =>
        new()
        {
            Success = true,
            Message = message,
            Data = data,
            ErrorCode = null,
            Timestamp = DateTime.UtcNow
        };

    public static ApiResponse<T> Fail(string message, string? errorCode = null) =>
        new()
        {
            Success = false,
            Message = message,
            Data = default,
            ErrorCode = errorCode,
            Timestamp = DateTime.UtcNow
        };
}

/// <summary>
/// Non-generic convenience wrapper for empty responses.
/// </summary>
public record ApiResponse : ApiResponse<object?>
{
    public static ApiResponse Ok(string message = "Success") =>
        new()
        {
            Success = true,
            Message = message,
            Data = null,
            ErrorCode = null,
            Timestamp = DateTime.UtcNow
        };

    public static new ApiResponse Fail(string message, string? errorCode = null) =>
        new()
        {
            Success = false,
            Message = message,
            Data = null,
            ErrorCode = errorCode,
            Timestamp = DateTime.UtcNow
        };
}
