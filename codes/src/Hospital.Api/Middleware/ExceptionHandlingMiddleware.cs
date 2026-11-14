using System.ComponentModel.DataAnnotations;
using Hospital.Application.Common.Exceptions;
using Hospital.Application.Common.Models;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace Hospital.Api.Middleware;

public class ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try { await next(context); }
        catch (OperationCanceledException) when (context.RequestAborted.IsCancellationRequested) { }
        catch (Exception ex)
        {
            if (context.Response.HasStarted) throw;
            var (status, code, message) = Classify(ex);
            if (status >= 500) logger.LogError(ex, "Request failed. TraceId: {TraceId}", context.TraceIdentifier);
            else logger.LogWarning("Request rejected: {Code}. TraceId: {TraceId}", code, context.TraceIdentifier);
            context.Response.StatusCode = status;
            var response = ApiResponse.Failed(message, code);
            response.TraceId = context.TraceIdentifier;
            await context.Response.WriteAsJsonAsync(response);
        }
    }

    private static (int, string, string) Classify(Exception ex) => ex switch
    {
        // EF wraps the SQL error when its connection retry policy is exhausted.
        RetryLimitExceededException { InnerException: { } inner } => Classify(inner),
        BusinessRuleException e when e.ErrorCode is "INVALID_CREDENTIALS" or "ACCOUNT_INACTIVE" => (401, e.ErrorCode, e.Message),
        BusinessRuleException e when e.ErrorCode.Contains("ALREADY_EXISTS") || e.ErrorCode is "TIMESLOT_OCCUPIED" or "IDEMPOTENCY_KEY_REUSED" => (409, e.ErrorCode, e.Message),
        BusinessRuleException e => (400, e.ErrorCode, e.Message),
        ValidationException e => (400, "VALIDATION_ERROR", e.Message),
        NotFoundException e => (404, "NOT_FOUND", e.Message),
        ForbiddenAccessException e => (403, "FORBIDDEN", e.Message),
        ConcurrencyException e => (409, "CONCURRENCY_CONFLICT", e.Message),
        DbUpdateConcurrencyException => (409, "CONCURRENCY_CONFLICT", "Dữ liệu vừa thay đổi. Vui lòng tải lại."),
        DbUpdateException { InnerException: SqlException { Number: 2601 or 2627 } } =>
            (409, "DUPLICATE_DATA", "Thông tin đã tồn tại hoặc khung giờ vừa được đặt. Vui lòng tải lại."),
        SqlException or DbUpdateException { InnerException: SqlException } =>
            (503, "DATABASE_UNAVAILABLE", "Chưa thể lưu hoặc tải dữ liệu. Vui lòng tra cứu lại trước khi gửi lại yêu cầu."),
        _ => (500, "INTERNAL_SERVER_ERROR", "Đã xảy ra lỗi hệ thống. Vui lòng cung cấp mã trace khi cần hỗ trợ.")
    };
}
