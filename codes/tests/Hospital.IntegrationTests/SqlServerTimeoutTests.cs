using System.Data.Common;
using Hospital.Application.DTOs.Appointments;
using Hospital.Domain.Enums;
using Hospital.Infrastructure.Data;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Hospital.IntegrationTests;

public class SqlServerTimeoutTests
{
    [SqlServerFact]
    public async Task CommandTimeoutAfterWriteRollsBackBothSlotsHistoryAuditAndOutbox()
    {
        using var f = new TestFixture(Environment.GetEnvironmentVariable("HOSPITAL_TEST_SQLSERVER")!);
        using var setup = f.CreateDb();
        var actor = f.Actor("patient1");
        var original = await f.Appointments(setup, actor).BookAsync(ConcurrencyTests.Request(f));
        var target = f.Slot(day: 2);
        var fault = new TimeoutAfterWrite(target.Id);
        // Disable retries only in this fault-injection context to observe the first real SQL timeout.
        using var broken = new HospitalDbContext(new DbContextOptionsBuilder<HospitalDbContext>()
            .UseSqlServer(setup.Database.GetConnectionString()).AddInterceptors(fault).Options);
        var error = await Assert.ThrowsAsync<DbUpdateException>(() => f.Appointments(broken, actor).RescheduleAsync(original.Id,
            new RescheduleAppointmentRequest { TimeSlotId = target.Id, SpecialtyId = original.SpecialtyId, RowVersion = original.RowVersion }));
        Assert.Equal(-2, Assert.IsType<SqlException>(error.InnerException).Number);
        Assert.True(fault.Injected);
        using var verify = f.CreateDb();
        var saved = (await verify.Appointments.FindAsync(original.Id))!;
        Assert.Equal(original.TimeSlotId, saved.TimeSlotId); Assert.Equal(original.RowVersion, saved.RowVersion);
        var untouched = (await verify.TimeSlots.FindAsync(target.Id))!;
        Assert.Null(untouched.ClosedReason); Assert.Equal(TimeSlotStatus.Open, untouched.Status);
        Assert.Equal(TimeSlotStatus.Occupied, (await verify.TimeSlots.FindAsync(original.TimeSlotId))!.Status);
        Assert.Equal(1, await verify.AppointmentStatusHistories.CountAsync(h => h.AppointmentId == original.Id));
        Assert.False(await verify.AuditLogs.AnyAsync(a => a.EntityId == original.Id.ToString() && a.Action == "RESCHEDULE_APPOINTMENT"));
        Assert.False(await verify.NotificationOutbox.AnyAsync(n => n.AppointmentId == original.Id && n.Type == NotificationType.Rescheduled));
    }

    private sealed class TimeoutAfterWrite(Guid slotId) : DbCommandInterceptor
    {
        public bool Injected { get; private set; }
        public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(DbCommand command,
            CommandEventData eventData, InterceptionResult<DbDataReader> result, CancellationToken cancellationToken = default)
        {
            if (!Injected && command.CommandText.Contains("UPDATE [Appointments]", StringComparison.Ordinal))
            {
                Injected = true; command.CommandTimeout = 1;
                // The probe writes inside the application's transaction before the real timeout.
                command.CommandText = $"UPDATE [TimeSlots] SET [ClosedReason] = N'Rollback timeout probe' WHERE [Id] = '{slotId:D}'; WAITFOR DELAY '00:00:03';\n" + command.CommandText;
            }
            return base.ReaderExecutingAsync(command, eventData, result, cancellationToken);
        }
    }
}
