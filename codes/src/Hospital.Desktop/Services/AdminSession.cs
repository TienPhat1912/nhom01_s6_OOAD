using System.Net;
using Hospital.Application.Common.Models;
using Hospital.Application.DTOs.Auth;
using Hospital.Client;
using Hospital.Domain.Enums;

namespace Hospital.Desktop.Services;

public sealed class AdminSession(HospitalApiClient api, AuthResponseDto auth)
{
    private string token = auth.Role == "Admin" ? auth.Token : throw new ArgumentException("Admin session required.");
    public Guid UserId { get; } = auth.UserId;
    public string FullName { get; set; } = auth.FullName;
    public bool Expired { get; private set; }
    public string EndMessage { get; private set; } = "Bạn đã đăng xuất khỏi không gian quản trị.";
    public void End(string message) { token = ""; Expired = true; EndMessage = message; }
    public Task<T> GetAsync<T>(string path, CancellationToken ct) => SendAsync<T>(HttpMethod.Get, path, null, ct);
    public async Task<T> SendAsync<T>(HttpMethod method, string path, object? body, CancellationToken ct)
    {
        if (Expired) throw new HospitalApiException(HttpStatusCode.Unauthorized, EndMessage);
        try { return await api.SendAsync<T>(method, path, body, token, ct); }
        catch (HospitalApiException ex) when (ex.Status is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
        { End("Phiên quản trị không còn hiệu lực. Vui lòng đăng nhập lại."); throw; }
    }
    public async Task<List<T>> AllAsync<T>(string path, CancellationToken ct)
    {
        var result = new List<T>();
        for (var page = 1; ; page++)
        {
            var batch = await GetAsync<PagedResult<T>>($"{path}{(path.Contains('?') ? '&' : '?')}page={page}&pageSize=100", ct);
            result.AddRange(batch.Items);
            if (batch.Items.Count == 0 || result.Count >= batch.TotalCount) return result;
        }
    }
    public static string Role(UserRole role) => role switch
    { UserRole.Admin => "Quản trị viên", UserRole.Doctor => "Bác sĩ", UserRole.Staff => "Nhân viên", _ => "Bệnh nhân" };
    public static string Error(HospitalApiException ex) => ex.Message
        + (ex.Errors.Count == 0 ? "" : "\n" + string.Join(" · ", ex.Errors))
        + (ex.TraceId == null ? "" : "\nMã hỗ trợ: " + ex.TraceId);
    public static DateOnly Today => DateOnly.FromDateTime(DateTime.UtcNow.AddHours(7));
}
