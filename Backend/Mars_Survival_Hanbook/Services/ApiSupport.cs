using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using System.Text.Json;
using Mars_Survival_Hanbook.Contracts;
using Mars_Survival_Hanbook.Data;
using Mars_Survival_Hanbook.Models;
using Mars_Survival_Hanbook.Models.Common;
using Mars_Survival_Hanbook.Models.Enums;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace Mars_Survival_Hanbook.Services;

public sealed class ApiException(int status, string code, string message) : Exception(message)
{
    public int Status { get; } = status;
    public string Code { get; } = code;
    public static ApiException Missing() => new(404, "not_found", "Không tìm thấy tài nguyên.");
    public static ApiException Invalid(string message) => new(400, "invalid_request", message);
    public static ApiException Gate(string message) => new(422, "publication_gate", message);
    public static ApiException Conflict(string message) => new(409, "conflict", message);
}
public class ApiExceptionMiddleware(RequestDelegate next, ILogger<ApiExceptionMiddleware> log)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try { await next(context); }
        catch (Exception e) when (!context.Response.HasStarted)
        {
            if (e is OperationCanceledException && context.RequestAborted.IsCancellationRequested) return;
            var (status, code, detail) = e switch
            {
                ApiException a => (a.Status, a.Code, a.Message),
                DbUpdateConcurrencyException => (412, "stale_etag", "Tài nguyên đã thay đổi; tải lại ETag."),
                DbUpdateException { InnerException: SqlException { Number: 2601 or 2627 } } => (409, "duplicate", "Định danh đã tồn tại."),
                JsonException or ValidationException => (400, "invalid_request", "Dữ liệu không hợp lệ."),
                _ => (500, "internal_error", "Không thể hoàn tất yêu cầu.")
            };
            if (status == 500) log.LogError(e, "Request failed {TraceId}", context.TraceIdentifier);
            context.Response.StatusCode = status;
            await context.Response.WriteAsJsonAsync(new ProblemDetails {
                Status = status, Title = detail, Type = "about:blank",
                Extensions = { ["code"] = code, ["traceId"] = context.TraceIdentifier }
            }, options: DiscoveryJson.Options, contentType: "application/problem+json");
        }
    }
}
public sealed class CurrentActor(IHttpContextAccessor accessor)
{
    public Guid Id => Guid.TryParse(accessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier), out var id)
        ? id : throw new ApiException(401, "unauthorized", "Yêu cầu đăng nhập.");
    public bool IsAdmin => accessor.HttpContext?.User.IsInRole(nameof(UserRole.Admin)) == true;
    public void Own(Guid owner) { if (!IsAdmin && owner != Id) throw new ApiException(403, "forbidden", "Không có quyền sửa tài nguyên này."); }
    public void ViewArticle(Article a) { if (!IsAdmin) Own(a.CreatedById); }
    public string? TraceId => accessor.HttpContext?.TraceIdentifier;
}
public static class Concurrency
{
    public static string ETag(Entity entity) => "\"" + Convert.ToBase64String(entity.RowVersion) + "\"";
    public static void Check(Entity entity, string? match)
    {
        if (string.IsNullOrWhiteSpace(match)) throw new ApiException(428, "if_match_required", "Yêu cầu If-Match.");
        if (!string.Equals(match, ETag(entity), StringComparison.Ordinal))
            throw new ApiException(412, "stale_etag", "Tài nguyên đã thay đổi; tải lại ETag.");
    }
}
public static class Audit
{
    public static void Add(MarsDbContext db, Guid? actor, string type, Guid id, AuditAction action, string note, string? traceId = null)
        => db.AuditLogs.Add(new AuditLog { ActorId = actor, EntityType = type, EntityId = id,
            Action = action, ChangeSummary = note, RequestId = traceId });
}
public static class QueryExtensions
{
    public static async Task<PageResult<T>> PageAsync<T>(this IQueryable<T> query, PageQuery page, CancellationToken ct)
    {
        Validator.ValidateObject(page, new ValidationContext(page), true);
        return new(await query.Skip(page.Skip).Take(page.PageSize).ToListAsync(ct),
            page.Page, page.PageSize, await query.CountAsync(ct));
    }
}
