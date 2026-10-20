using Hospital.Application.Common.Exceptions;
using Hospital.Application.Common.Interfaces;
using Hospital.Application.Common.Models;
using Hospital.Application.Services.Interfaces;
using Hospital.Domain.Entities;
using Hospital.Domain.Enums;
using Hospital.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Hospital.Infrastructure.Services;

public sealed class NotificationDispatcher(HospitalDbContext db, ClinicTime clock)
{
    public async Task DispatchAsync()
    {
        var now = clock.UtcNow;
        var ids = await db.NotificationOutbox.AsNoTracking().Where(n => n.DeliveredAt == null
            && n.SuppressedAt == null && n.ScheduledFor <= now).OrderBy(n => n.ScheduledFor)
            .Select(n => n.Id).Take(100).ToListAsync();
        foreach (var id in ids)
        {
            await db.AtomicAsync(async () =>
            {
                var item = await db.NotificationOutbox.SingleAsync(n => n.Id == id);
                if (item.DeliveredAt != null || item.SuppressedAt != null) return false;
                if (item.Type == NotificationType.Reminder)
                {
                    var a = await db.Appointments.SingleAsync(a => a.Id == item.AppointmentId);
                    var start = clock.ToUtc(a.AppointmentDate, a.StartTime);
                    if (a.Status != AppointmentStatus.Confirmed || start <= clock.UtcNow || start.AddHours(-1) != item.ScheduledFor)
                    {
                        item.SuppressedAt = clock.UtcNow;
                        return false;
                    }
                }
                if (!await db.Notifications.AnyAsync(n => n.OutboxId == item.Id))
                    db.Notifications.Add(new Notification
                    {
                        Id = Guid.NewGuid(), OutboxId = item.Id, PatientId = item.PatientId,
                        AppointmentId = item.AppointmentId, Type = item.Type, Title = item.Title,
                        Message = item.Message, IsSent = true, CreatedAt = clock.UtcNow,
                        ScheduledFor = item.ScheduledFor
                    });
                item.DeliveredAt = clock.UtcNow;
                return true;
            });
        }
    }
}

public sealed class NotificationService(HospitalDbContext db, ICurrentUserService actor, ClinicTime clock) : INotificationService
{
    public Task<PagedResult<NotificationDto>> ListAsync(PageQuery page, bool unreadOnly)
    {
        actor.RequireRole("Patient");
        if (!actor.PatientId.HasValue) throw new ForbiddenAccessException();
        return db.Notifications.AsNoTracking().Where(n => n.PatientId == actor.PatientId && n.IsSent
                && (!unreadOnly || !n.IsRead))
            .OrderByDescending(n => n.CreatedAt).ThenBy(n => n.Id)
            .Select(n => new NotificationDto(n.Id, n.AppointmentId, n.Type, n.Title, n.Message, n.IsRead, n.CreatedAt))
            .PageAsync(page);
    }

    public async Task ReadAsync(Guid id)
    {
        actor.RequireRole("Patient");
        var notification = await db.Notifications.SingleOrDefaultAsync(n => n.Id == id && n.PatientId == actor.PatientId && n.IsSent)
            ?? throw new NotFoundException("Thông báo", id);
        if (notification.IsRead) return;
        notification.IsRead = true;
        notification.ReadAt = clock.UtcNow;
        await db.SaveChangesAsync();
    }
}
