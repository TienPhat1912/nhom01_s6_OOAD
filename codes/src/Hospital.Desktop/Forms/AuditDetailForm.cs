using System.Text.Json;
using Hospital.Application.DTOs.Admin;
using Hospital.Desktop.Design;

namespace Hospital.Desktop.Forms;

public sealed class AuditDetailForm : Form
{
    public AuditDetailForm(AuditLogDto log)
    {
        Text = "Chi tiết nhật ký"; StartPosition = FormStartPosition.CenterParent; Font = UiTheme.Body();
        AutoScaleMode = AutoScaleMode.Dpi; ClientSize = new Size(900, 660); MinimumSize = new Size(720, 560); BackColor = Color.White;
        var root = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(24), RowCount = 3, ColumnCount = 2 };
        root.ColumnStyles.Add(new(SizeType.Percent, 50)); root.ColumnStyles.Add(new(SizeType.Percent, 50));
        root.RowStyles.Add(new(SizeType.Absolute, 120)); root.RowStyles.Add(new(SizeType.Absolute, 34)); root.RowStyles.Add(new(SizeType.Percent, 100));
        var summary = UiTheme.Label($"{ActionLabel(log.Action)} · {EntityLabel(log.EntityType)}\n{log.CreatedAt.AddHours(7):dd/MM/yyyy HH:mm:ss} (UTC+7) · {log.UserName ?? "Hệ thống"}\nMã bản ghi: {log.EntityId ?? "—"}", 13, true);
        root.Controls.Add(summary, 0, 0); root.SetColumnSpan(summary, 2);
        root.Controls.Add(UiTheme.Label("Trước thay đổi", 11, true), 0, 1); root.Controls.Add(UiTheme.Label("Sau thay đổi", 11, true), 1, 1);
        root.Controls.Add(JsonBox(log.OldValues, "Trước thay đổi"), 0, 2); root.Controls.Add(JsonBox(log.NewValues, "Sau thay đổi"), 1, 2); Controls.Add(root);
    }
    private static TextBox JsonBox(string? value, string label)
    {
        var text = value ?? "Không có dữ liệu.";
        if (value != null) { try { using var json = JsonDocument.Parse(value); text = JsonSerializer.Serialize(json.RootElement, new JsonSerializerOptions { WriteIndented = true, Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping }); } catch (JsonException) { } }
        return new TextBox { Text = text, Multiline = true, ReadOnly = true, ScrollBars = ScrollBars.Both, WordWrap = false, Dock = DockStyle.Fill, AccessibleName = label };
    }
    public static string EntityLabel(string value) => value switch { "User" => "Tài khoản", "Doctor" => "Bác sĩ", "Specialty" => "Chuyên khoa", "WorkingSchedule" => "Ca làm việc", "TimeSlot" => "Khung giờ", "Appointment" => "Lịch hẹn", _ => value };
    public static string ActionLabel(string value) => value switch
    {
        "CREATE_USER" or "CREATE_DOCTOR" or "CREATE_SPECIALTY" or "CREATE_SCHEDULE" => "Tạo mới",
        "UPDATE_USER" or "UPDATE_DOCTOR" or "UPDATE_SPECIALTY" or "UPDATE_SCHEDULE" => "Cập nhật",
        "CHANGE_ROLE" => "Đổi vai trò", "CLOSE_TIMESLOT" => "Đóng khung giờ", "CHANGE_PASSWORD" => "Đổi mật khẩu",
        "UPDATE_PROFILE" => "Sửa hồ sơ", "REGISTER_ACCOUNT" => "Đăng ký", "BOOK_APPOINTMENT" => "Đặt lịch",
        "CANCEL_APPOINTMENT" => "Hủy lịch", "CHECK_IN" => "Tiếp nhận", "MARK_NO_SHOW" => "Vắng mặt", "RESCHEDULE_APPOINTMENT" => "Đổi lịch", _ => value
    };
}
