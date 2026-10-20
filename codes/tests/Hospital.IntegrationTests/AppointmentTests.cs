using Hospital.Application.Common.Exceptions;
using Hospital.Application.DTOs.Appointments;
using Hospital.Domain.Entities;
using Hospital.Domain.Enums;
using Hospital.Infrastructure.Data;
using Hospital.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Hospital.IntegrationTests;

public class AppointmentTests
{
    private static BookAppointmentRequest Request(TestFixture f, Guid? slot = null) => new()
    { TimeSlotId = slot ?? f.Slot().Id, SpecialtyId = f.Specialty(), RequestKey = Guid.NewGuid() };

    [Fact]
    public async Task BookingIsAtomicAndIdempotentWithHistoryAuditAndNotification()
    {
        using var f = new TestFixture(); using var db = f.CreateDb();
        var service = f.Appointments(db, f.Actor("patient1")); var request = Request(f);
        var a = await service.BookAsync(request); var repeat = await service.BookAsync(request);
        Assert.Equal(a.Id, repeat.Id); Assert.Equal(AppointmentStatus.Confirmed, a.Status);
        Assert.Single(a.History); Assert.NotEmpty(a.RowVersion);
        using var verify = f.CreateDb();
        Assert.Equal(TimeSlotStatus.Occupied, (await verify.TimeSlots.FindAsync(request.TimeSlotId))!.Status);
        Assert.Single(await verify.Appointments.Where(x => x.RequestKey == request.RequestKey).ToListAsync());
        Assert.Single(await verify.Notifications.Where(n => n.AppointmentId == a.Id).ToListAsync());
        Assert.Contains(await verify.AuditLogs.ToListAsync(), l => l.EntityId == a.Id.ToString() && l.Action == "BOOK_APPOINTMENT");
        request.TimeSlotId = f.Slot(hour: 10).Id;
        var error = await Assert.ThrowsAsync<BusinessRuleException>(() => service.BookAsync(request));
        Assert.Equal("IDEMPOTENCY_KEY_REUSED", error.ErrorCode);
    }

    [Fact]
    public async Task RejectsOccupiedSlotAndPatientOverlapButAllowsAdjacentSlots()
    {
        using var f = new TestFixture(); using var db = f.CreateDb();
        var service = f.Appointments(db, f.Actor("patient1"));
        await service.BookAsync(Request(f));
        using var otherDb = f.CreateDb();
        await Assert.ThrowsAsync<BusinessRuleException>(() => f.Appointments(otherDb, f.Actor("patient2")).BookAsync(Request(f)));
        var overlap = await f.ExtraSlotAsync();
        var req = Request(f, overlap.Id); req.SpecialtyId = f.Specialty("NHI");
        var error = await Assert.ThrowsAsync<BusinessRuleException>(() => service.BookAsync(req));
        Assert.Equal("PATIENT_SCHEDULE_OVERLAP", error.ErrorCode);
        await service.BookAsync(Request(f, f.Slot(hour: 9, minute: 30).Id));
    }

    [Fact]
    public async Task PatientCannotBookForOrReadOrModifyAnotherPatient()
    {
        using var f = new TestFixture(); using var db = f.CreateDb();
        var actor = f.Actor("patient1"); var service = f.Appointments(db, actor); var req = Request(f);
        req.PatientId = f.Actor("patient2").PatientId;
        await Assert.ThrowsAsync<ForbiddenAccessException>(() => service.BookAsync(req));
        using var other = f.CreateDb(); var a = await f.Appointments(other, f.Actor("patient2")).BookAsync(Request(f));
        await Assert.ThrowsAsync<NotFoundException>(() => service.GetAsync(a.Id));
        await Assert.ThrowsAsync<NotFoundException>(() => service.CancelAsync(a.Id, new() { RowVersion = a.RowVersion }));
        Assert.DoesNotContain((await service.ListAsync(new())).Items, x => x.Id == a.Id);
    }

    [Theory]
    [InlineData(1.99, false)]
    [InlineData(2.0, true)]
    public async Task PatientRescheduleEnforcesExactTwoHourBoundary(double hours, bool allowed)
    {
        using var f = new TestFixture(); using var db = f.CreateDb(); var service = f.Appointments(db, f.Actor("patient1"));
        var a = await service.BookAsync(Request(f));
        f.Provider.Now = new DateTimeOffset(f.Clock.ToUtc(a.AppointmentDate, a.StartTime).AddHours(-hours));
        var request = new RescheduleAppointmentRequest { RowVersion = a.RowVersion, TimeSlotId = f.Slot(day: 2).Id, SpecialtyId = f.Specialty() };
        if (allowed) Assert.Equal(request.TimeSlotId, (await service.RescheduleAsync(a.Id, request)).TimeSlotId);
        else Assert.Equal("CHANGE_DEADLINE_PASSED", (await Assert.ThrowsAsync<BusinessRuleException>(() => service.RescheduleAsync(a.Id, request))).ErrorCode);
    }

    [Fact]
    public async Task FailedRescheduleKeepsOldAppointmentSlotAndHistory()
    {
        using var f = new TestFixture(); using var db = f.CreateDb(); var service = f.Appointments(db, f.Actor("patient1"));
        var a = await service.BookAsync(Request(f));
        using var other = f.CreateDb(); var occupied = await f.Appointments(other, f.Actor("patient2"))
            .BookAsync(Request(f, f.Slot(hour: 10).Id));
        await Assert.ThrowsAsync<BusinessRuleException>(() => service.RescheduleAsync(a.Id,
            new() { TimeSlotId = occupied.TimeSlotId, SpecialtyId = a.SpecialtyId, RowVersion = a.RowVersion }));
        var unchanged = await service.GetAsync(a.Id);
        Assert.Equal(a.TimeSlotId, unchanged.TimeSlotId); Assert.Single(unchanged.History);
        using var verify = f.CreateDb(); Assert.Equal(TimeSlotStatus.Occupied, (await verify.TimeSlots.FindAsync(a.TimeSlotId))!.Status);
    }

    [Fact]
    public async Task CancellationReleasesSlotAndPreservesHistoryForRebooking()
    {
        using var f = new TestFixture(); using var db = f.CreateDb(); var service = f.Appointments(db, f.Actor("patient1"));
        var req = Request(f); var a = await service.BookAsync(req);
        var cancel = new AppointmentMutationRequest { RowVersion = a.RowVersion, Reason = "Bận công việc" };
        var cancelled = await service.CancelAsync(a.Id, cancel); await service.CancelAsync(a.Id, cancel);
        Assert.Equal(AppointmentStatus.Cancelled, cancelled.Status); Assert.Equal(2, cancelled.History.Count);
        using var other = f.CreateDb();
        var next = await f.Appointments(other, f.Actor("patient2")).BookAsync(Request(f, a.TimeSlotId));
        Assert.NotEqual(a.Id, next.Id);
        using var verify = f.CreateDb(); Assert.Equal(2, await verify.Appointments.CountAsync(x => x.TimeSlotId == a.TimeSlotId));
        Assert.Equal(2, await verify.Notifications.CountAsync(n => n.AppointmentId == a.Id));
        Assert.DoesNotContain(await verify.NotificationOutbox.ToListAsync(), n => n.AppointmentId == a.Id
            && n.Type == NotificationType.Reminder && n.SuppressedAt == null && n.DeliveredAt == null);
    }

    [Fact]
    public async Task StaffNeedsReasonButMayChangeInsidePatientDeadline()
    {
        using var f = new TestFixture(); using var db = f.CreateDb();
        var patient = f.Appointments(db, f.Actor("patient1")); var a = await patient.BookAsync(Request(f));
        f.Provider.Now = new DateTimeOffset(f.Clock.ToUtc(a.AppointmentDate, a.StartTime).AddMinutes(-30));
        using var staffDb = f.CreateDb(); var staff = f.Appointments(staffDb, f.Actor("staff1"));
        await Assert.ThrowsAsync<BusinessRuleException>(() => staff.CancelAsync(a.Id, new() { RowVersion = a.RowVersion }));
        Assert.Equal(AppointmentStatus.Cancelled, (await staff.CancelAsync(a.Id, new() { RowVersion = a.RowVersion, Reason = "Bệnh nhân yêu cầu" })).Status);
    }

    [Fact]
    public async Task StaleVersionCannotOverwriteNewSchedule()
    {
        using var f = new TestFixture(); using var db = f.CreateDb(); var service = f.Appointments(db, f.Actor("patient1"));
        var a = await service.BookAsync(Request(f));
        await service.RescheduleAsync(a.Id, new() { RowVersion = a.RowVersion, TimeSlotId = f.Slot(hour: 10).Id, SpecialtyId = a.SpecialtyId });
        await Assert.ThrowsAsync<ConcurrencyException>(() => service.CancelAsync(a.Id, new() { RowVersion = a.RowVersion }));
    }

    [Fact]
    public async Task ReceptionOnlyAcceptsValidSameDayAppointmentAndIsIdempotent()
    {
        using var f = new TestFixture(); using var db = f.CreateDb(); var a = await f.Appointments(db, f.Actor("patient1")).BookAsync(Request(f));
        var service = f.Appointments(db, f.Actor("staff1"));
        await Assert.ThrowsAsync<BusinessRuleException>(() => service.CheckInAsync(a.Id, new() { RowVersion = a.RowVersion }));
        f.Provider.Now = new DateTimeOffset(f.Clock.ToUtc(a.AppointmentDate, a.StartTime).AddMinutes(-10));
        var checkedIn = await service.CheckInAsync(a.Id, new() { RowVersion = a.RowVersion });
        Assert.Equal(AppointmentStatus.CheckedIn, checkedIn.Status); Assert.NotNull(checkedIn.CheckedInAt);
        var repeat = await service.CheckInAsync(a.Id, new() { RowVersion = a.RowVersion });
        Assert.Equal(checkedIn.History.Count, repeat.History.Count);
        await Assert.ThrowsAsync<BusinessRuleException>(() => service.NoShowAsync(a.Id, new() { RowVersion = checkedIn.RowVersion }));
    }

    [Fact]
    public async Task NoShowRequiresEndTimeStrictlyPassed()
    {
        using var f = new TestFixture(); using var db = f.CreateDb(); var a = await f.Appointments(db, f.Actor("patient1")).BookAsync(Request(f));
        var staff = f.Appointments(db, f.Actor("staff1"));
        f.Provider.Now = new DateTimeOffset(f.Clock.ToUtc(a.AppointmentDate, a.EndTime));
        await Assert.ThrowsAsync<BusinessRuleException>(() => staff.NoShowAsync(a.Id, new() { RowVersion = a.RowVersion }));
        f.Provider.Now = f.Provider.Now.AddSeconds(1);
        Assert.Equal(AppointmentStatus.NoShow, (await staff.NoShowAsync(a.Id, new() { RowVersion = a.RowVersion })).Status);
    }

    [Fact]
    public async Task SaveFailureRollsBackSlotAppointmentHistoryAndOutbox()
    {
        using var f = new TestFixture(); var req = Request(f);
        using var before = f.CreateDb(); var outboxCount = await before.NotificationOutbox.CountAsync();
        using var broken = f.CreateDb(new FailSave());
        await Assert.ThrowsAsync<IOException>(() => f.Appointments(broken, f.Actor("patient1")).BookAsync(req));
        using var verify = f.CreateDb();
        Assert.False(await verify.Appointments.AnyAsync(a => a.RequestKey == req.RequestKey));
        Assert.Equal(TimeSlotStatus.Open, (await verify.TimeSlots.FindAsync(req.TimeSlotId))!.Status);
        Assert.Equal(outboxCount, await verify.NotificationOutbox.CountAsync());
    }

    [Fact]
    public async Task NotificationFailureDoesNotUndoBookingAndOutboxRetriesOnce()
    {
        using var f = new TestFixture(); using var broken = f.CreateDb(new FailSave(notificationsOnly: true));
        var a = await f.Appointments(broken, f.Actor("patient1")).BookAsync(Request(f));
        using var db = f.CreateDb(); Assert.True(await db.Appointments.AnyAsync(x => x.Id == a.Id));
        Assert.False(await db.Notifications.AnyAsync(n => n.AppointmentId == a.Id));
        var dispatcher = new NotificationDispatcher(db, f.Clock); await dispatcher.DispatchAsync(); await dispatcher.DispatchAsync();
        Assert.Single(await db.Notifications.Where(n => n.AppointmentId == a.Id).ToListAsync());
    }

    [Fact]
    public async Task RescheduleSaveFailureRollsBackBothSlotsAndHistory()
    {
        using var f = new TestFixture(); using var db = f.CreateDb();
        var a = await f.Appointments(db, f.Actor("patient1")).BookAsync(Request(f));
        var newSlot = f.Slot(day: 2);
        using var broken = f.CreateDb(new FailSave());
        await Assert.ThrowsAsync<IOException>(() => f.Appointments(broken, f.Actor("patient1")).RescheduleAsync(a.Id,
            new() { TimeSlotId = newSlot.Id, SpecialtyId = a.SpecialtyId, RowVersion = a.RowVersion }));
        using var verify = f.CreateDb();
        var unchanged = await f.Appointments(verify, f.Actor("patient1")).GetAsync(a.Id);
        Assert.Equal(a.TimeSlotId, unchanged.TimeSlotId); Assert.Single(unchanged.History);
        Assert.Equal(TimeSlotStatus.Occupied, (await verify.TimeSlots.FindAsync(a.TimeSlotId))!.Status);
        Assert.Equal(TimeSlotStatus.Open, (await verify.TimeSlots.FindAsync(newSlot.Id))!.Status);
    }

    [Fact]
    public async Task CancelledAppointmentCannotBeReceivedOrMarkedNoShow()
    {
        using var f = new TestFixture(); using var db = f.CreateDb(); var patient = f.Appointments(db, f.Actor("patient1"));
        var a = await patient.BookAsync(Request(f));
        var cancelled = await patient.CancelAsync(a.Id, new() { RowVersion = a.RowVersion });
        f.Provider.Now = new DateTimeOffset(f.Clock.ToUtc(a.AppointmentDate, a.StartTime));
        var staff = f.Appointments(db, f.Actor("staff1"));
        Assert.Equal("INVALID_APPOINTMENT_STATUS", (await Assert.ThrowsAsync<BusinessRuleException>(() =>
            staff.CheckInAsync(a.Id, new() { RowVersion = cancelled.RowVersion }))).ErrorCode);
        await Assert.ThrowsAsync<BusinessRuleException>(() => staff.NoShowAsync(a.Id, new() { RowVersion = cancelled.RowVersion }));
    }

    [Theory]
    [InlineData("doctor1")]
    [InlineData("admin")]
    public async Task NonBookingRolesCannotBook(string username)
    {
        using var f = new TestFixture(); using var db = f.CreateDb();
        await Assert.ThrowsAsync<ForbiddenAccessException>(() => f.Appointments(db, f.Actor(username)).BookAsync(Request(f)));
    }

    [Fact]
    public async Task PastClosedAndInactiveDoctorSlotsAreRejected()
    {
        using var f = new TestFixture(); using var db = f.CreateDb(); var service = f.Appointments(db, f.Actor("patient1"));
        var slot = f.Slot(day: 0); f.Provider.Now = new DateTimeOffset(f.Clock.ToUtc(slot.SlotDate, slot.StartTime));
        await Assert.ThrowsAsync<BusinessRuleException>(() => service.BookAsync(Request(f, slot.Id)));
        var tomorrow = await db.TimeSlots.FindAsync(f.Slot().Id); tomorrow!.Status = TimeSlotStatus.Closed; await db.SaveChangesAsync();
        await Assert.ThrowsAsync<BusinessRuleException>(() => service.BookAsync(Request(f, tomorrow.Id)));
        var doctor = await db.Doctors.FindAsync(f.Actor("doctor1").DoctorId); doctor!.IsActive = false; await db.SaveChangesAsync();
        await Assert.ThrowsAsync<BusinessRuleException>(() => service.BookAsync(Request(f, f.Slot(hour: 10).Id)));
    }
}

public sealed class FailSave(bool notificationsOnly = false) : SaveChangesInterceptor
{
    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData data,
        InterceptionResult<int> result, CancellationToken cancellationToken = default)
    {
        if (!notificationsOnly || data.Context!.ChangeTracker.Entries<Notification>().Any(e => e.State == EntityState.Added))
            throw new IOException("Injected database save failure");
        return base.SavingChangesAsync(data, result, cancellationToken);
    }
}
