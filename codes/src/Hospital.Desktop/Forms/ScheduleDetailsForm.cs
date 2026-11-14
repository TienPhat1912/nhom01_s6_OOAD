using Hospital.Application.DTOs.Schedules;
using Hospital.Client;
using Hospital.Desktop.Design;
using Hospital.Desktop.Services;

namespace Hospital.Desktop.Forms;

public sealed class ScheduleDetailsForm : Form
{
    private readonly AdminSession session;
    private readonly IDesktopDialogs dialogs;
    private readonly Guid scheduleId;
    private readonly CancellationTokenSource lifetime = new();
    private readonly UiDataGrid grid = new();
    private readonly UiBanner banner = new();
    private readonly Label heading = UiTheme.Label("", 20, true);
    private readonly UiButton closeSlot = new() { Text = "Đóng khung giờ", Width = 170 };
    private readonly UiButton edit = new() { Text = "Điều chỉnh ca", Secondary = true };
    private readonly UiButton refresh = new() { Text = "Làm mới", Secondary = true };
    private WorkingScheduleDto? schedule;
    private bool loading;

    public ScheduleDetailsForm(AdminSession session, IDesktopDialogs dialogs, Guid scheduleId)
    {
        this.session = session; this.dialogs = dialogs; this.scheduleId = scheduleId;
        Text = "Chi tiết ca làm việc"; StartPosition = FormStartPosition.CenterParent; Font = UiTheme.Body();
        AutoScaleMode = AutoScaleMode.Dpi; ClientSize = new Size(960, 640); MinimumSize = new Size(860, 560); BackColor = Color.White;
        var root = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(24), ColumnCount = 1, RowCount = 5 };
        foreach (var height in new[] { 52, 50, 80 }) root.RowStyles.Add(new(SizeType.Absolute, height));
        root.RowStyles.Add(new(SizeType.Percent, 100)); root.RowStyles.Add(new(SizeType.Absolute, 68));
        root.Controls.Add(heading, 0, 0);
        root.Controls.Add(UiTheme.Label("Chọn khung còn trống trong tương lai để đóng. Các khung cũ được giữ lại để đối chiếu lịch sử.", color: DesignTokens.Slate500), 0, 1);
        root.Controls.Add(banner, 0, 2); root.Controls.Add(grid, 0, 3);
        var actions = new FlowLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(0, 8, 0, 0) };
        actions.Controls.Add(closeSlot); actions.Controls.Add(edit); actions.Controls.Add(refresh); root.Controls.Add(actions, 0, 4); Controls.Add(root);
        grid.AutoGenerateColumns = false;
        foreach (var label in new[] { "Bắt đầu", "Kết thúc", "Trạng thái", "Hiệu lực", "Lý do đóng" }) grid.Columns.Add(label, label);
        grid.Columns[4].FillWeight = 220;
        grid.CurrentCellChanged += (_, _) => UpdateActions();
        closeSlot.Click += async (_, _) =>
        {
            if (grid.CurrentRow?.Tag is not TimeSlotDto slot || !CanClose(slot)) return;
            using var form = new CloseSlotForm(session, dialogs, slot);
            form.ShowDialog(this); if (EndExpired()) return; await LoadAsync();
        };
        edit.Click += async (_, _) =>
        {
            if (schedule == null) return;
            using var form = new ScheduleEditorForm(session, dialogs, schedule);
            form.ShowDialog(this); if (EndExpired()) return; await LoadAsync();
        };
        refresh.Click += async (_, _) => await LoadAsync(); Shown += async (_, _) => await LoadAsync();
        FormClosing += (_, _) => lifetime.Cancel();
    }
    private static bool CanClose(TimeSlotDto slot) => slot.IsAvailable && slot.SlotDate.ToDateTime(slot.StartTime) > DateTime.UtcNow.AddHours(7);
    private bool EndExpired() { if (!session.Expired) return false; DialogResult = DialogResult.Abort; Close(); return true; }
    private void UpdateActions()
    {
        closeSlot.Enabled = !loading && grid.CurrentRow?.Tag is TimeSlotDto slot && CanClose(slot);
        edit.Enabled = !loading && schedule != null && schedule.WorkDate.ToDateTime(schedule.StartTime) > DateTime.UtcNow.AddHours(7);
    }
    private async Task LoadAsync()
    {
        if (loading || lifetime.IsCancellationRequested) return;
        loading = true; refresh.Enabled = false; UpdateActions(); grid.Rows.Clear(); banner.ShowMessage("Đang tải khung giờ…");
        try
        {
            schedule = await session.GetAsync<WorkingScheduleDto>($"api/schedules/{scheduleId}", lifetime.Token);
            heading.Text = $"{schedule.DoctorName} · {schedule.WorkDate:dd/MM/yyyy}";
            foreach (var slot in schedule.TimeSlots.OrderBy(s => s.StartTime).ThenByDescending(s => s.IsActive))
            {
                var row = grid.Rows[grid.Rows.Add(slot.StartTime.ToString("HH:mm"), slot.EndTime.ToString("HH:mm"),
                    slot.Status switch { Domain.Enums.TimeSlotStatus.Open => "Còn trống", Domain.Enums.TimeSlotStatus.Occupied => "Đã đặt", _ => "Đã đóng" },
                    slot.IsActive ? "Hiện hành" : "Lịch sử", slot.ClosedReason ?? "—")]; row.Tag = slot;
            }
            banner.ShowMessage($"{schedule.SlotsCount} khung hiện hành · {schedule.OpenSlotsCount} còn trống · {schedule.OccupiedSlotsCount} đã đặt. {schedule.Note}");
        }
        catch (HospitalApiException ex) { schedule = null; if (!EndExpired()) banner.ShowMessage(AdminSession.Error(ex), true); }
        catch (OperationCanceledException) when (lifetime.IsCancellationRequested) { }
        finally { loading = false; if (!IsDisposed) { refresh.Enabled = true; UpdateActions(); } }
    }
    protected override void Dispose(bool disposing) { if (disposing && !IsDisposed) { lifetime.Cancel(); lifetime.Dispose(); } base.Dispose(disposing); }
}
