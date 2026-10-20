using Hospital.Application.Common.Exceptions;
using Hospital.Application.DTOs.Appointments;
using Hospital.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Hospital.IntegrationTests;

public class ConcurrencyTests
{
    public static async Task<(Guid? Id, Exception? Error)> AttemptAsync(TestFixture f, string patient, BookAppointmentRequest request, Task gate)
    {
        var actor = f.Actor(patient);
        await gate;
        using var db = f.CreateDb();
        try { return ((await f.Appointments(db, actor).BookAsync(request)).Id, null); }
        catch (Exception error) { return (null, error); }
    }

    public static BookAppointmentRequest Request(TestFixture f) => new()
    { TimeSlotId = f.Slot().Id, SpecialtyId = f.Specialty(), RequestKey = Guid.NewGuid() };

    [Fact]
    public async Task ConcurrentBookingsOnRelationalDatabaseHaveExactlyOneWinner()
    {
        using var f = new TestFixture();
        var gate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var r1 = Request(f); var r2 = Request(f);
        var first = Task.Run(() => AttemptAsync(f, "patient1", r1, gate.Task));
        var second = Task.Run(() => AttemptAsync(f, "patient2", r2, gate.Task));
        gate.SetResult(); var results = await Task.WhenAll(first, second);
        Assert.Single(results, r => r.Id.HasValue);
        Assert.IsType<BusinessRuleException>(results.Single(r => r.Error != null).Error);
        using var db = f.CreateDb();
        Assert.Equal(1, await db.Appointments.CountAsync(a => a.TimeSlotId == r1.TimeSlotId && a.Status == AppointmentStatus.Confirmed));
    }
}

public sealed class SqlServerFactAttribute : FactAttribute
{
    public SqlServerFactAttribute()
    {
        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("HOSPITAL_TEST_SQLSERVER")))
            Skip = "Cần HOSPITAL_TEST_SQLSERVER trỏ tới SQL Server thử nghiệm (quyền tạo/xóa database riêng).";
    }
}

public class SqlServerConcurrencyTests
{
    private static TestFixture Fixture() => new(Environment.GetEnvironmentVariable("HOSPITAL_TEST_SQLSERVER")!);

    [SqlServerFact]
    public async Task MigrationsAndConcurrentSlotBookingOnSqlServer()
    {
        using var f = Fixture();
        var gate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var r1 = ConcurrencyTests.Request(f); var r2 = ConcurrencyTests.Request(f);
        var first = Task.Run(() => ConcurrencyTests.AttemptAsync(f, "patient1", r1, gate.Task));
        var second = Task.Run(() => ConcurrencyTests.AttemptAsync(f, "patient2", r2, gate.Task));
        gate.SetResult(); var results = await Task.WhenAll(first, second);
        Assert.Single(results, r => r.Id.HasValue);
        Assert.IsType<BusinessRuleException>(results.Single(r => r.Error != null).Error);
        using var db = f.CreateDb(); Assert.Empty(await db.Database.GetPendingMigrationsAsync());
        Assert.Equal(1, await db.Appointments.CountAsync(a => a.TimeSlotId == r1.TimeSlotId && a.Status == AppointmentStatus.Confirmed));
    }

    [SqlServerFact]
    public async Task ConcurrentSameRequestReturnsOneAppointmentOnSqlServer()
    {
        using var f = Fixture(); var gate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var request = ConcurrencyTests.Request(f);
        var first = Task.Run(() => ConcurrencyTests.AttemptAsync(f, "patient1", request, gate.Task));
        var second = Task.Run(() => ConcurrencyTests.AttemptAsync(f, "patient1", request, gate.Task));
        gate.SetResult(); var results = await Task.WhenAll(first, second);
        Assert.All(results, r => Assert.Null(r.Error)); Assert.Equal(results[0].Id, results[1].Id);
        using var db = f.CreateDb(); Assert.Single(await db.Appointments.Where(a => a.RequestKey == request.RequestKey).ToListAsync());
    }

    [SqlServerFact]
    public async Task ConcurrentOverlappingPatientBookingsOnSqlServer()
    {
        using var f = Fixture(); var overlap = await f.ExtraSlotAsync();
        var r1 = ConcurrencyTests.Request(f); var r2 = ConcurrencyTests.Request(f);
        r2.TimeSlotId = overlap.Id; r2.SpecialtyId = f.Specialty("NHI");
        var gate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var first = Task.Run(() => ConcurrencyTests.AttemptAsync(f, "patient1", r1, gate.Task));
        var second = Task.Run(() => ConcurrencyTests.AttemptAsync(f, "patient1", r2, gate.Task));
        gate.SetResult(); var results = await Task.WhenAll(first, second);
        Assert.Single(results, r => r.Id.HasValue);
        Assert.Equal("PATIENT_SCHEDULE_OVERLAP", Assert.IsType<BusinessRuleException>(results.Single(r => r.Error != null).Error).ErrorCode);
    }

    [SqlServerFact]
    public async Task ConcurrentDisablingAdminsPreservesOneActiveAdminOnSqlServer()
    {
        using var f = Fixture(); using var db = f.CreateDb();
        var second = await f.Admin(db).CreateUserAsync(new()
        { Username = "admin2", Password = "Admin@123", FullName = "Second admin", Email = "admin2@test.com", Role = UserRole.Admin });
        var firstId = f.Actor("admin").UserId!.Value;
        var gate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        async Task<Exception?> Disable(Guid id, string email)
        {
            await gate.Task; using var context = f.CreateDb();
            try { await f.Admin(context).UpdateUserAsync(id, new() { FullName = "Admin", Email = email, IsActive = false }); return null; }
            catch (Exception ex) { return ex; }
        }
        var firstTask = Task.Run(() => Disable(firstId, "admin@hospital.com"));
        var secondTask = Task.Run(() => Disable(second.Id, second.Email));
        gate.SetResult(); var errors = await Task.WhenAll(firstTask, secondTask);
        Assert.Single(errors, e => e == null);
        Assert.Equal("LAST_ADMIN_PROTECTED", Assert.IsType<BusinessRuleException>(errors.Single(e => e != null)).ErrorCode);
        using var verify = f.CreateDb(); Assert.Equal(1, await verify.Users.CountAsync(u => u.RoleId == (int)UserRole.Admin && u.IsActive));
    }
}
