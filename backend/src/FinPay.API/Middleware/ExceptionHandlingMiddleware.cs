using System.Net;
using System.Text.Json;
using FinPay.Application.Common;
using FinPay.Application.Exceptions;
using FinPay.Domain.Exceptions;

namespace FinPay.API.Middleware;

public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate _next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        this._next = _next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var (statusCode, errorCode, message) = exception switch
        {
            InsufficientFundsException ifEx => (
                HttpStatusCode.BadRequest,
                "INSUFFICIENT_FUNDS",
                ifEx.Message),

            WalletFrozenException wfEx => (
                HttpStatusCode.BadRequest,
                "WALLET_FROZEN",
                wfEx.Message),

            SelfTransferException stEx => (
                HttpStatusCode.BadRequest,
                "SELF_TRANSFER_FORBIDDEN",
                stEx.Message),

            DailyLimitExceededException dlEx => (
                HttpStatusCode.BadRequest,
                "DAILY_LIMIT_EXCEEDED",
                dlEx.Message),

            DomainValidationException dvEx => (
                HttpStatusCode.BadRequest,
                "DOMAIN_VALIDATION_ERROR",
                dvEx.Message),

            ValidationException vEx => (
                HttpStatusCode.BadRequest,
                "VALIDATION_ERROR",
                vEx.Message),

            EntityNotFoundException enfEx => (
                HttpStatusCode.NotFound,
                "NOT_FOUND",
                enfEx.Message),

            UnauthorizedException uEx => (
                HttpStatusCode.Unauthorized,
                "UNAUTHORIZED",
                uEx.Message),

            ConcurrencyException cEx => (
                HttpStatusCode.Conflict,
                "CONCURRENCY_CONFLICT",
                cEx.Message),

            _ => (
                HttpStatusCode.InternalServerError,
                "INTERNAL_SERVER_ERROR",
                "An unexpected server error occurred. Please contact customer support.")
        };

        if (statusCode == HttpStatusCode.InternalServerError)
        {
            _logger.LogError(exception, "Unhandled server error: {Message}", exception.Message);
        }
        else
        {
            _logger.LogWarning("Handled domain/application error [{ErrorCode}]: {Message}", errorCode, message);
        }

        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)statusCode;

        var response = ApiResponse.Fail(message, errorCode);
        var json = JsonSerializer.Serialize(response, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        await context.Response.WriteAsync(json);
    }
}
