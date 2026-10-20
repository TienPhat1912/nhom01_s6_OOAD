using System.ComponentModel.DataAnnotations;
using System.Data;
using System.Text.Json;
using Hospital.Application.Common.Exceptions;
using Hospital.Application.Common.Interfaces;
using Hospital.Application.Common.Models;
using Hospital.Domain.Entities;
using Hospital.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Hospital.Infrastructure.Services;

internal static class DatabaseWork
{
    // Every retry re-reads state; a failed attempt must not leave tracked mutations behind.
    public static Task<T> AtomicAsync<T>(this HospitalDbContext db, Func<Task<T>> operation) =>
        db.Database.CreateExecutionStrategy().ExecuteAsync(async () =>
        {
            db.ChangeTracker.Clear();
            await using var tx = db.Database.IsRelational()
                ? await db.Database.BeginTransactionAsync(IsolationLevel.Serializable) : null;
            try
            {
                var result = await operation();
                await db.SaveChangesAsync();
                if (tx != null) await tx.CommitAsync();
                return result;
            }
            catch
            {
                // Disposal rolls back an active transaction. An explicit rollback after a
                // SQL Server deadlock can mask the original exception and prevent retries.
                db.ChangeTracker.Clear();
                throw;
            }
        });

    public static void Validate(object value) =>
        Validator.ValidateObject(value, new ValidationContext(value), validateAllProperties: true);

    public static void Audit(this HospitalDbContext db, Guid actor, string action, string entity, object id,
        object? oldValue = null, object? newValue = null) => db.AuditLogs.Add(new AuditLog
        {
            UserId = actor, Action = action, EntityType = entity, EntityId = id.ToString(),
            OldValues = oldValue == null ? null : JsonSerializer.Serialize(oldValue),
            NewValues = newValue == null ? null : JsonSerializer.Serialize(newValue)
        });

    public static Guid RequireRole(this ICurrentUserService actor, params string[] roles)
    {
        if (!actor.IsAuthenticated || !actor.UserId.HasValue || !roles.Contains(actor.Role))
            throw new ForbiddenAccessException();
        return actor.UserId.Value;
    }

    public static async Task<PagedResult<T>> PageAsync<T>(this IQueryable<T> query, PageQuery page)
    {
        Validate(page);
        return new(await query.Skip((page.Page - 1) * page.PageSize).Take(page.PageSize).ToListAsync(),
            await query.CountAsync(), page.Page, page.PageSize);
    }

    public static void CheckVersion(byte[] actual, byte[] expected)
    {
        if (expected.Length == 0 || !actual.SequenceEqual(expected)) throw new ConcurrencyException();
    }
}
