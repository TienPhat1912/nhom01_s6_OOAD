using System.Net;
using System.Net.Http.Json;
using Hospital.Application.Common.Models;
using Hospital.Application.DTOs.Admin;
using Hospital.Application.DTOs.Appointments;
using Hospital.Application.DTOs.Auth;
using Hospital.Application.DTOs.Catalog;
using Hospital.Domain.Enums;
using Hospital.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;

namespace Hospital.IntegrationTests;

public sealed class SqlServerTheoryAttribute : TheoryAttribute
{
    public SqlServerTheoryAttribute()
    {
        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("HOSPITAL_TEST_SQLSERVER")))
            Skip = "Requires HOSPITAL_TEST_SQLSERVER with permission to create isolated test databases.";
    }
}

// Each required scenario gets its own migrated SQL Server database and actual HTTP API host.
public class Phase7AcceptanceTests
{
    [SqlServerTheory]
    [InlineData(1, "duplicate username and email")]
    [InlineData(2, "wrong password and locked account")]
    [InlineData(3, "role authorization")]
    [InlineData(4, "other patient's appointment")]
    [InlineData(5, "occupied slot")]
    [InlineData(6, "concurrent patients, one winner")]
    [InlineData(7, "overlapping patient appointments")]
    [InlineData(8, "reschedule inside two hours")]
    [InlineData(9, "failed reschedule preserves original")]
    [InlineData(10, "cancellation releases slot")]
    [InlineData(11, "invalid reception")]
    [InlineData(12, "early no-show")]
    [InlineData(13, "closing booked slot")]
    [InlineData(14, "deactivation preserves history")]
    [InlineData(15, "last active administrator")]
    [InlineData(16, "repeat successful booking")]
    [InlineData(17, "database outage and recovery")]
    [InlineData(18, "notifications after successful mutations")]
    public async Task RequiredScenario(int scenario, string description)
    {
        Assert.NotEmpty(description);
        if (scenario == 17)
        {
            await new SqlServerBackendTests().UnavailableSqlDatabaseReturns503AndRecoversWithoutPartialBooking();
            return;
        }
        using var f = new TestFixture(Environment.GetEnvironmentVariable("HOSPITAL_TEST_SQLSERVER")!);
        using var patient = await f.ClientAsync("patient1"); using var other = await f.ClientAsync("patient2");
        using var admin = await f.ClientAsync("admin"); using var staff = await f.ClientAsync("staff1");
        using var db = f.CreateDb();
        var request = ConcurrencyTests.Request(f);
        switch (scenario)
        {
            case 1:
                using (var guest = await f.ClientAsync())
                {
                    foreach (var (username, email) in new[] { ("patient1", "new@example.test"), ("unique", "patient1@gmail.com") })
                    {
                        using var response = await guest.PostAsJsonAsync("/api/auth/register", new RegisterRequestDto
                        { Username = username, Email = email, Password = "Patient@123", FullName = "Duplicate", DateOfBirth = new(1990, 1, 1) });
                        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
                    }
                }
                Assert.Equal(6, await db.Users.CountAsync()); break;
            case 2:
                using (var guest = await f.ClientAsync())
                {
                    using var wrong = await guest.PostAsJsonAsync("/api/auth/login", new LoginRequestDto { Username = "patient1", Password = "Wrong@123" });
                    Assert.Equal(HttpStatusCode.Unauthorized, wrong.StatusCode);
                    var user = await Data<UserDto>(await admin.GetAsync($"/api/admin/users/{f.Actor("patient1").UserId}"));
                    (await admin.PutAsJsonAsync($"/api/admin/users/{user.Id}", new UpdateUserRequest { FullName = user.FullName, Email = user.Email, IsActive = false })).EnsureSuccessStatusCode();
                    using var locked = await guest.PostAsJsonAsync("/api/auth/login", new LoginRequestDto { Username = "patient1", Password = "Patient@123" });
                    Assert.Equal(HttpStatusCode.Unauthorized, locked.StatusCode);
                    Assert.Equal(HttpStatusCode.Unauthorized, (await patient.GetAsync("/api/auth/profile")).StatusCode);
                    Assert.False((await db.Users.FindAsync(user.Id))!.IsActive);
                }
                break;
            case 3:
                Assert.Equal(HttpStatusCode.Forbidden, (await patient.GetAsync("/api/admin/users")).StatusCode);
                Assert.Equal(HttpStatusCode.Forbidden, (await staff.GetAsync("/api/admin/audit-logs")).StatusCode);
                Assert.Equal(HttpStatusCode.Forbidden, (await admin.PostAsJsonAsync("/api/appointments", request)).StatusCode);
                Assert.Equal(2, await db.Appointments.CountAsync()); break;
            case 4:
                var owned = await Book(patient, request);
                Assert.Equal(HttpStatusCode.NotFound, (await other.GetAsync($"/api/appointments/{owned.Id}")).StatusCode);
                Assert.Equal(HttpStatusCode.NotFound, (await other.PostAsJsonAsync($"/api/appointments/{owned.Id}/cancel", Mutation(owned))).StatusCode);
                Assert.Equal(HttpStatusCode.NotFound, (await other.PostAsJsonAsync($"/api/appointments/{owned.Id}/reschedule", Move(owned, f.Slot(hour: 10).Id))).StatusCode);
                Assert.Equal(owned.TimeSlotId, (await db.Appointments.FindAsync(owned.Id))!.TimeSlotId);
                Assert.Equal(1, await db.AppointmentStatusHistories.CountAsync(h => h.AppointmentId == owned.Id)); break;
            case 5:
                var booked = await Book(patient, request);
                await Rejected(await other.PostAsJsonAsync("/api/appointments", new BookAppointmentRequest { TimeSlotId = request.TimeSlotId, SpecialtyId = request.SpecialtyId, RequestKey = Guid.NewGuid() }));
                Assert.Equal(1, await db.Appointments.CountAsync(a => a.TimeSlotId == booked.TimeSlotId)); break;
            case 6:
                var second = ConcurrencyTests.Request(f);
                var gate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
                async Task<HttpResponseMessage> Attempt(HttpClient client, BookAppointmentRequest body) { await gate.Task; return await client.PostAsJsonAsync("/api/appointments", body); }
                var firstTask = Attempt(patient, request); var secondTask = Attempt(other, second); gate.SetResult();
                var results = await Task.WhenAll(firstTask, secondTask);
                try
                {
                    Assert.Single(results, r => r.StatusCode == HttpStatusCode.Created);
                    await Rejected(results.Single(r => !r.IsSuccessStatusCode));
                    Assert.Equal(1, await db.Appointments.CountAsync(a => a.TimeSlotId == request.TimeSlotId && a.Status == AppointmentStatus.Confirmed));
                    Assert.Equal(TimeSlotStatus.Occupied, (await db.TimeSlots.FindAsync(request.TimeSlotId))!.Status);
                }
                finally { foreach (var result in results) result.Dispose(); }
                break;
            case 7:
                var first = await Book(patient, request); var overlap = await f.ExtraSlotAsync();
                await Rejected(await patient.PostAsJsonAsync("/api/appointments", new BookAppointmentRequest { TimeSlotId = overlap.Id, SpecialtyId = f.Specialty("NHI"), RequestKey = Guid.NewGuid() }));
                Assert.False(await db.Appointments.AnyAsync(a => a.TimeSlotId == overlap.Id));
                Assert.Equal(TimeSlotStatus.Open, (await db.TimeSlots.FindAsync(overlap.Id))!.Status); break;
            case 8:
                var late = await Book(patient, request);
                f.Provider.Now = new(f.Clock.ToUtc(late.AppointmentDate, late.StartTime).AddMinutes(-119));
                await Rejected(await patient.PostAsJsonAsync($"/api/appointments/{late.Id}/reschedule", Move(late, f.Slot(hour: 10, day: 0).Id)));
                Assert.Equal(late.TimeSlotId, (await db.Appointments.FindAsync(late.Id))!.TimeSlotId); break;
            case 9:
                var original = await Book(patient, request); var target = f.Slot(hour: 10);
                await Book(other, new() { TimeSlotId = target.Id, SpecialtyId = original.SpecialtyId, RequestKey = Guid.NewGuid() });
                await Rejected(await patient.PostAsJsonAsync($"/api/appointments/{original.Id}/reschedule", Move(original, target.Id)));
                var preserved = (await db.Appointments.FindAsync(original.Id))!;
                Assert.Equal(original.TimeSlotId, preserved.TimeSlotId); Assert.Equal(original.RowVersion, preserved.RowVersion);
                Assert.Equal(1, await db.AppointmentStatusHistories.CountAsync(h => h.AppointmentId == original.Id));
                Assert.Equal(TimeSlotStatus.Occupied, (await db.TimeSlots.FindAsync(original.TimeSlotId))!.Status);
                Assert.Equal(TimeSlotStatus.Occupied, (await db.TimeSlots.FindAsync(target.Id))!.Status); break;
            case 10:
                var cancellation = await Book(patient, request);
                var cancelled = await Data<AppointmentDto>(await patient.PostAsJsonAsync($"/api/appointments/{cancellation.Id}/cancel", Mutation(cancellation)));
                Assert.Equal(AppointmentStatus.Cancelled, cancelled.Status);
                Assert.Equal(TimeSlotStatus.Open, (await db.TimeSlots.FindAsync(cancellation.TimeSlotId))!.Status);
                Assert.Equal(2, await db.AppointmentStatusHistories.CountAsync(h => h.AppointmentId == cancellation.Id));
                var replacement = await Book(other, new() { TimeSlotId = request.TimeSlotId, SpecialtyId = request.SpecialtyId, RequestKey = Guid.NewGuid() });
                Assert.NotEqual(cancelled.Id, replacement.Id); break;
            case 11:
                var reception = await Book(patient, request);
                var inactive = await Data<AppointmentDto>(await patient.PostAsJsonAsync($"/api/appointments/{reception.Id}/cancel", Mutation(reception)));
                await Rejected(await staff.PostAsJsonAsync($"/api/appointments/{inactive.Id}/check-in", Mutation(inactive)));
                var expired = await Book(other, new() { TimeSlotId = request.TimeSlotId, SpecialtyId = request.SpecialtyId, RequestKey = Guid.NewGuid() });
                f.Provider.Now = new(f.Clock.ToUtc(expired.AppointmentDate, expired.EndTime).AddSeconds(1));
                await Rejected(await staff.PostAsJsonAsync($"/api/appointments/{expired.Id}/check-in", Mutation(expired)));
                Assert.Equal(AppointmentStatus.Confirmed, (await db.Appointments.FindAsync(expired.Id))!.Status); break;
            case 12:
                var attendance = await Book(patient, request);
                f.Provider.Now = new(f.Clock.ToUtc(attendance.AppointmentDate, attendance.EndTime));
                await Rejected(await staff.PostAsJsonAsync($"/api/appointments/{attendance.Id}/no-show", Mutation(attendance)));
                f.Provider.Now = f.Provider.Now.AddSeconds(1);
                var absent = await Data<AppointmentDto>(await staff.PostAsJsonAsync($"/api/appointments/{attendance.Id}/no-show", Mutation(attendance)));
                Assert.Equal(AppointmentStatus.NoShow, absent.Status);
                Assert.Equal(TimeSlotStatus.Open, (await db.TimeSlots.FindAsync(attendance.TimeSlotId))!.Status); break;
            case 13:
                var protectedAppointment = await Book(patient, request);
                await Rejected(await admin.PostAsJsonAsync($"/api/timeslots/{request.TimeSlotId}/close", new { Reason = "Must be blocked" }));
                f.Provider.Now = new(f.Clock.ToUtc(protectedAppointment.AppointmentDate, protectedAppointment.StartTime).AddMinutes(-30));
                var checkedIn = await Data<AppointmentDto>(await staff.PostAsJsonAsync($"/api/appointments/{protectedAppointment.Id}/check-in", Mutation(protectedAppointment)));
                Assert.Equal(AppointmentStatus.CheckedIn, checkedIn.Status);
                await Rejected(await admin.PostAsJsonAsync($"/api/timeslots/{request.TimeSlotId}/close", new { Reason = "Still blocked" }));
                Assert.Equal(TimeSlotStatus.Occupied, (await db.TimeSlots.FindAsync(request.TimeSlotId))!.Status); break;
            case 14:
                var historical = await Book(patient, request);
                await Data<AppointmentDto>(await patient.PostAsJsonAsync($"/api/appointments/{historical.Id}/cancel", Mutation(historical)));
                f.Provider.Now = f.Provider.Now.AddDays(10);
                var doctor = await Data<DoctorDetailDto>(await admin.GetAsync($"/api/doctors/{historical.DoctorId}"));
                (await admin.PutAsJsonAsync($"/api/admin/doctors/{doctor.Id}", new SaveDoctorRequest { FullName = doctor.FullName, SpecialtyId = doctor.SpecialtyId, IsActive = false })).EnsureSuccessStatusCode();
                var specialty = await Data<SpecialtyDto>(await admin.GetAsync($"/api/specialties/{historical.SpecialtyId}"));
                (await admin.PutAsJsonAsync($"/api/admin/specialties/{specialty.Id}", new SaveSpecialtyRequest { Code = specialty.Code, Name = specialty.Name, IsActive = false })).EnsureSuccessStatusCode();
                Assert.False((await db.Doctors.FindAsync(doctor.Id))!.IsActive); Assert.False((await db.Specialties.FindAsync(specialty.Id))!.IsActive);
                Assert.Equal(AppointmentStatus.Cancelled, (await Data<AppointmentDto>(await patient.GetAsync($"/api/appointments/{historical.Id}"))).Status);
                Assert.Equal(2, await db.AppointmentStatusHistories.CountAsync(h => h.AppointmentId == historical.Id)); break;
            case 15:
                var soleAdmin = await Data<UserDto>(await admin.GetAsync($"/api/admin/users/{f.Actor("admin").UserId}"));
                await Rejected(await admin.PutAsJsonAsync($"/api/admin/users/{soleAdmin.Id}", new UpdateUserRequest { FullName = soleAdmin.FullName, Email = soleAdmin.Email, IsActive = false }));
                await Rejected(await admin.PutAsJsonAsync($"/api/admin/users/{soleAdmin.Id}/role", new ChangeRoleRequest { Role = UserRole.Staff }));
                Assert.Equal(1, await db.Users.CountAsync(u => u.IsActive && u.RoleId == (int)UserRole.Admin)); break;
            case 16:
                var once = await Book(patient, request); var twice = await Book(patient, request);
                Assert.Equal(once.Id, twice.Id);
                Assert.Equal(1, await db.Appointments.CountAsync(a => a.RequestKey == request.RequestKey));
                Assert.Equal(1, await db.AppointmentStatusHistories.CountAsync(h => h.AppointmentId == once.Id));
                Assert.Equal(1, await db.AuditLogs.CountAsync(a => a.EntityId == once.Id.ToString() && a.Action == "BOOK_APPOINTMENT")); break;
            case 18:
                var notified = await Book(patient, request);
                var moved = await Data<AppointmentDto>(await patient.PostAsJsonAsync($"/api/appointments/{notified.Id}/reschedule", Move(notified, f.Slot(day: 2).Id)));
                await Data<AppointmentDto>(await patient.PostAsJsonAsync($"/api/appointments/{notified.Id}/cancel", Mutation(moved)));
                await new NotificationDispatcher(db, f.Clock).DispatchAsync(); await new NotificationDispatcher(db, f.Clock).DispatchAsync();
                var notifications = await db.Notifications.Where(n => n.AppointmentId == notified.Id).ToListAsync();
                Assert.Equal(new[] { NotificationType.Confirmed, NotificationType.Rescheduled, NotificationType.Cancelled }, notifications.Select(n => n.Type).OrderBy(t => t));
                Assert.All(notifications, n => { Assert.False(n.IsRead); Assert.True(n.IsSent); });
                Assert.Contains(notified.AppointmentCode, await patient.GetStringAsync("/api/notifications"));
                Assert.Equal(HttpStatusCode.NotFound, (await other.PutAsync($"/api/notifications/{notifications[0].Id}/read", null)).StatusCode);
                (await patient.PutAsync($"/api/notifications/{notifications[0].Id}/read", null)).EnsureSuccessStatusCode();
                db.ChangeTracker.Clear(); Assert.True((await db.Notifications.FindAsync(notifications[0].Id))!.IsRead); break;
            default: throw new ArgumentOutOfRangeException(nameof(scenario));
        }
    }
    private static AppointmentMutationRequest Mutation(AppointmentDto a) => new() { RowVersion = a.RowVersion, Reason = "Phase 7 acceptance" };
    private static RescheduleAppointmentRequest Move(AppointmentDto a, Guid target) => new() { TimeSlotId = target, SpecialtyId = a.SpecialtyId, RowVersion = a.RowVersion, Reason = "Phase 7 reschedule" };
    private static async Task<AppointmentDto> Book(HttpClient client, BookAppointmentRequest request) => await Data<AppointmentDto>(await client.PostAsJsonAsync("/api/appointments", request));
    private static async Task<T> Data<T>(HttpResponseMessage response)
    {
        using (response) { response.EnsureSuccessStatusCode(); return (await response.Content.ReadFromJsonAsync<ApiResponse<T>>(TestFixture.Json))!.Data!; }
    }
    private static async Task Rejected(HttpResponseMessage response)
    {
        using (response)
        {
            Assert.True(response.StatusCode is HttpStatusCode.BadRequest or HttpStatusCode.Conflict, await response.Content.ReadAsStringAsync());
            Assert.NotEmpty((await response.Content.ReadFromJsonAsync<ApiResponse>())!.TraceId!);
        }
    }

    [SqlServerFact]
    public async Task SqlServerWriteFailureRollsBackMutationAndOutboxDeliveryCanRecover()
    {
        using var f = new TestFixture(Environment.GetEnvironmentVariable("HOSPITAL_TEST_SQLSERVER")!);
        using var patient = await f.ClientAsync("patient1");
        var original = await Book(patient, ConcurrencyTests.Request(f));
        var target = f.Slot(day: 2);
        using (var broken = f.CreateDb(new FailSave()))
            await Assert.ThrowsAsync<IOException>(() => f.Appointments(broken, f.Actor("patient1")).RescheduleAsync(original.Id, Move(original, target.Id)));
        using (var verify = f.CreateDb())
        {
            var current = (await verify.Appointments.FindAsync(original.Id))!;
            Assert.Equal(original.TimeSlotId, current.TimeSlotId); Assert.Equal(original.RowVersion, current.RowVersion);
            Assert.Equal(TimeSlotStatus.Open, (await verify.TimeSlots.FindAsync(target.Id))!.Status);
            Assert.Equal(TimeSlotStatus.Occupied, (await verify.TimeSlots.FindAsync(original.TimeSlotId))!.Status);
            Assert.Equal(1, await verify.AppointmentStatusHistories.CountAsync(h => h.AppointmentId == original.Id));
            Assert.False(await verify.NotificationOutbox.AnyAsync(n => n.AppointmentId == original.Id && n.Type == NotificationType.Rescheduled));
        }
        AppointmentDto committed;
        using (var brokenDelivery = f.CreateDb(new FailSave(notificationsOnly: true)))
            committed = await f.Appointments(brokenDelivery, f.Actor("patient2")).BookAsync(new()
                { TimeSlotId = target.Id, SpecialtyId = original.SpecialtyId, RequestKey = Guid.NewGuid() });
        using var recovered = f.CreateDb();
        Assert.True(await recovered.Appointments.AnyAsync(a => a.Id == committed.Id));
        Assert.False(await recovered.Notifications.AnyAsync(n => n.AppointmentId == committed.Id));
        await new NotificationDispatcher(recovered, f.Clock).DispatchAsync(); await new NotificationDispatcher(recovered, f.Clock).DispatchAsync();
        Assert.Equal(1, await recovered.Notifications.CountAsync(n => n.AppointmentId == committed.Id));
    }
}
