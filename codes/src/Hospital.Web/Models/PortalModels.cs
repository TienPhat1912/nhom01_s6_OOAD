using Hospital.Application.Common.Models;
using Hospital.Application.DTOs.Appointments;
using Hospital.Application.DTOs.Auth;
using Hospital.Domain.Enums;

namespace Hospital.Web.Models;

public class LoginModel : LoginRequestDto
{
    public string? ReturnUrl { get; set; }
}

public class ProfileModel : UpdateProfileRequestDto
{
    [Microsoft.AspNetCore.Mvc.ModelBinding.Validation.ValidateNever]
    public string Role { get; set; } = "";
    [Microsoft.AspNetCore.Mvc.ModelBinding.Validation.ValidateNever]
    public string Username { get; set; } = "";
}

public record UiMessage(string Title, string Message, string Tone = "info", string? TraceId = null);
public record EmptyState(string Title, string Message, string Icon = "calendar");
public record Pagination(int Page, int TotalCount, int PageSize);
public record StatCard(string Label, int Value, string Icon, string Description);
public class DashboardModel
{
    public List<StatCard> Stats { get; set; } = [];
    public List<AppointmentDto> Appointments { get; set; } = [];
    public string Period { get; set; } = "";
}
public record AppointmentListModel(PagedResult<AppointmentDto> Page, string? Keyword, AppointmentStatus? Status,
    DateOnly? FromDate = null, DateOnly? ToDate = null, Guid? DoctorId = null, bool Descending = false);

public static class DisplayText
{
    public static string Role(string? role) => role switch
    { "Patient" => "Bệnh nhân", "Doctor" => "Bác sĩ", "Staff" => "Nhân viên tiếp nhận", "Admin" => "Quản trị viên", _ => "Người dùng" };
    public static string Status(AppointmentStatus status) => status switch
    { AppointmentStatus.Confirmed => "Đã xác nhận", AppointmentStatus.CheckedIn => "Đã tiếp nhận",
      AppointmentStatus.Cancelled => "Đã hủy", AppointmentStatus.NoShow => "Không đến", _ => "Không xác định" };
    public static DateOnly Today => DateOnly.FromDateTime(DateTime.UtcNow.AddHours(7));
    public static string Initials(string? name) => string.Concat((name ?? "AT").Split(' ', StringSplitOptions.RemoveEmptyEntries).TakeLast(2).Select(x => x[0]));
}
