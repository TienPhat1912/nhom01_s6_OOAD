using System.ComponentModel.DataAnnotations;
using System.Net;
using System.Net.Http.Json;
using Hospital.Application.Common.Exceptions;
using Hospital.Application.Common.Models;
using Hospital.Application.DTOs.Admin;
using Hospital.Application.DTOs.Catalog;
using Hospital.Domain.Enums;
using Hospital.Infrastructure.Data;
using Hospital.Infrastructure.Services;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.Configuration;

namespace Hospital.IntegrationTests;

public class SingleSpecialtyTests
{
    [Fact]
    public async Task DoctorRequiresOneExistingActiveSpecialtyAndRejectsLegacyPayload()
    {
        using var f = new TestFixture(); using var db = f.CreateDb(); var admin = f.Admin(db);
        await Assert.ThrowsAsync<ValidationException>(() => admin.SaveDoctorAsync(null, new() { FullName = "No specialty" }));
        await Assert.ThrowsAsync<BusinessRuleException>(() => admin.SaveDoctorAsync(null,
            new() { FullName = "Unknown specialty", SpecialtyId = int.MaxValue }));
        var inactive = await admin.SaveSpecialtyAsync(null, new() { Code = "OFF", Name = "Inactive", IsActive = false });
        await Assert.ThrowsAsync<BusinessRuleException>(() => admin.SaveDoctorAsync(null,
            new() { FullName = "Inactive specialty", SpecialtyId = inactive.Id }));

        using var client = await f.ClientAsync("admin");
        using var response = await client.PostAsJsonAsync("/api/admin/doctors",
            new { FullName = "Legacy payload", SpecialtyIds = new[] { f.Specialty(), f.Specialty("NHI") }, PrimarySpecialtyId = f.Specialty() });
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        using var verify = f.CreateDb();
        Assert.Equal(2, await verify.Doctors.CountAsync());
    }

    [Fact]
    public async Task ChangingSpecialtyBlocksAffectedBookingsThenPreservesCancelledHistory()
    {
        using var f = new TestFixture(); using var db = f.CreateDb(); var admin = f.Admin(db);
        var doctorId = f.Actor("doctor1").DoctorId!.Value;
        var oldSpecialty = f.Specialty(); var newSpecialty = f.Specialty("NHI");
        var request = new SaveDoctorRequest { FullName = "Doctor one", SpecialtyId = newSpecialty };
        Assert.Equal("ACTIVE_APPOINTMENTS_EXIST", (await Assert.ThrowsAsync<BusinessRuleException>(
            () => admin.SaveDoctorAsync(doctorId, request))).ErrorCode);
        var staff = f.Appointments(db, f.Actor("staff1"));
        var appointment = Assert.Single((await staff.ListAsync(new() { DoctorId = doctorId })).Items);
        await staff.CancelAsync(appointment.Id, new() { RowVersion = appointment.RowVersion, Reason = "Change department" });
        var changed = await admin.SaveDoctorAsync(doctorId, request);
        Assert.Equal(newSpecialty, changed.SpecialtyId);

        using var verify = f.CreateDb();
        var saved = await verify.Appointments.Include(a => a.StatusHistories).SingleAsync(a => a.Id == appointment.Id);
        Assert.Equal(oldSpecialty, saved.SpecialtyId);
        Assert.Equal(AppointmentStatus.Cancelled, saved.Status);
        Assert.NotEmpty(saved.StatusHistories);
        var catalog = new CatalogService(verify, f.Actor("patient1"), f.Clock);
        Assert.DoesNotContain((await catalog.GetDoctorsAsync(oldSpecialty)).Items, d => d.Id == doctorId);
        Assert.Contains((await catalog.GetDoctorsAsync(newSpecialty)).Items, d => d.Id == doctorId);
        var slot = f.Slot(hour: 10);
        var patient = f.Appointments(verify, f.Actor("patient1"));
        await Assert.ThrowsAsync<BusinessRuleException>(() => patient.BookAsync(new()
            { TimeSlotId = slot.Id, SpecialtyId = oldSpecialty, RequestKey = Guid.NewGuid() }));
        var booked = await patient.BookAsync(new() { TimeSlotId = slot.Id, SpecialtyId = newSpecialty, RequestKey = Guid.NewGuid() });
        Assert.Equal(newSpecialty, booked.SpecialtyId);
    }

    [Fact]
    public async Task CatalogueAndProfileExposeSingleSpecialtyAndHideInactiveDoctors()
    {
        using var f = new TestFixture(); using var client = await f.ClientAsync("patient1");
        var doctorId = f.Actor("doctor1").DoctorId!.Value;
        var detail = (await client.GetFromJsonAsync<ApiResponse<DoctorDetailDto>>($"/api/doctors/{doctorId}", TestFixture.Json))!.Data!;
        Assert.Equal(f.Specialty(), detail.SpecialtyId);
        Assert.Equal(detail.SpecialtyId, detail.Specialty.Id);
        using var db = f.CreateDb();
        var profile = await new AuthService(db, f.Hasher, new JwtTokenGenerator(
            new Microsoft.Extensions.Configuration.ConfigurationBuilder().AddInMemoryCollection(
                new Dictionary<string, string?> { ["JwtSettings:Secret"] = TestFixture.JwtKey }).Build()))
            .GetProfileAsync(f.Actor("doctor1").UserId!.Value);
        Assert.Equal(detail.Specialty.Name, profile.SpecialtyName);
        (await db.Specialties.FindAsync(detail.SpecialtyId))!.IsActive = false;
        await db.SaveChangesAsync();
        using var hidden = await client.GetAsync($"/api/doctors/{doctorId}");
        Assert.Equal(HttpStatusCode.NotFound, hidden.StatusCode);
    }

    [SqlServerFact]
    public async Task MigrationBackfillsSingleSpecialtyAndPreservesAppointmentsInBothDirections()
    {
        using var f = new TestFixture(Environment.GetEnvironmentVariable("HOSPITAL_TEST_SQLSERVER")!);
        using var db = f.CreateDb();
        var doctors = await db.Doctors.AsNoTracking().ToDictionaryAsync(d => d.Id, d => d.SpecialtyId);
        var history = await db.Appointments.AsNoTracking().Select(a => new { a.Id, a.SpecialtyId, a.TimeSlotId, a.Status }).OrderBy(a => a.Id).ToListAsync();
        var migrator = db.GetService<IMigrator>();
        await migrator.MigrateAsync("20261016143621_Phase3Backend");
        Assert.Equal(doctors.Count, await db.Database.SqlQueryRaw<int>("SELECT COUNT(*) AS [Value] FROM DoctorSpecialties WHERE IsPrimary = 1").SingleAsync());
        await migrator.MigrateAsync();
        Assert.Equal(doctors.OrderBy(d => d.Key), (await db.Doctors.AsNoTracking().ToDictionaryAsync(d => d.Id, d => d.SpecialtyId)).OrderBy(d => d.Key));
        Assert.Equal(history, await db.Appointments.AsNoTracking().Select(a => new { a.Id, a.SpecialtyId, a.TimeSlotId, a.Status }).OrderBy(a => a.Id).ToListAsync());
        // The new FK must keep a referenced specialty even when there is no appointment.
        await Assert.ThrowsAsync<SqlException>(() => db.Database.ExecuteSqlInterpolatedAsync(
            $"DELETE FROM Specialties WHERE Id = {f.Specialty("NHI")}"));
    }

    [SqlServerTheory]
    [InlineData(0)]
    [InlineData(2)]
    public async Task MigrationRejectsMissingOrMultipleLegacySpecialtiesWithoutPartialChanges(int links)
    {
        using var f = new TestFixture(Environment.GetEnvironmentVariable("HOSPITAL_TEST_SQLSERVER")!);
        using var db = f.CreateDb();
        var doctorId = f.Actor("doctor1").DoctorId!.Value;
        var extraSpecialty = f.Specialty("NHI");
        var migrator = db.GetService<IMigrator>();
        await migrator.MigrateAsync("20261016143621_Phase3Backend");
        if (links == 0)
            await db.Database.ExecuteSqlInterpolatedAsync($"DELETE FROM DoctorSpecialties WHERE DoctorId = {doctorId}");
        else
            await db.Database.ExecuteSqlInterpolatedAsync($"INSERT INTO DoctorSpecialties (DoctorId, SpecialtyId, IsPrimary) VALUES ({doctorId}, {extraSpecialty}, 0)");
        var error = await Assert.ThrowsAsync<SqlException>(() => migrator.MigrateAsync());
        Assert.Equal(51001, error.Number);
        Assert.Contains("OOAD_A2", error.Message);
        Assert.Equal(links, await db.Database.SqlQuery<int>($"SELECT COUNT(*) AS [Value] FROM DoctorSpecialties WHERE DoctorId = {doctorId}").SingleAsync());
        Assert.Equal(0, await db.Database.SqlQueryRaw<int>("SELECT COUNT(*) AS [Value] FROM sys.columns WHERE object_id = OBJECT_ID('Doctors') AND name = 'SpecialtyId'").SingleAsync());
        Assert.DoesNotContain("20261118014141_SingleDoctorSpecialty", await db.Database.GetAppliedMigrationsAsync());
    }
}
