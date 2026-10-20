using Hospital.Application.Common.Exceptions;
using Hospital.Application.DTOs.Admin;
using Hospital.Domain.Enums;
using Hospital.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;

namespace Hospital.IntegrationTests;

public class AdminAndScheduleTests
{
    [Fact]
    public async Task LastActiveAdminCannotBeDisabledOrDemoted()
    {
        using var f = new TestFixture(); using var db = f.CreateDb(); var service = f.Admin(db); var id = f.Actor("admin").UserId!.Value;
        Assert.Equal("LAST_ADMIN_PROTECTED", (await Assert.ThrowsAsync<BusinessRuleException>(() => service.UpdateUserAsync(id,
            new() { Email = "admin@hospital.com", FullName = "Admin", IsActive = false }))).ErrorCode);
        await Assert.ThrowsAsync<BusinessRuleException>(() => service.ChangeRoleAsync(id, new() { Role = UserRole.Staff }));
    }

    [Fact]
    public async Task AdminMayDisableAnotherAdminAndInvalidatesItsTokens()
    {
        using var f = new TestFixture(); using var db = f.CreateDb(); var service = f.Admin(db);
        var second = await service.CreateUserAsync(new()
        { Username = "admin2", Password = "Admin2@123", Email = "admin2@test.com", FullName = "Second admin", Role = UserRole.Admin });
        await service.UpdateUserAsync(second.Id, new() { Email = second.Email, FullName = second.FullName, IsActive = false });
        using var verify = f.CreateDb(); var user = await verify.Users.FindAsync(second.Id);
        Assert.False(user!.IsActive); Assert.Equal(1, user.TokenVersion);
    }

    [Fact]
    public async Task DoctorProfileExistsBeforeAccountAndSupportsMultipleSpecialties()
    {
        using var f = new TestFixture(); using var db = f.CreateDb(); var service = f.Admin(db);
        var specialty = await service.SaveSpecialtyAsync(null, new() { Code = "TIM", Name = "Tim mạch" });
        var doctor = await service.SaveDoctorAsync(null, new()
        { FullName = "Nguyễn Bác Sĩ", SpecialtyIds = [specialty.Id, f.Specialty()], PrimarySpecialtyId = specialty.Id });
        Assert.Equal("Nguyễn Bác Sĩ", doctor.FullName); Assert.Equal(2, doctor.Specialties.Count);
        var user = await service.CreateUserAsync(new()
        { Username = "newdoctor", Password = "Doctor@123", Email = "newdoctor@test.com", FullName = "Ignored name", Role = UserRole.Doctor, DoctorId = doctor.Id });
        Assert.Equal(doctor.Id, user.DoctorId); Assert.Equal(doctor.FullName, user.FullName);
        await Assert.ThrowsAsync<BusinessRuleException>(() => service.CreateUserAsync(new()
        { Username = "duplicate", Password = "Doctor@123", Email = "duplicate@test.com", FullName = "Duplicate", Role = UserRole.Doctor, DoctorId = doctor.Id }));
    }

    [Fact]
    public async Task RejectsDuplicateSpecialtyAndLoginIdentityAndDoctorWithoutProfile()
    {
        using var f = new TestFixture(); using var db = f.CreateDb(); var service = f.Admin(db);
        await Assert.ThrowsAsync<BusinessRuleException>(() => service.SaveSpecialtyAsync(null, new() { Code = "NOI", Name = "Other" }));
        await Assert.ThrowsAsync<BusinessRuleException>(() => service.SaveSpecialtyAsync(null, new() { Code = "NEW", Name = "Nội tổng quát" }));
        await Assert.ThrowsAsync<BusinessRuleException>(() => service.CreateUserAsync(new()
        { Username = "staff1", Password = "Staff@123", Email = "other@test.com", FullName = "Duplicate", Role = UserRole.Staff }));
        await Assert.ThrowsAsync<BusinessRuleException>(() => service.CreateUserAsync(new()
        { Username = "noprof", Password = "Doctor@123", Email = "noprof@test.com", FullName = "Missing", Role = UserRole.Doctor }));
    }

    [Fact]
    public async Task InactivationWithFutureAppointmentsIsBlockedThenKeepsHistoryAfterCancellation()
    {
        using var f = new TestFixture(); using var db = f.CreateDb(); var service = f.Admin(db);
        var doctorId = f.Actor("doctor1").DoctorId!.Value;
        var save = new SaveDoctorRequest { FullName = "Doctor one", SpecialtyIds = [f.Specialty()], PrimarySpecialtyId = f.Specialty(), IsActive = false };
        await Assert.ThrowsAsync<BusinessRuleException>(() => service.SaveDoctorAsync(doctorId, save));
        await Assert.ThrowsAsync<BusinessRuleException>(() => service.SaveSpecialtyAsync(f.Specialty(), new() { Code = "NOI", Name = "Nội tổng quát", IsActive = false }));
        var actor = f.Actor("staff1"); var appointments = f.Appointments(db, actor);
        foreach (var appointment in (await appointments.ListAsync(new() { DoctorId = doctorId })).Items)
            await appointments.CancelAsync(appointment.Id, new() { RowVersion = appointment.RowVersion, Reason = "Ngừng nhận lịch" });
        await service.SaveDoctorAsync(doctorId, save);
        await service.SaveSpecialtyAsync(f.Specialty(), new() { Code = "NOI", Name = "Nội tổng quát", IsActive = false });
        using var verify = f.CreateDb();
        Assert.False((await verify.Doctors.FindAsync(doctorId))!.IsActive);
        Assert.True(await verify.Appointments.AnyAsync(a => a.DoctorId == doctorId));
        Assert.False((await verify.Specialties.FindAsync(f.Specialty()))!.IsActive);
    }

    [Fact]
    public async Task SchedulesGenerateThirtyMinuteSlotsAndRejectOverlapsAndPastTimes()
    {
        using var f = new TestFixture(); using var db = f.CreateDb(); var actor = f.Actor("admin");
        var service = new ScheduleService(db, actor, f.Clock); var doctor = f.Actor("doctor1").DoctorId!.Value;
        var request = new Hospital.Application.DTOs.Schedules.CreateWorkingScheduleRequestDto
        { DoctorId = doctor, WorkDate = f.Clock.Today.AddDays(8), StartTime = new(8, 0), EndTime = new(10, 0) };
        var schedule = await service.CreateWorkingScheduleAsync(actor.UserId!.Value, request);
        Assert.Equal(4, schedule.TimeSlots.Count); Assert.All(schedule.TimeSlots, s => Assert.Equal(30, (s.EndTime - s.StartTime).TotalMinutes));
        request.StartTime = new(9, 0); request.EndTime = new(11, 0);
        Assert.Equal("SCHEDULE_OVERLAP", (await Assert.ThrowsAsync<BusinessRuleException>(() => service.CreateWorkingScheduleAsync(actor.UserId.Value, request))).ErrorCode);
        request.WorkDate = f.Clock.Today.AddDays(-1);
        await Assert.ThrowsAsync<BusinessRuleException>(() => service.CreateWorkingScheduleAsync(actor.UserId.Value, request));
        request.WorkDate = f.Clock.Today.AddDays(9); request.StartTime = new(23, 45); request.EndTime = new(23, 59);
        await Assert.ThrowsAsync<BusinessRuleException>(() => service.CreateWorkingScheduleAsync(actor.UserId.Value, request));
    }

    [Fact]
    public async Task CannotCloseOrChangeScheduleWithConfirmedAppointments()
    {
        using var f = new TestFixture(); using var db = f.CreateDb(); var actor = f.Actor("admin");
        var service = new ScheduleService(db, actor, f.Clock); var slot = f.Slot(hour: 8, minute: 30);
        await Assert.ThrowsAsync<BusinessRuleException>(() => service.CloseTimeSlotAsync(actor.UserId!.Value, slot.Id, new() { Reason = "Bác sĩ bận" }));
        await Assert.ThrowsAsync<BusinessRuleException>(() => service.UpdateWorkingScheduleAsync(actor.UserId!.Value, slot.WorkingScheduleId,
            new() { WorkDate = slot.SlotDate, StartTime = new(7, 30), EndTime = new(11, 30) }));
    }

    [Fact]
    public async Task ScheduleChangeRetainsCancelledSlotsAndTheirAppointmentHistory()
    {
        using var f = new TestFixture(); using var db = f.CreateDb(); var slot = f.Slot(hour: 8, minute: 30);
        var staff = f.Appointments(db, f.Actor("staff1"));
        var a = (await staff.ListAsync(new() { DoctorId = slot.DoctorId })).Items.Single();
        await staff.CancelAsync(a.Id, new() { RowVersion = a.RowVersion, Reason = "Đổi ca" });
        var actor = f.Actor("admin"); var service = new ScheduleService(db, actor, f.Clock);
        var updated = await service.UpdateWorkingScheduleAsync(actor.UserId!.Value, slot.WorkingScheduleId,
            new() { WorkDate = slot.SlotDate, StartTime = new(7, 30), EndTime = new(11, 30) });
        Assert.Equal(8, updated.OpenSlotsCount);
        using var verify = f.CreateDb(); var retained = await verify.TimeSlots.FindAsync(slot.Id);
        Assert.NotNull(retained); Assert.False(retained.IsActive);
        Assert.Equal(slot.Id, (await verify.Appointments.FindAsync(a.Id))!.TimeSlotId);
    }

    [Fact]
    public async Task DoctorAndPatientHaveRestrictedScheduleAccess()
    {
        using var f = new TestFixture(); using var db = f.CreateDb();
        var own = f.Actor("doctor1"); var service = new ScheduleService(db, own, f.Clock);
        var list = await service.GetWorkingSchedulesAsync(f.Actor("doctor2").DoctorId);
        Assert.All(list, s => Assert.Equal(own.DoctorId, s.DoctorId));
        await Assert.ThrowsAsync<NotFoundException>(() => service.GetWorkingScheduleByIdAsync(f.Slot("doctor2", hour: 14).WorkingScheduleId));
        await Assert.ThrowsAsync<ForbiddenAccessException>(() => new ScheduleService(db, f.Actor("patient1"), f.Clock).GetWorkingSchedulesAsync());
    }

    [Fact]
    public async Task CatalogueFiltersInactiveDataAndUsesPaginationAndClinicTime()
    {
        using var f = new TestFixture(); using var db = f.CreateDb(); var actor = f.Actor("patient1");
        var catalogue = new CatalogService(db, actor, f.Clock);
        var result = await catalogue.GetSpecialtiesAsync(page: new() { Page = 2, PageSize = 2 });
        Assert.Equal(5, result.TotalCount); Assert.Equal(2, result.Items.Count);
        await Assert.ThrowsAsync<ForbiddenAccessException>(() => catalogue.GetDoctorsAsync(onlyActive: false));
        var doctorId = f.Actor("doctor1").DoctorId!.Value;
        f.Provider.Now = new DateTimeOffset(f.Clock.ToUtc(f.Clock.Today, new TimeOnly(12, 0)));
        Assert.DoesNotContain(f.Clock.Today, (await catalogue.GetDoctorByIdAsync(doctorId)).AvailableDates);
    }
}
