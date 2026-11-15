using System.Net;
using System.Net.Http.Json;
using Hospital.Application.Common.Models;
using Hospital.Application.DTOs.Appointments;
using Hospital.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Hospital.IntegrationTests;

public class SqlServerBackendTests
{
    private static TestFixture Fixture() => new(Environment.GetEnvironmentVariable("HOSPITAL_TEST_SQLSERVER")!);

    [SqlServerFact]
    public async Task FullBackendFlowUsesSqlServerThroughHttp()
    {
        using var f = Fixture();
        await ApiTests.VerifyFullBackendFlowAsync(f);
    }

    [SqlServerFact]
    public async Task RealRowVersionRejectsStaleApiAndDatabaseWrites()
    {
        using var f = Fixture();
        using var client = await f.ClientAsync("patient1");
        var request = ConcurrencyTests.Request(f);
        using var booking = await client.PostAsJsonAsync("/api/appointments", request);
        Assert.Equal(HttpStatusCode.Created, booking.StatusCode);
        var original = (await booking.Content.ReadFromJsonAsync<ApiResponse<AppointmentDto>>(TestFixture.Json))!.Data!;
        Assert.Equal(8, original.RowVersion.Length);

        using var staleDb = f.CreateDb();
        var stale = await staleDb.Appointments.SingleAsync(a => a.Id == original.Id);
        var target = f.Slot(hour: 10);
        using var reschedule = await client.PostAsJsonAsync($"/api/appointments/{original.Id}/reschedule",
            new RescheduleAppointmentRequest { TimeSlotId = target.Id, SpecialtyId = request.SpecialtyId,
                RowVersion = original.RowVersion });
        Assert.Equal(HttpStatusCode.OK, reschedule.StatusCode);
        var changed = (await reschedule.Content.ReadFromJsonAsync<ApiResponse<AppointmentDto>>(TestFixture.Json))!.Data!;
        Assert.Equal(8, changed.RowVersion.Length);
        Assert.False(original.RowVersion.SequenceEqual(changed.RowVersion));

        using var cancellation = await client.PostAsJsonAsync($"/api/appointments/{original.Id}/cancel",
            new { original.RowVersion });
        Assert.Equal(HttpStatusCode.Conflict, cancellation.StatusCode);
        Assert.Equal("CONCURRENCY_CONFLICT", (await cancellation.Content.ReadFromJsonAsync<ApiResponse>())!.ErrorCode);
        stale.Notes = "Stale write must not overwrite the rescheduled appointment";
        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => staleDb.SaveChangesAsync());

        using var verify = f.CreateDb();
        var saved = await verify.Appointments.Include(a => a.StatusHistories).SingleAsync(a => a.Id == original.Id);
        Assert.Equal(target.Id, saved.TimeSlotId);
        Assert.Equal(AppointmentStatus.Confirmed, saved.Status);
        Assert.Equal(2, saved.StatusHistories.Count);
        Assert.Null(saved.Notes);
        Assert.Equal(TimeSlotStatus.Open, (await verify.TimeSlots.FindAsync(request.TimeSlotId))!.Status);
        Assert.Equal(TimeSlotStatus.Occupied, (await verify.TimeSlots.FindAsync(target.Id))!.Status);
    }

    [SqlServerFact]
    public async Task UnavailableSqlDatabaseReturns503AndRecoversWithoutPartialBooking()
    {
        using var f = Fixture();
        using var client = await f.ClientAsync("patient1");
        using var probe = await f.ClientAsync();
        var request = ConcurrencyTests.Request(f);
        f.DatabaseUnavailable = true;
        try
        {
            using var readiness = await probe.GetAsync("/health/ready");
            Assert.Equal(HttpStatusCode.ServiceUnavailable, readiness.StatusCode);
            using var response = await client.PostAsJsonAsync("/api/appointments", request);
            Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
            var error = (await response.Content.ReadFromJsonAsync<ApiResponse>())!;
            Assert.False(error.Success);
            Assert.Equal("DATABASE_UNAVAILABLE", error.ErrorCode);
            Assert.NotEmpty(error.TraceId!);
            Assert.Null(error.Errors);
            Assert.DoesNotContain("HospitalPhase3Tests_", await response.Content.ReadAsStringAsync());
        }
        finally { f.DatabaseUnavailable = false; }

        using (var verify = f.CreateDb())
        {
            Assert.False(await verify.Appointments.AnyAsync(a => a.RequestKey == request.RequestKey));
            Assert.Equal(TimeSlotStatus.Open, (await verify.TimeSlots.FindAsync(request.TimeSlotId))!.Status);
        }
        using var recovered = await probe.GetAsync("/health/ready");
        Assert.Equal(HttpStatusCode.OK, recovered.StatusCode);
        using var retry = await client.PostAsJsonAsync("/api/appointments", request);
        Assert.Equal(HttpStatusCode.Created, retry.StatusCode);
        using var db = f.CreateDb();
        Assert.Equal(1, await db.Appointments.CountAsync(a => a.RequestKey == request.RequestKey));
    }
}
