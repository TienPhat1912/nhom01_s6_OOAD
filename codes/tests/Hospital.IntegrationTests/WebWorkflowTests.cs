extern alias web;

using System.Net;
using System.Net.Http.Json;
using System.Text.RegularExpressions;
using Hospital.Application.Common.Models;
using Hospital.Application.DTOs.Appointments;
using Hospital.Client;
using Hospital.Domain.Enums;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Hospital.IntegrationTests;

// Exercise MVC + cookies + antiforgery + the actual API/services/database in one deterministic host pair.
internal sealed class ApiBridge(HttpClient api) : HttpMessageHandler
{
    public bool LoseNextBookingResponse { get; set; }
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        using var forwarded = new HttpRequestMessage(request.Method, request.RequestUri!.PathAndQuery);
        foreach (var header in request.Headers) forwarded.Headers.TryAddWithoutValidation(header.Key, header.Value);
        if (request.Content != null)
        {
            forwarded.Content = new ByteArrayContent(await request.Content.ReadAsByteArrayAsync(cancellationToken));
            foreach (var header in request.Content.Headers) forwarded.Content.Headers.TryAddWithoutValidation(header.Key, header.Value);
        }
        var result = await api.SendAsync(forwarded, cancellationToken);
        if (LoseNextBookingResponse && request.Method == HttpMethod.Post && request.RequestUri.AbsolutePath == "/api/appointments" && result.IsSuccessStatusCode)
        {
            LoseNextBookingResponse = false; result.Dispose();
            return new(HttpStatusCode.GatewayTimeout) { Content = JsonContent.Create(new { success = false, message = "Response lost after commit.", traceId = "lost-response" }) };
        }
        return result;
    }
}

internal sealed class WorkflowWebFactory(TestFixture fixture, ApiBridge bridge) : WebApplicationFactory<web::Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<TimeProvider>(); services.AddSingleton<TimeProvider>(fixture.Provider);
            services.AddHttpClient<HospitalApiClient>().ConfigurePrimaryHttpMessageHandler(() => bridge);
        });
    }
    public HttpClient Browser() => CreateClient(new() { BaseAddress = new("https://localhost"), AllowAutoRedirect = false });
}

public class WebWorkflowTests
{
    private static string Field(string html, string name)
    {
        var input = Regex.Match(html, "<input[^>]*name=\"" + Regex.Escape(name) + "\"[^>]*>").Value;
        var value = Regex.Match(input, "value=\"([^\"]*)\"").Groups[1].Value;
        Assert.False(string.IsNullOrEmpty(value), $"Missing field: {name}");
        return WebUtility.HtmlDecode(value);
    }
    private static async Task<HttpResponseMessage> Post(HttpClient browser, string path, string html, Dictionary<string, string> fields)
    {
        fields["__RequestVerificationToken"] = Field(html, "__RequestVerificationToken");
        return await browser.PostAsync(path, new FormUrlEncodedContent(fields));
    }
    private static async Task Login(HttpClient browser, string username = "patient1", string password = "Patient@123")
    {
        var html = await browser.GetStringAsync("/Account/Login");
        using var response = await Post(browser, "/Account/Login", html, new() { ["Username"] = username, ["Password"] = password });
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
    }
    private static async Task<string> Review(HttpClient browser, TestFixture fixture, int hour = 9, Guid? patientId = null, Guid? appointmentId = null, string? reason = null, string? existingContext = null)
    {
        var context = existingContext;
        if (context == null)
        {
            using var start = await browser.GetAsync($"/Booking/Start?patientId={patientId}&appointmentId={appointmentId}");
            Assert.Equal(HttpStatusCode.Redirect, start.StatusCode);
            context = Microsoft.AspNetCore.WebUtilities.QueryHelpers.ParseQuery(start.Headers.Location!.OriginalString.Split('?').Last())["context"].ToString();
        }
        var slot = fixture.Slot(hour: hour);
        var path = $"/Booking/Slots?context={Uri.EscapeDataString(context)}&specialtyId={fixture.Specialty()}&doctorId={slot.DoctorId}&date={slot.SlotDate:yyyy-MM-dd}";
        var slots = await browser.GetStringAsync(path);
        using var review = await Post(browser, "/Booking/Review", slots, new()
        {
            ["Context"] = context, ["SpecialtyId"] = fixture.Specialty().ToString(), ["DoctorId"] = slot.DoctorId.ToString(),
            ["Date"] = slot.SlotDate.ToString("yyyy-MM-dd"), ["TimeSlotId"] = slot.Id.ToString(), ["Notes"] = "Web workflow test", ["Reason"] = reason ?? ""
        });
        Assert.Equal(HttpStatusCode.OK, review.StatusCode);
        return await review.Content.ReadAsStringAsync();
    }
    private static Task<HttpResponseMessage> Confirm(HttpClient browser, string html) =>
        Post(browser, "/Booking/Confirm", html, new() { ["draftToken"] = Field(html, "draftToken") });
    private static Guid ResultId(HttpResponseMessage response)
    {
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        return Guid.Parse(response.Headers.Location!.OriginalString.Split('/').Last());
    }

    [Fact]
    public async Task RegisterAndPasswordChangeValidateAndRevokeWebSessions()
    {
        using var fixture = new TestFixture(); using var api = await fixture.ClientAsync();
        using var factory = new WorkflowWebFactory(fixture, new(api)); using var browser = factory.Browser();
        var form = await browser.GetStringAsync("/Account/Register");
        var fields = new Dictionary<string, string> { ["Username"] = "webnew", ["FullName"] = "Web Patient", ["Email"] = "webnew@example.test",
            ["Password"] = "WebPatient@123", ["ConfirmPassword"] = "Mismatch@123", ["DateOfBirth"] = "1995-01-01", ["Gender"] = "Female", ["Role"] = "Admin" };
        using var invalid = await Post(browser, "/Account/Register", form, fields);
        Assert.Contains("validation-summary-errors", await invalid.Content.ReadAsStringAsync());
        using (var db = fixture.CreateDb()) Assert.False(await db.Users.AnyAsync(u => u.Username == "webnew"));
        fields["ConfirmPassword"] = fields["Password"];
        using var registered = await Post(browser, "/Account/Register", form, fields);
        Assert.Equal(HttpStatusCode.Redirect, registered.StatusCode);
        using (var db = fixture.CreateDb()) Assert.Equal((int)UserRole.Patient, (await db.Users.SingleAsync(u => u.Username == "webnew")).RoleId);
        var passwordForm = await browser.GetStringAsync("/Account/ChangePassword");
        using var wrong = await Post(browser, "/Account/ChangePassword", passwordForm, new()
            { ["OldPassword"] = "Incorrect@123", ["NewPassword"] = "NewPassword@123", ["ConfirmPassword"] = "NewPassword@123" });
        var wrongHtml = await wrong.Content.ReadAsStringAsync();
        Assert.Contains("validation-summary-errors", wrongHtml); Assert.DoesNotContain("value=\"NewPassword@123\"", wrongHtml);
        using var changed = await Post(browser, "/Account/ChangePassword", passwordForm, new()
            { ["OldPassword"] = "WebPatient@123", ["NewPassword"] = "NewPassword@123", ["ConfirmPassword"] = "NewPassword@123" });
        Assert.Equal("/Account/Login", changed.Headers.Location!.OriginalString);
        Assert.Contains("/Account/Login", (await browser.GetAsync("/Dashboard")).Headers.Location!.OriginalString);
        await Login(browser, "webnew", "NewPassword@123");
    }

    [Fact]
    public Task PatientBookingRetriesLostResponseWithoutCreatingDuplicate() => BookingRetry(null);

    [SqlServerFact]
    public Task PatientWebBookingUsesSqlServerAndRealRowVersions() => BookingRetry(Environment.GetEnvironmentVariable("HOSPITAL_TEST_SQLSERVER"));

    private static async Task BookingRetry(string? connection)
    {
        using var fixture = new TestFixture(connection); using var api = await fixture.ClientAsync(); var bridge = new ApiBridge(api);
        using var factory = new WorkflowWebFactory(fixture, bridge); using var browser = factory.Browser(); await Login(browser);
        var review = await Review(browser, fixture);
        bridge.LoseNextBookingResponse = true;
        using var lost = await Confirm(browser, review);
        var retryHtml = await lost.Content.ReadAsStringAsync();
        Assert.Contains("lost-response", retryHtml); Assert.Equal(Field(review, "draftToken"), Field(retryHtml, "draftToken"));
        using var saved = await Confirm(browser, retryHtml); var id = ResultId(saved);
        using var replay = await Confirm(browser, review); Assert.Equal(id, ResultId(replay));
        var slotId = fixture.Slot().Id;
        using (var db = fixture.CreateDb()) Assert.Equal(1, await db.Appointments.CountAsync(a => a.TimeSlotId == slotId));
        Assert.Contains("Web workflow test", await browser.GetStringAsync($"/Booking/Receipt/{id}"));
        using var other = factory.Browser(); await Login(other, "patient2");
        var otherForm = await other.GetStringAsync("/Account/Profile");
        using var stolen = await Post(other, "/Booking/Confirm", otherForm, new() { ["draftToken"] = Field(review, "draftToken") });
        Assert.Equal(HttpStatusCode.BadRequest, stolen.StatusCode);
        using var tampered = await Post(browser, "/Booking/Confirm", review, new() { ["draftToken"] = "invalid" });
        Assert.Equal(HttpStatusCode.BadRequest, tampered.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await browser.PostAsync("/Booking/Confirm", new FormUrlEncodedContent(new Dictionary<string, string> { ["draftToken"] = Field(review, "draftToken") }))).StatusCode);
    }

    [Fact]
    public async Task RescheduleAndCancellationPreserveOriginalVersionOnConflict()
    {
        using var fixture = new TestFixture(); using var api = await fixture.ClientAsync();
        using var factory = new WorkflowWebFactory(fixture, new(api)); using var browser = factory.Browser(); await Login(browser);
        using var booked = await Confirm(browser, await Review(browser, fixture)); var id = ResultId(booked);
        var cancel = await browser.GetStringAsync($"/Reception/Cancel/{id}");
        var firstDraft = await Review(browser, fixture, 10, appointmentId: id);
        var staleDraft = await Review(browser, fixture, 11, appointmentId: id);
        using var changed = await Confirm(browser, firstDraft); Assert.Equal(id, ResultId(changed));
        using var staleChange = await Confirm(browser, staleDraft);
        Assert.Contains("validation-summary-errors", await staleChange.Content.ReadAsStringAsync());
        using var staleCancel = await Post(browser, $"/Reception/Cancel/{id}", cancel, new() { ["RowVersion"] = Field(cancel, "RowVersion") });
        var staleHtml = await staleCancel.Content.ReadAsStringAsync();
        Assert.Contains("validation-summary-errors", staleHtml); Assert.Equal(Field(cancel, "RowVersion"), Field(staleHtml, "RowVersion"));
        using (var db = fixture.CreateDb()) { var current = await db.Appointments.FindAsync(id); Assert.Equal(fixture.Slot(hour: 10).Id, current!.TimeSlotId); Assert.Equal(AppointmentStatus.Confirmed, current.Status); }
        var latest = await browser.GetStringAsync($"/Reception/Cancel/{id}");
        using var cancelled = await Post(browser, $"/Reception/Cancel/{id}", latest, new() { ["RowVersion"] = Field(latest, "RowVersion") });
        Assert.Equal(HttpStatusCode.Redirect, cancelled.StatusCode);
        using (var db = fixture.CreateDb()) Assert.Equal(AppointmentStatus.Cancelled, (await db.Appointments.FindAsync(id))!.Status);
    }

    [Fact]
    public async Task StaffAssistedBookingRequiresReasonsAndPatientVerification()
    {
        using var fixture = new TestFixture(); using var api = await fixture.ClientAsync();
        using var factory = new WorkflowWebFactory(fixture, new(api)); using var browser = factory.Browser(); await Login(browser, "staff1", "Staff@123");
        var patient = fixture.Actor("patient2").PatientId!.Value;
        Assert.Contains("patientId", await browser.GetStringAsync("/Patients?keyword=BN-2026"));
        using var booked = await Confirm(browser, await Review(browser, fixture, patientId: patient)); var id = ResultId(booked);
        var missingReason = await Review(browser, fixture, 10, appointmentId: id);
        Assert.Contains("validation-summary-errors", missingReason); Assert.DoesNotContain("name=\"draftToken\"", missingReason);
        var cancel = await browser.GetStringAsync($"/Reception/Cancel/{id}");
        using var invalidCancel = await Post(browser, $"/Reception/Cancel/{id}", cancel, new() { ["RowVersion"] = Field(cancel, "RowVersion") });
        Assert.Contains("validation-summary-errors", await invalidCancel.Content.ReadAsStringAsync());
        using var rescheduled = await Confirm(browser, await Review(browser, fixture, 10, appointmentId: id, reason: "Patient requested a later slot"));
        Assert.Equal(id, ResultId(rescheduled));
        using var second = await Confirm(browser, await Review(browser, fixture, 11, patientId: patient)); var secondId = ResultId(second);
        fixture.Provider.Now = new DateTimeOffset(fixture.Slot(hour: 10).SlotDate.ToDateTime(new TimeOnly(1, 0)), TimeSpan.Zero);
        var checkIn = await browser.GetStringAsync($"/Reception/CheckIn/{id}");
        using var notVerified = await Post(browser, $"/Reception/CheckIn/{id}", checkIn, new() { ["RowVersion"] = Field(checkIn, "RowVersion") });
        Assert.Contains("validation-summary-errors", await notVerified.Content.ReadAsStringAsync());
        using var received = await Post(browser, $"/Reception/CheckIn/{id}", checkIn, new() { ["RowVersion"] = Field(checkIn, "RowVersion"), ["PatientVerified"] = "true" });
        Assert.Equal(HttpStatusCode.Redirect, received.StatusCode);
        fixture.Provider.Now = fixture.Provider.Now.AddHours(4);
        var noShow = await browser.GetStringAsync($"/Reception/NoShow/{secondId}");
        using var marked = await Post(browser, $"/Reception/NoShow/{secondId}", noShow, new() { ["RowVersion"] = Field(noShow, "RowVersion") });
        Assert.Equal(HttpStatusCode.Redirect, marked.StatusCode);
        using var db = fixture.CreateDb();
        Assert.Equal(AppointmentStatus.CheckedIn, (await db.Appointments.FindAsync(id))!.Status);
        Assert.Equal(AppointmentStatus.NoShow, (await db.Appointments.FindAsync(secondId))!.Status);
        Assert.Equal(fixture.Actor("staff1").UserId, (await db.Appointments.FindAsync(id))!.BookedBy);
    }

    [Fact]
    public async Task SlotTakenAfterReviewShowsFailureWithoutCreatingAnotherAppointment()
    {
        using var fixture = new TestFixture(); using var api = await fixture.ClientAsync();
        using var factory = new WorkflowWebFactory(fixture, new(api)); using var browser = factory.Browser(); await Login(browser);
        var review = await Review(browser, fixture);
        using var competitor = await fixture.ClientAsync("patient2");
        var slotId = fixture.Slot().Id;
        using var occupied = await competitor.PostAsJsonAsync("/api/appointments", new BookAppointmentRequest
            { TimeSlotId = slotId, SpecialtyId = fixture.Specialty(), RequestKey = Guid.NewGuid() });
        Assert.Equal(HttpStatusCode.Created, occupied.StatusCode);
        using var rejected = await Confirm(browser, review);
        Assert.Contains("validation-summary-errors", await rejected.Content.ReadAsStringAsync());
        using var db = fixture.CreateDb();
        var only = await db.Appointments.SingleAsync(a => a.TimeSlotId == slotId);
        Assert.Equal(fixture.Actor("patient2").PatientId, only.PatientId);
    }

    [Fact]
    public async Task PatientDeadlineAndEarlyNoShowRemainEnforcedThroughPostedForms()
    {
        using var fixture = new TestFixture(); using var api = await fixture.ClientAsync();
        using var factory = new WorkflowWebFactory(fixture, new(api)); using var browser = factory.Browser(); await Login(browser);
        using var booked = await Confirm(browser, await Review(browser, fixture)); var id = ResultId(booked);
        var slot = fixture.Slot();
        fixture.Provider.Now = new DateTimeOffset(slot.SlotDate.ToDateTime(new TimeOnly(1, 0)), TimeSpan.Zero);
        var detail = await browser.GetStringAsync($"/Appointments/Detail/{id}");
        Assert.DoesNotContain("href=\"/Reception/Cancel", detail);
        Assert.Equal(HttpStatusCode.BadRequest, (await browser.GetAsync($"/Booking/Start?appointmentId={id}")).StatusCode);
        var form = await browser.GetStringAsync($"/Reception/Cancel/{id}");
        using var invalid = await Post(browser, $"/Reception/Cancel/{id}", form, new() { ["RowVersion"] = Field(form, "RowVersion") });
        Assert.Contains("validation-summary-errors", await invalid.Content.ReadAsStringAsync());
        using var staff = factory.Browser(); await Login(staff, "staff1", "Staff@123");
        var noShow = await staff.GetStringAsync($"/Reception/NoShow/{id}");
        using var early = await Post(staff, $"/Reception/NoShow/{id}", noShow, new() { ["RowVersion"] = Field(noShow, "RowVersion") });
        Assert.Contains("validation-summary-errors", await early.Content.ReadAsStringAsync());
        using var db = fixture.CreateDb(); Assert.Equal(AppointmentStatus.Confirmed, (await db.Appointments.FindAsync(id))!.Status);
    }

    [Fact]
    public async Task WebAndPatientLookupEnforceRoleAndOwnershipBoundaries()
    {
        using var fixture = new TestFixture(); using var api = await fixture.ClientAsync();
        using var factory = new WorkflowWebFactory(fixture, new(api)); using var patient = factory.Browser(); await Login(patient);
        Assert.Contains("AccessDenied", (await patient.GetAsync("/Patients")).Headers.Location!.OriginalString);
        Assert.Contains("AccessDenied", (await patient.GetAsync("/Reception/CheckIn/" + Guid.NewGuid())).Headers.Location!.OriginalString);
        using var doctor = factory.Browser(); await Login(doctor, "doctor1", "Doctor@123");
        Assert.Contains("AccessDenied", (await doctor.GetAsync("/Booking")).Headers.Location!.OriginalString);
        Assert.Equal(HttpStatusCode.OK, (await doctor.GetAsync("/Schedule")).StatusCode);
        using var patientApi = await fixture.ClientAsync("patient1");
        Assert.Equal(HttpStatusCode.Forbidden, (await patientApi.GetAsync($"/api/patients/{fixture.Actor("patient2").PatientId}")).StatusCode);
        var invalidDates = await patient.GetStringAsync("/Appointments?fromDate=2026-12-01&toDate=2026-01-01");
        Assert.Contains("validation-summary-errors", invalidDates);
    }
}
