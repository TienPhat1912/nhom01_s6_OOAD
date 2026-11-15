using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.RegularExpressions;
using Hospital.Application.Common.Models;
using Hospital.Application.DTOs.Appointments;
using Hospital.Application.DTOs.Auth;
using Hospital.Client;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace Hospital.WebTests;

public sealed class TestBackend : HttpMessageHandler
{
    public bool Revoked { get; set; }
    public bool Unavailable { get; set; }
    public string? LastProfileBody { get; private set; }
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var path = request.RequestUri!.AbsolutePath;
        if (Unavailable) return Response(HttpStatusCode.ServiceUnavailable, new { success = false, message = "Không thể kết nối hệ thống.", traceId = "trace-ui-outage" });
        if (path == "/api/auth/login")
        {
            var login = (await request.Content!.ReadFromJsonAsync<LoginRequestDto>(cancellationToken))!;
            if (login.Password != "Correct@123") return Response(HttpStatusCode.Unauthorized, new { success = false, message = "Thông tin đăng nhập không chính xác." });
            return Response(HttpStatusCode.OK, ApiResponse<AuthResponseDto>.Succeeded(new()
            { Token = "private-test-token-" + login.Username, UserId = Guid.NewGuid(), FullName = "Người dùng kiểm thử", Username = login.Username,
                Role = login.Username == "doctor" ? "Doctor" : "Patient", ExpiresAt = DateTime.UtcNow.AddHours(1) }));
        }
        if (Revoked) return Response(HttpStatusCode.Unauthorized, new { success = false, message = "Phiên hết hiệu lực." });
        if (path == "/api/auth/profile")
        {
            if (request.Method == HttpMethod.Put) LastProfileBody = await request.Content!.ReadAsStringAsync(cancellationToken);
            return Response(HttpStatusCode.OK, ApiResponse<UserProfileDto>.Succeeded(new()
            { FullName = "Người dùng kiểm thử", Username = "patient", Role = request.Headers.Authorization?.Parameter?.EndsWith("doctor") == true ? "Doctor" : "Patient",
                Email = "patient@example.test", IsActive = true, DateOfBirth = new DateOnly(1990, 1, 1) }));
        }
        if (path.StartsWith("/api/appointments")) return Response(HttpStatusCode.OK, ApiResponse<PagedResult<AppointmentDto>>.Succeeded(new([], 0, 1, 10)));
        return Response(HttpStatusCode.NotFound, new { success = false, message = "Không tìm thấy." });
    }
    private static HttpResponseMessage Response(HttpStatusCode status, object body) => new(status) { Content = JsonContent.Create(body, options: HospitalApiClient.Json) };
}

public sealed class PortalFactory : WebApplicationFactory<Program>
{
    public TestBackend Backend { get; } = new();
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureServices(services => services.AddHttpClient<HospitalApiClient>().ConfigurePrimaryHttpMessageHandler(() => Backend));
    }
    public HttpClient Browser() => CreateClient(new() { BaseAddress = new Uri("https://localhost"), AllowAutoRedirect = false });
}

public class PortalTests
{
    private static async Task<string> Token(HttpClient client, string path)
    {
        var html = await client.GetStringAsync(path);
        return WebUtility.HtmlDecode(Regex.Match(html, "name=\"__RequestVerificationToken\"[^>]*value=\"([^\"]+)\"").Groups[1].Value);
    }
    private static async Task<HttpResponseMessage> Login(HttpClient client, string username = "patient", string? returnUrl = null)
    {
        var token = await Token(client, "/Account/Login");
        return await client.PostAsync("/Account/Login", new FormUrlEncodedContent(new Dictionary<string, string>
        { ["Username"] = username, ["Password"] = "Correct@123", ["ReturnUrl"] = returnUrl ?? "", ["__RequestVerificationToken"] = token }));
    }

    [Fact]
    public async Task LoginRequiresAntiforgeryAndNeverRedirectsToExternalSite()
    {
        using var factory = new PortalFactory(); using var client = factory.Browser();
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsync("/Account/Login", new FormUrlEncodedContent(new Dictionary<string, string>
        { ["Username"] = "patient", ["Password"] = "Correct@123" }))).StatusCode);
        using var response = await Login(client, returnUrl: "https://example.org/steal");
        Assert.Equal("/Dashboard", response.Headers.Location!.OriginalString);
        Assert.Contains(response.Headers.GetValues("Set-Cookie"), c => c.Contains("Hospital.Session") && c.Contains("httponly", StringComparison.OrdinalIgnoreCase));
        var html = await client.GetStringAsync("/Dashboard");
        Assert.DoesNotContain("private-test-token", html);
        Assert.Contains("lang=\"vi\"", html);
    }

    [Fact]
    public async Task RoleGuardsAndBackendRevocationApplyToWebSession()
    {
        using var factory = new PortalFactory(); using var client = factory.Browser();
        await Login(client, "doctor");
        using var forbidden = await client.GetAsync("/Notifications");
        Assert.Contains("AccessDenied", forbidden.Headers.Location!.OriginalString);
        factory.Backend.Revoked = true;
        using var expired = await client.GetAsync("/Dashboard");
        Assert.Contains("/Account/Login", expired.Headers.Location!.OriginalString);
    }

    [Fact]
    public async Task OutageShowsTraceAndRetainsSessionForRecovery()
    {
        using var factory = new PortalFactory(); using var client = factory.Browser();
        await Login(client); factory.Backend.Unavailable = true;
        using var response = await client.GetAsync("/Dashboard");
        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        var html = WebUtility.HtmlDecode(await response.Content.ReadAsStringAsync());
        Assert.Contains("trace-ui-outage", html); Assert.Contains("Thử lại", html); Assert.DoesNotContain("stat-value", html);
        factory.Backend.Unavailable = false;
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/Dashboard")).StatusCode);
    }

    [Fact]
    public async Task ProfileFormValidatesAndSendsOnlyEditableFields()
    {
        using var factory = new PortalFactory(); using var client = factory.Browser(); await Login(client);
        var token = await Token(client, "/Account/Profile");
        var data = new Dictionary<string, string> { ["FullName"] = "Tên đã cập nhật", ["Email"] = "invalid",
            ["Role"] = "Admin", ["__RequestVerificationToken"] = token };
        using var invalid = await client.PostAsync("/Account/Profile", new FormUrlEncodedContent(data));
        Assert.Equal(HttpStatusCode.OK, invalid.StatusCode); Assert.Null(factory.Backend.LastProfileBody);
        Assert.Contains("validation-summary-errors", await invalid.Content.ReadAsStringAsync());
        data["Email"] = "valid@example.test";
        using var saved = await client.PostAsync("/Account/Profile", new FormUrlEncodedContent(data));
        Assert.Equal(HttpStatusCode.Redirect, saved.StatusCode);
        Assert.NotNull(factory.Backend.LastProfileBody);
        using var json = JsonDocument.Parse(factory.Backend.LastProfileBody!);
        Assert.False(json.RootElement.TryGetProperty("role", out _));
        Assert.Equal("valid@example.test", json.RootElement.GetProperty("email").GetString());
    }

    [Fact]
    public async Task LogoutRequiresPostAndCsrfToken()
    {
        using var factory = new PortalFactory(); using var client = factory.Browser(); await Login(client);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync("/Account/Logout")).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsync("/Account/Logout", new FormUrlEncodedContent([]))).StatusCode);
        var token = await Token(client, "/Dashboard");
        using var logout = await client.PostAsync("/Account/Logout", new FormUrlEncodedContent(new Dictionary<string, string> { ["__RequestVerificationToken"] = token }));
        Assert.Equal(HttpStatusCode.Redirect, logout.StatusCode);
        Assert.Contains("/Account/Login", (await client.GetAsync("/Dashboard")).Headers.Location!.OriginalString);
    }

    [Fact]
    public async Task ComponentGalleryIsNotPublishedOutsideDevelopment()
    {
        using var factory = new PortalFactory(); using var client = factory.Browser();
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync("/Design")).StatusCode);
    }
}
