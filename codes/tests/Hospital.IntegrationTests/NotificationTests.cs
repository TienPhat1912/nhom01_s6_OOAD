using Hospital.Application.Common.Exceptions;
using Hospital.Application.DTOs.Appointments;
using Hospital.Domain.Enums;
using Hospital.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;

namespace Hospital.IntegrationTests;

public class NotificationTests
{
    [Fact]
    public async Task ReminderAppearsAtOneHourAndIsNeverDuplicated()
    {
        using var f = new TestFixture(); using var db = f.CreateDb(); var actor = f.Actor("patient1");
        var a = await f.Appointments(db, actor).BookAsync(new()
        { TimeSlotId = f.Slot().Id, SpecialtyId = f.Specialty(), RequestKey = Guid.NewGuid() });
        var dispatcher = new NotificationDispatcher(db, f.Clock);
        f.Provider.Now = new DateTimeOffset(f.Clock.ToUtc(a.AppointmentDate, a.StartTime).AddHours(-1).AddSeconds(-1));
        await dispatcher.DispatchAsync(); Assert.False(await db.Notifications.AnyAsync(n => n.AppointmentId == a.Id && n.Type == NotificationType.Reminder));
        f.Provider.Now = f.Provider.Now.AddSeconds(1);
        await dispatcher.DispatchAsync(); await dispatcher.DispatchAsync();
        Assert.Single(await db.Notifications.Where(n => n.AppointmentId == a.Id && n.Type == NotificationType.Reminder).ToListAsync());
    }

    [Fact]
    public async Task LastMinuteBookingUsesConfirmationWithoutAdditionalReminder()
    {
        using var f = new TestFixture(); var slot = f.Slot();
        f.Provider.Now = new DateTimeOffset(f.Clock.ToUtc(slot.SlotDate, slot.StartTime).AddMinutes(-45));
        using var db = f.CreateDb(); var a = await f.Appointments(db, f.Actor("patient2")).BookAsync(new()
        { TimeSlotId = slot.Id, SpecialtyId = f.Specialty(), RequestKey = Guid.NewGuid() });
        Assert.False(await db.NotificationOutbox.AnyAsync(n => n.AppointmentId == a.Id && n.Type == NotificationType.Reminder));
        Assert.Single(await db.Notifications.Where(n => n.AppointmentId == a.Id).ToListAsync());
    }

    [Fact]
    public async Task RescheduleSuppressesOldReminderAndKeepsEventHistory()
    {
        using var f = new TestFixture(); using var db = f.CreateDb(); var service = f.Appointments(db, f.Actor("patient1"));
        var a = await service.BookAsync(new() { TimeSlotId = f.Slot().Id, SpecialtyId = f.Specialty(), RequestKey = Guid.NewGuid() });
        var moved = await service.RescheduleAsync(a.Id, new()
        { RowVersion = a.RowVersion, TimeSlotId = f.Slot(day: 2).Id, SpecialtyId = a.SpecialtyId });
        f.Provider.Now = new DateTimeOffset(f.Clock.ToUtc(a.AppointmentDate, a.StartTime).AddHours(-1));
        await new NotificationDispatcher(db, f.Clock).DispatchAsync();
        Assert.False(await db.Notifications.AnyAsync(n => n.AppointmentId == a.Id && n.Type == NotificationType.Reminder));
        Assert.Equal(2, await db.Notifications.CountAsync(n => n.AppointmentId == a.Id));
        f.Provider.Now = new DateTimeOffset(f.Clock.ToUtc(moved.AppointmentDate, moved.StartTime).AddHours(-1));
        await new NotificationDispatcher(db, f.Clock).DispatchAsync();
        Assert.Equal(3, await db.Notifications.CountAsync(n => n.AppointmentId == a.Id));
    }

    [Fact]
    public async Task PatientMayOnlyReadOwnNotificationsAndReadingDoesNotChangeAppointment()
    {
        using var f = new TestFixture(); using var db = f.CreateDb(); var actor = f.Actor("patient1");
        var a = await f.Appointments(db, actor).BookAsync(new()
        { TimeSlotId = f.Slot().Id, SpecialtyId = f.Specialty(), RequestKey = Guid.NewGuid() });
        var service = new NotificationService(db, actor, f.Clock);
        var notification = (await service.ListAsync(new(), false)).Items.Single(n => n.AppointmentId == a.Id);
        await Assert.ThrowsAsync<NotFoundException>(() => new NotificationService(db, f.Actor("patient2"), f.Clock).ReadAsync(notification.Id));
        await service.ReadAsync(notification.Id); await service.ReadAsync(notification.Id);
        Assert.DoesNotContain((await service.ListAsync(new(), true)).Items, n => n.Id == notification.Id);
        Assert.Equal(AppointmentStatus.Confirmed, (await db.Appointments.FindAsync(a.Id))!.Status);
    }
}
