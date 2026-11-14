using Hospital.Application.DTOs.Catalog;
using Hospital.Application.DTOs.Schedules;
using Hospital.Desktop.Design;
using Hospital.Desktop.Services;

namespace Hospital.Desktop.Forms;

public sealed class ScheduleEditorForm : EditorForm
{
    private readonly WorkingScheduleDto? existing;
    private Guid? doctorId;
    private readonly UiButton doctor;
    private readonly DateTimePicker date, start, end;
    private readonly TextBox note;
    private readonly Label preview;
    public ScheduleEditorForm(AdminSession session, IDesktopDialogs dialogs, WorkingScheduleDto? existing = null, DoctorDto? selectedDoctor = null)
        : base(session, dialogs, existing == null ? "Thêm lịch làm việc" : "Điều chỉnh lịch làm việc",
            "Ca trực gồm các khung 30 phút, không chồng lấn hoặc bắt đầu trong quá khứ. Thay giờ chỉ được lưu khi không còn lịch hẹn có hiệu lực bị ảnh hưởng.")
    {
        this.existing = existing; doctorId = existing?.DoctorId ?? selectedDoctor?.Id;
        doctor = AddField("DoctorId", "Bác sĩ", new UiButton { Text = existing?.DoctorName ?? selectedDoctor?.FullName ?? "Chọn bác sĩ", Secondary = true, Enabled = existing == null }, wide: true);
        doctor.Click += (_, _) =>
        {
            using var picker = new DoctorPickerForm(Session);
            if (picker.ShowDialog(this) == DialogResult.OK) { doctorId = picker.SelectedDoctor!.Id; doctor.Text = picker.SelectedDoctor.FullName; }
            else if (Session.Expired) { DialogResult = DialogResult.Abort; Close(); }
        };
        date = AddField("WorkDate", "Ngày làm việc", new DateTimePicker { Format = DateTimePickerFormat.Custom, CustomFormat = "dd/MM/yyyy", Value = (existing?.WorkDate ?? AdminSession.Today.AddDays(1)).ToDateTime(TimeOnly.MinValue) });
        preview = AddField("Preview", "Khung giờ dự kiến", UiTheme.Label("", 11, true));
        start = AddField("StartTime", "Giờ bắt đầu", TimePicker(existing?.StartTime ?? new TimeOnly(8, 0)));
        end = AddField("EndTime", "Giờ kết thúc", TimePicker(existing?.EndTime ?? new TimeOnly(11, 30)));
        note = TextField("Note", "Ghi chú ca trực", 500, existing?.Note, multiline: true);
        start.ValueChanged += (_, _) => Preview(); end.ValueChanged += (_, _) => Preview(); Preview();
    }
    private static DateTimePicker TimePicker(TimeOnly time) => new() { Format = DateTimePickerFormat.Custom, CustomFormat = "HH:mm", ShowUpDown = true, Value = DateTime.Today.Add(time.ToTimeSpan()) };
    private void Preview()
    { var minutes = (end.Value.TimeOfDay - start.Value.TimeOfDay).TotalMinutes; preview.Text = minutes > 0 && minutes % 30 == 0 ? $"{minutes / 30:0} khung × 30 phút" : "Chọn khoảng giờ hợp lệ"; }
    protected override object CreateRequest() => existing == null ? new CreateWorkingScheduleRequestDto
    { DoctorId = doctorId ?? Guid.Empty, WorkDate = DateOnly.FromDateTime(date.Value), StartTime = TimeOnly.FromDateTime(start.Value), EndTime = TimeOnly.FromDateTime(end.Value), Note = note.Text.Trim() }
    : new UpdateWorkingScheduleRequestDto
    { WorkDate = DateOnly.FromDateTime(date.Value), StartTime = TimeOnly.FromDateTime(start.Value), EndTime = TimeOnly.FromDateTime(end.Value), Note = note.Text.Trim() };
    protected override bool ValidateRequest(object request)
    {
        if (!doctorId.HasValue) return Error("DoctorId", "Chọn bác sĩ phụ trách ca trực.");
        var minutes = (end.Value.TimeOfDay - start.Value.TimeOfDay).TotalMinutes;
        if (minutes <= 0 || minutes % 30 != 0) return Error("EndTime", "Giờ kết thúc phải sau giờ bắt đầu và tạo đủ các khung 30 phút.");
        if (date.Value.Date + start.Value.TimeOfDay <= DateTime.UtcNow.AddHours(7)) return Error("WorkDate", "Không tạo hoặc sửa ca trực đã bắt đầu.");
        return true;
    }
    protected override string Confirmation => $"Lưu ca trực của {doctor.Text} ngày {date.Value:dd/MM/yyyy}, {start.Value:HH:mm}–{end.Value:HH:mm}? Hệ thống sẽ kiểm tra lịch bị ảnh hưởng trước khi thay đổi khung giờ.";
    protected override async Task<object?> PersistAsync(object request) => await Session.SendAsync<WorkingScheduleDto>(existing == null ? HttpMethod.Post : HttpMethod.Put,
        existing == null ? "api/schedules" : $"api/schedules/{existing.Id}", request, Lifetime.Token);
}

public sealed class CloseSlotForm : EditorForm
{
    private readonly TimeSlotDto slot;
    private readonly TextBox reason;
    public CloseSlotForm(AdminSession session, IDesktopDialogs dialogs, TimeSlotDto slot) : base(session, dialogs, "Đóng khung giờ",
        $"{slot.DoctorName} · {slot.SlotDate:dd/MM/yyyy} · {slot.StartTime:HH:mm}–{slot.EndTime:HH:mm}. Lịch hẹn có hiệu lực phải được xử lý trước.")
    { this.slot = slot; reason = TextField("Reason", "Lý do đóng (bắt buộc)", 500, multiline: true); }
    protected override object CreateRequest() => new CloseTimeSlotRequestDto { Reason = reason.Text.Trim() };
    protected override string Confirmation => $"Đóng khung giờ {slot.StartTime:HH:mm}–{slot.EndTime:HH:mm} ngày {slot.SlotDate:dd/MM/yyyy}? Khung giờ sẽ ngừng nhận lịch mới và được lưu trong lịch sử.";
    protected override async Task<object?> PersistAsync(object request) => await Session.SendAsync<object>(HttpMethod.Post, $"api/timeslots/{slot.Id}/close", request, Lifetime.Token);
}
