using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Hospital.Application.Common.Models;
using Hospital.Application.DTOs.Admin;
using Hospital.Application.DTOs.Appointments;
using Hospital.Application.DTOs.Catalog;
using Hospital.Application.DTOs.Schedules;
using Hospital.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Hospital.IntegrationTests;

public class ApiTests
{
    [Fact]
    public async Task DatabaseOutageReportsTraceAndLeavesNoPartialBooking()
    {
        using var f = new TestFixture(); using var client = await f.ClientAsync("patient1");
        var request = ConcurrencyTests.Request(f);
        f.DatabaseUnavailable = true;
        using var probe = await f.ClientAsync();
        Assert.Equal(HttpStatusCode.ServiceUnavailable, (await probe.GetAsync("/health/ready")).StatusCode);
        var response = await client.PostAsJsonAsync("/api/appointments", request);
        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        var error = await response.Content.ReadFromJsonAsync<ApiResponse>();
        Assert.False(error!.Success); Assert.NotEmpty(error.TraceId!); Assert.Null(error.Errors);
        f.DatabaseUnavailable = false;
        using var db = f.CreateDb();
        Assert.False(await db.Appointments.AnyAsync(a => a.RequestKey == request.RequestKey));
        Assert.Equal(TimeSlotStatus.Open, (await db.TimeSlots.FindAsync(request.TimeSlotId))!.Status);
    }

    [Theory]
    [InlineData("/api/auth/profile")]
    [InlineData("/api/doctors")]
    [InlineData("/api/specialties")]
    [InlineData("/api/timeslots/available")]
    [InlineData("/api/notifications")]
    public async Task ProtectedEndpointsRequireAuthentication(string path)
    {
        using var f = new TestFixture(); using var client = await f.ClientAsync();
        var response = await client.GetAsync(path); Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<ApiResponse>();
        Assert.False(body!.Success); Assert.NotEmpty(body.TraceId!);
    }

    [Theory]
    [InlineData("patient1", "/api/admin/users")]
    [InlineData("patient1", "/api/schedules")]
    [InlineData("staff1", "/api/schedules")]
    [InlineData("doctor1", "/api/appointments")]
    [InlineData("admin", "/api/appointments")]
    public async Task WrongRoleIsForbidden(string username, string path)
    {
        using var f = new TestFixture(); using var client = await f.ClientAsync(username);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync(path)).StatusCode);
    }

    [Fact]
    public async Task AuthRejectsDuplicatesWrongPasswordInactiveAccountAndInvalidInput()
    {
        using var f = new TestFixture(); using var client = await f.ClientAsync();
        var duplicate = await client.PostAsJsonAsync("/api/auth/register", new
        { Username = "patient1", Password = "Patient@123", FullName = "Duplicate", Email = "other@test.com", DateOfBirth = "1990-01-01" });
        Assert.Equal(HttpStatusCode.Conflict, duplicate.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await client.PostAsJsonAsync("/api/auth/register", new
        { Username = "freshname", Password = "Patient@123", FullName = "Duplicate email", Email = "patient1@gmail.com", DateOfBirth = "1990-01-01" })).StatusCode);
        var wrong = await client.PostAsJsonAsync("/api/auth/login", new { Username = "patient1", Password = "WrongPassword" });
        Assert.Equal(HttpStatusCode.Unauthorized, wrong.StatusCode);
        using (var db = f.CreateDb()) { var user = await db.Users.SingleAsync(u => u.Username == "patient1"); user.IsActive = false; await db.SaveChangesAsync(); }
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.PostAsJsonAsync("/api/auth/login", new { Username = "patient1", Password = "Patient@123" })).StatusCode);
        var invalid = await client.PostAsJsonAsync("/api/auth/register", new { Username = "x", Password = "short", Email = "bad" });
        Assert.Equal(HttpStatusCode.BadRequest, invalid.StatusCode);
        Assert.Equal("VALIDATION_ERROR", (await invalid.Content.ReadFromJsonAsync<ApiResponse>())!.ErrorCode);
    }

    [Fact]
    public async Task AuthRateLimiterReturns429()
    {
        using var f = new TestFixture(); using var client = await f.ClientAsync();
        for (var i = 0; i < 10; i++)
            Assert.Equal(HttpStatusCode.Unauthorized, (await client.PostAsJsonAsync("/api/auth/login", new { Username = "unknown", Password = "invalid" })).StatusCode);
        var limited = await client.PostAsJsonAsync("/api/auth/login", new { Username = "unknown", Password = "invalid" });
        Assert.Equal(HttpStatusCode.TooManyRequests, limited.StatusCode); Assert.NotNull(limited.Headers.RetryAfter);
    }

    [Fact]
    public async Task LockRoleChangeAndPasswordChangeInvalidateExistingTokens()
    {
        using var f = new TestFixture(); using var patient = await f.ClientAsync("patient1");
        Assert.Equal(HttpStatusCode.OK, (await patient.PostAsJsonAsync("/api/auth/change-password", new { OldPassword = "Patient@123", NewPassword = "NewPassword@123" })).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await patient.GetAsync("/api/auth/profile")).StatusCode);
        using var staff = await f.ClientAsync("staff1");
        using var admin = await f.ClientAsync("admin");
        var staffId = f.Actor("staff1").UserId;
        Assert.Equal(HttpStatusCode.OK, (await admin.PutAsJsonAsync($"/api/admin/users/{staffId}/role", new { Role = "Admin" })).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await staff.GetAsync("/api/patients")).StatusCode);
        using var patient2 = await f.ClientAsync("patient2");
        Assert.Equal(HttpStatusCode.OK, (await admin.PutAsJsonAsync($"/api/admin/users/{f.Actor("patient2").UserId}",
            new { FullName = "Patient 2", Email = "patient2@gmail.com", IsActive = false })).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await patient2.GetAsync("/api/auth/profile")).StatusCode);
    }

    [Fact]
    public async Task PatientOwnershipAndPaginationAreCheckedAtApi()
    {
        using var f = new TestFixture(); using var first = await f.ClientAsync("patient1"); using var second = await f.ClientAsync("patient2");
        var response = await second.PostAsJsonAsync("/api/appointments", new BookAppointmentRequest
        { TimeSlotId = f.Slot().Id, SpecialtyId = f.Specialty(), RequestKey = Guid.NewGuid() });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var a = (await response.Content.ReadFromJsonAsync<ApiResponse<AppointmentDto>>(TestFixture.Json))!.Data!;
        Assert.Equal(HttpStatusCode.NotFound, (await first.GetAsync($"/api/appointments/{a.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await first.PostAsJsonAsync($"/api/appointments/{a.Id}/cancel", new { a.RowVersion })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await first.GetAsync("/api/appointments/my?pageSize=101")).StatusCode);
        var page = (await first.GetFromJsonAsync<ApiResponse<PagedResult<SpecialtyDto>>>("/api/specialties?page=2&pageSize=2"))!.Data!;
        Assert.Equal(2, page.Items.Count); Assert.Equal(5, page.TotalCount);
        using var doctor = await f.ClientAsync("doctor2");
        var list = (await doctor.GetFromJsonAsync<ApiResponse<PagedResult<AppointmentDto>>>("/api/appointments/doctor", TestFixture.Json))!.Data!;
        Assert.All(list.Items, x => Assert.Equal(f.Actor("doctor2").DoctorId, x.DoctorId));
    }

    [Fact]
    public async Task BookingThroughApiRequiresKeyAndVersionAndSupportsReplay()
    {
        using var f = new TestFixture(); using var client = await f.ClientAsync("patient1");
        var req = new BookAppointmentRequest { TimeSlotId = f.Slot().Id, SpecialtyId = f.Specialty() };
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync("/api/appointments", req)).StatusCode);
        req.RequestKey = Guid.NewGuid();
        var first = await client.PostAsJsonAsync("/api/appointments", req); first.EnsureSuccessStatusCode();
        var a = (await first.Content.ReadFromJsonAsync<ApiResponse<AppointmentDto>>(TestFixture.Json))!.Data!;
        var again = (await (await client.PostAsJsonAsync("/api/appointments", req)).Content.ReadFromJsonAsync<ApiResponse<AppointmentDto>>(TestFixture.Json))!.Data!;
        Assert.Equal(a.Id, again.Id);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync($"/api/appointments/{a.Id}/cancel", new { })).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.PostAsJsonAsync($"/api/appointments/{a.Id}/cancel", new { a.RowVersion })).StatusCode);
    }

    [Fact]
    public async Task FullBackendFlowAdminToPatientDoctorAndReceptionUsesOneDatabase()
    {
        using var f = new TestFixture();
        await VerifyFullBackendFlowAsync(f);
    }

    internal static async Task VerifyFullBackendFlowAsync(TestFixture f)
    {
        using var admin = await f.ClientAsync("admin");
        var specialty = f.Specialty();
        var doctorResponse = await admin.PostAsJsonAsync("/api/admin/doctors", new SaveDoctorRequest
        { FullName = "Bác sĩ mới", SpecialtyIds = [specialty], PrimarySpecialtyId = specialty });
        Assert.Equal(HttpStatusCode.Created, doctorResponse.StatusCode);
        var doctor = (await doctorResponse.Content.ReadFromJsonAsync<ApiResponse<DoctorDetailDto>>())!.Data!;
        var account = await admin.PostAsJsonAsync("/api/admin/users", new CreateUserRequest
        { Username = "doctor3", Password = "Doctor@123", FullName = doctor.FullName, Email = "doctor3@test.com", Role = UserRole.Doctor, DoctorId = doctor.Id });
        Assert.Equal(HttpStatusCode.Created, account.StatusCode);
        var date = f.Clock.Today.AddDays(8);
        var scheduleResponse = await admin.PostAsJsonAsync("/api/schedules", new CreateWorkingScheduleRequestDto
        { DoctorId = doctor.Id, WorkDate = date, StartTime = new(9, 0), EndTime = new(10, 0) });
        Assert.Equal(HttpStatusCode.Created, scheduleResponse.StatusCode);
        using var patient = await f.ClientAsync("patient1");
        var slots = (await patient.GetFromJsonAsync<ApiResponse<List<TimeSlotDto>>>($"/api/timeslots/available?doctorId={doctor.Id}&date={date:yyyy-MM-dd}", TestFixture.Json))!.Data!;
        Assert.Equal(2, slots.Count);
        var booking = await patient.PostAsJsonAsync("/api/appointments", new BookAppointmentRequest
        { TimeSlotId = slots[0].Id, SpecialtyId = specialty, RequestKey = Guid.NewGuid() });
        Assert.Equal(HttpStatusCode.Created, booking.StatusCode);
        var appointment = (await booking.Content.ReadFromJsonAsync<ApiResponse<AppointmentDto>>(TestFixture.Json))!.Data!;
        using var doctorClient = await f.ClientAsync("doctor3");
        Assert.Contains((await doctorClient.GetFromJsonAsync<ApiResponse<PagedResult<AppointmentDto>>>("/api/appointments/doctor", TestFixture.Json))!.Data!.Items, a => a.Id == appointment.Id);
        f.Provider.Now = new DateTimeOffset(f.Clock.ToUtc(date, slots[0].StartTime).AddMinutes(-10));
        using var staff = await f.ClientAsync("staff1");
        Assert.Equal(HttpStatusCode.OK, (await staff.PostAsJsonAsync($"/api/appointments/{appointment.Id}/check-in", new { appointment.RowVersion })).StatusCode);
        Assert.Equal(AppointmentStatus.CheckedIn, (await patient.GetFromJsonAsync<ApiResponse<AppointmentDto>>($"/api/appointments/{appointment.Id}", TestFixture.Json))!.Data!.Status);
        var notifications = (await patient.GetFromJsonAsync<ApiResponse<PagedResult<Hospital.Application.Services.Interfaces.NotificationDto>>>("/api/notifications", TestFixture.Json))!.Data!;
        Assert.Contains(notifications.Items, n => n.AppointmentId == appointment.Id);
    }
}
