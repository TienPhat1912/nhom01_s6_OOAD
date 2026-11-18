using System.Net;
using System.Net.Http.Json;
using Hospital.Application.Common.Models;
using Hospital.Application.DTOs.Admin;
using Hospital.Application.DTOs.Catalog;

namespace Hospital.IntegrationTests;

public class AdminDesktopApiTests
{
    [Fact]
    public async Task UserDetailRequiresAdminAndReturnsCurrentProfileWithoutSecrets()
    {
        using var fixture = new TestFixture();
        using var admin = await fixture.ClientAsync("admin");
        var id = fixture.Actor("doctor1").UserId;
        var response = await admin.GetAsync($"/api/admin/users/{id}");
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<UserDto>>(TestFixture.Json);
        Assert.Equal(fixture.Actor("doctor1").DoctorId, body!.Data!.DoctorId);
        Assert.Equal("doctor1", body.Data.Username);
        Assert.DoesNotContain("password", await response.Content.ReadAsStringAsync(), StringComparison.OrdinalIgnoreCase);
        Assert.Equal(HttpStatusCode.NotFound, (await admin.GetAsync($"/api/admin/users/{Guid.NewGuid()}")).StatusCode);
        foreach (var actor in new string?[] { null, "patient1", "doctor1", "staff1" })
        {
            using var client = await fixture.ClientAsync(actor);
            Assert.Equal(actor == null ? HttpStatusCode.Unauthorized : HttpStatusCode.Forbidden, (await client.GetAsync($"/api/admin/users/{id}")).StatusCode);
        }
    }

    [Fact]
    public async Task CatalogExposesAccountLinkOnlyToAdminAndReturnsSingleSpecialty()
    {
        using var fixture = new TestFixture(); using var admin = await fixture.ClientAsync("admin");
        var doctorId = fixture.Actor("doctor1").DoctorId;
        var doctors = (await admin.GetFromJsonAsync<ApiResponse<PagedResult<DoctorDto>>>("/api/doctors?onlyActive=false", TestFixture.Json))!.Data!;
        Assert.Equal(fixture.Actor("doctor1").UserId, doctors.Items.Single(d => d.Id == doctorId).LinkedUserId);
        var detail = (await admin.GetFromJsonAsync<ApiResponse<DoctorDetailDto>>($"/api/doctors/{doctorId}", TestFixture.Json))!.Data!;
        Assert.Equal(fixture.Specialty(), detail.SpecialtyId);
        foreach (var actor in new[] { "patient1", "staff1" })
        {
            using var client = await fixture.ClientAsync(actor);
            var visible = (await client.GetFromJsonAsync<ApiResponse<PagedResult<DoctorDto>>>("/api/doctors", TestFixture.Json))!.Data!;
            Assert.All(visible.Items, d => Assert.Null(d.LinkedUserId));
        }
        using var doctor = await fixture.ClientAsync("doctor1");
        Assert.Equal(HttpStatusCode.Forbidden, (await doctor.GetAsync("/api/doctors")).StatusCode);
    }

    [Fact]
    public async Task AuditIncludesActorAndFiltersWithoutExposingSecrets()
    {
        using var fixture = new TestFixture(); using var admin = await fixture.ClientAsync("admin");
        (await admin.PostAsJsonAsync("/api/admin/specialties", new SaveSpecialtyRequest { Code = "DESKTOP", Name = "Desktop specialty", IsActive = true })).EnsureSuccessStatusCode();
        var logs = (await admin.GetFromJsonAsync<ApiResponse<PagedResult<AuditLogDto>>>("/api/admin/audit-logs?entityType=Specialty", TestFixture.Json))!.Data!;
        var entry = Assert.Single(logs.Items);
        Assert.Equal(fixture.Actor("admin").UserId, entry.UserId); Assert.False(string.IsNullOrWhiteSpace(entry.UserName));
        Assert.Equal("CREATE_SPECIALTY", entry.Action); Assert.Contains("DESKTOP", entry.NewValues!);
        using var staff = await fixture.ClientAsync("staff1");
        Assert.Equal(HttpStatusCode.Forbidden, (await staff.GetAsync("/api/admin/audit-logs")).StatusCode);
    }
}
