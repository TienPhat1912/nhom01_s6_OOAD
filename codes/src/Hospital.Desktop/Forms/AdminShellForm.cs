using Hospital.Application.Common.Models;
using Hospital.Application.DTOs.Admin;
using Hospital.Application.DTOs.Auth;
using Hospital.Application.DTOs.Catalog;
using Hospital.Application.DTOs.Schedules;
using Hospital.Client;
using Hospital.Desktop.Design;
using Hospital.Desktop.Services;
using Hospital.Domain.Enums;

namespace Hospital.Desktop.Forms;

public sealed class AdminShellForm : Form
{
    private readonly AdminSession session;
    private readonly IDesktopDialogs dialogs;
    private readonly CancellationTokenSource lifetime = new();
    private readonly UiDataGrid grid = new() { AutoGenerateColumns = false };
    private readonly UiBanner banner = new() { Dock = DockStyle.Fill };
    private readonly Label title = UiTheme.Label("Tổng quan", 24, true, DesignTokens.Slate900);
    private readonly Label subtitle = UiTheme.Label("", color: DesignTokens.Slate500);
    private readonly Label pageLabel = UiTheme.Label("", color: DesignTokens.Slate500);
    private readonly Label identity = UiTheme.Label("", 10, true);
    private readonly Label clock = UiTheme.Label("", 9, color: DesignTokens.Slate500);
    private readonly System.Windows.Forms.Timer clockTimer = new() { Interval = 1000 };
    private readonly Label empty = UiTheme.Label("Chưa có dữ liệu phù hợp.\nThử thay đổi bộ lọc hoặc thêm bản ghi mới.", 13, color: DesignTokens.Slate500);
    private readonly TextBox search = new() { PlaceholderText = "Nhập tên hoặc mã để tìm kiếm", Dock = DockStyle.Fill, AccessibleName = "Từ khóa tìm kiếm" };
    private readonly UiButton refresh = new() { Text = "Tìm / Làm mới", Dock = DockStyle.Fill };
    private readonly UiButton previous = new() { Text = "Trước", Secondary = true, Dock = DockStyle.Fill };
    private readonly UiButton next = new() { Text = "Sau", Secondary = true, Dock = DockStyle.Fill };
    private readonly UiButton add = new() { Text = "Thêm mới", Width = 130 };
    private readonly UiButton edit = new() { Text = "Sửa thông tin", Secondary = true, Width = 140 };
    private readonly UiButton role = new() { Text = "Phân quyền", Secondary = true, Width = 130 };
    private readonly UiButton detail = new() { Text = "Xem chi tiết", Secondary = true, Width = 150 };
    private readonly UiButton profile = new() { Text = "Hồ sơ cá nhân", Secondary = true, Dock = DockStyle.Fill };
    private readonly UiButton password = new() { Text = "Đổi mật khẩu", Secondary = true, Dock = DockStyle.Fill };
    private readonly Dictionary<string, UiButton> navigation = [];
    private readonly FlowLayoutPanel dateFilters = new() { Dock = DockStyle.Fill, WrapContents = false };
    private readonly DateTimePicker from = DatePicker("Từ ngày", AdminSession.Today);
    private readonly DateTimePicker to = DatePicker("Đến ngày", AdminSession.Today.AddDays(6));
    private readonly UiButton doctorFilter = new() { Text = "Chọn bác sĩ", Secondary = true, Width = 180, Height = 34 };
    private readonly UiButton clearDoctor = new() { Text = "Tất cả", Secondary = true, Width = 80, Height = 34 };
    private readonly ComboBox auditFilter = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 220, AccessibleName = "Loại nhật ký" };
    private DoctorDto? selectedDoctor;
    private string section = "dashboard";
    private int page = 1, total;
    private bool loading;
    public string SessionEndedMessage => session.EndMessage;

    public AdminShellForm(HospitalApiClient api, AuthResponseDto auth, IDesktopDialogs? dialogs = null)
    {
        session = new(api, auth); this.dialogs = dialogs ?? new DesktopDialogs();
        Text = "An Tâm · Không gian quản trị"; StartPosition = FormStartPosition.CenterScreen;
        AutoScaleMode = AutoScaleMode.Dpi; AutoScaleDimensions = new SizeF(96, 96);
        ClientSize = new Size(1280, 820); MinimumSize = new Size(1120, 740); Font = UiTheme.Body(); BackColor = DesignTokens.Slate50;
        var root = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1 };
        root.ColumnStyles.Add(new(SizeType.Absolute, 220)); root.ColumnStyles.Add(new(SizeType.Percent, 100));
        var side = new TableLayoutPanel { Dock = DockStyle.Fill, BackColor = Color.White, Padding = new Padding(18), RowCount = 13, ColumnCount = 1, Margin = new Padding(0) };
        side.RowStyles.Add(new(SizeType.Absolute, 72)); side.RowStyles.Add(new(SizeType.Absolute, 28));
        for (var i = 0; i < 6; i++) side.RowStyles.Add(new(SizeType.Absolute, 50));
        side.RowStyles.Add(new(SizeType.Percent, 100)); side.RowStyles.Add(new(SizeType.Absolute, 64));
        for (var i = 0; i < 3; i++) side.RowStyles.Add(new(SizeType.Absolute, 46));
        side.Controls.Add(UiTheme.Label("✚ An Tâm", 19, true, DesignTokens.Primary900), 0, 0);
        side.Controls.Add(UiTheme.Label("QUẢN TRỊ HỆ THỐNG", 9, true, DesignTokens.Slate500), 0, 1);
        var sections = new[] { ("dashboard", "Tổng quan"), ("users", "Tài khoản"), ("doctors", "Bác sĩ"), ("specialties", "Chuyên khoa"), ("schedules", "Lịch làm việc"), ("audit", "Nhật ký") };
        for (var i = 0; i < sections.Length; i++)
        {
            var item = sections[i]; var button = new UiButton { Text = item.Item2, Dock = DockStyle.Fill, Secondary = true };
            button.Click += async (_, _) => { if (loading) return; section = item.Item1; page = 1; search.Clear(); title.Text = item.Item2; await LoadAsync(); };
            navigation[item.Item1] = button; side.Controls.Add(button, 0, i + 2);
        }
        side.Controls.Add(identity, 0, 9); side.Controls.Add(profile, 0, 10); side.Controls.Add(password, 0, 11);
        var logout = new UiButton { Text = "Đăng xuất", Secondary = true, Dock = DockStyle.Fill };
        logout.Click += (_, _) => { if (this.dialogs.Confirm(this, "Đăng xuất", "Đăng xuất khỏi không gian quản trị?")) Close(); }; side.Controls.Add(logout, 0, 12);
        profile.Click += async (_, _) => await OpenAsync("profile"); password.Click += async (_, _) => await OpenAsync("password");
        var content = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(24), RowCount = 8, ColumnCount = 1 };
        foreach (var height in new[] { 52, 60, 52, 46, 56, 84 }) content.RowStyles.Add(new(SizeType.Absolute, height));
        content.RowStyles.Add(new(SizeType.Percent, 100)); content.RowStyles.Add(new(SizeType.Absolute, 58));
        var header = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1, Margin = new Padding(0) };
        header.ColumnStyles.Add(new(SizeType.Percent, 100)); header.ColumnStyles.Add(new(SizeType.Absolute, 200));
        clock.TextAlign = ContentAlignment.MiddleRight; clock.AutoEllipsis = false; subtitle.AutoEllipsis = false; identity.AutoEllipsis = false;
        void UpdateClock() => clock.Text = DateTime.UtcNow.AddHours(7).ToString("dd/MM/yyyy · HH:mm:ss") + " (UTC+7)";
        UpdateClock(); clockTimer.Tick += (_, _) => UpdateClock(); clockTimer.Start();
        header.Controls.Add(title, 0, 0); header.Controls.Add(clock, 1, 0); content.Controls.Add(header, 0, 0); content.Controls.Add(subtitle, 0, 1);
        var toolbar = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1, Margin = new Padding(0) };
        toolbar.ColumnStyles.Add(new(SizeType.Percent, 100)); toolbar.ColumnStyles.Add(new(SizeType.Absolute, 170));
        toolbar.Controls.Add(search, 0, 0); toolbar.Controls.Add(refresh, 1, 0); content.Controls.Add(toolbar, 0, 2);
        refresh.Click += async (_, _) => { page = 1; await LoadAsync(); };
        search.KeyDown += async (_, e) => { if (e.KeyCode == Keys.Enter) { e.SuppressKeyPress = true; page = 1; await LoadAsync(); } };
        dateFilters.Controls.Add(new Label { Text = "Từ", AutoSize = true, Margin = new Padding(0, 7, 4, 0) }); dateFilters.Controls.Add(from);
        dateFilters.Controls.Add(new Label { Text = "Đến", AutoSize = true, Margin = new Padding(8, 7, 4, 0) }); dateFilters.Controls.Add(to);
        dateFilters.Controls.Add(doctorFilter); dateFilters.Controls.Add(clearDoctor);
        doctorFilter.Click += async (_, _) =>
        { using var picker = new DoctorPickerForm(session, onlyActive: false); if (picker.ShowDialog(this) == DialogResult.OK) { selectedDoctor = picker.SelectedDoctor; doctorFilter.Text = selectedDoctor!.FullName; page = 1; await LoadAsync(); } else EndExpired(); };
        clearDoctor.Click += async (_, _) => { selectedDoctor = null; doctorFilter.Text = "Chọn bác sĩ"; page = 1; await LoadAsync(); };
        foreach (var key in new[] { "", "User", "Doctor", "Specialty", "WorkingSchedule", "TimeSlot", "Appointment" }) auditFilter.Items.Add(new Choice<string>(key, key == "" ? "Tất cả nhật ký" : AuditDetailForm.EntityLabel(key)));
        auditFilter.SelectedIndex = 0; auditFilter.SelectedIndexChanged += async (_, _) => { page = 1; await LoadAsync(); };
        var filters = new Panel { Dock = DockStyle.Fill }; filters.Controls.Add(dateFilters); filters.Controls.Add(auditFilter); content.Controls.Add(filters, 0, 3);
        var actions = new FlowLayoutPanel { Dock = DockStyle.Fill, WrapContents = false };
        actions.Controls.Add(add); actions.Controls.Add(edit); actions.Controls.Add(role); actions.Controls.Add(detail); content.Controls.Add(actions, 0, 4);
        add.Click += async (_, _) => await OpenAsync("add"); edit.Click += async (_, _) => await OpenAsync("edit"); role.Click += async (_, _) => await OpenAsync("role"); detail.Click += async (_, _) => await OpenAsync("detail");
        grid.CurrentCellChanged += (_, _) => UpdateActions(); grid.CellDoubleClick += async (_, e) => { if (e.RowIndex >= 0) await OpenAsync(section is "dashboard" or "schedules" or "audit" ? "detail" : "edit"); };
        content.Controls.Add(banner, 0, 5);
        var surface = new Panel { Dock = DockStyle.Fill, BackColor = Color.White, Margin = new Padding(0) };
        empty.TextAlign = ContentAlignment.MiddleCenter; empty.Visible = false; surface.Controls.Add(grid); surface.Controls.Add(empty); content.Controls.Add(surface, 0, 6);
        var paging = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 1, ColumnCount = 3, Margin = new Padding(0, 12, 0, 0) };
        paging.ColumnStyles.Add(new(SizeType.Percent, 100)); paging.ColumnStyles.Add(new(SizeType.Absolute, 100)); paging.ColumnStyles.Add(new(SizeType.Absolute, 100));
        paging.Controls.Add(pageLabel, 0, 0); paging.Controls.Add(previous, 1, 0); paging.Controls.Add(next, 2, 0); content.Controls.Add(paging, 0, 7);
        previous.Click += async (_, _) => { page--; await LoadAsync(); }; next.Click += async (_, _) => { page++; await LoadAsync(); };
        root.Controls.Add(side, 0, 0); root.Controls.Add(content, 1, 0); Controls.Add(root);
        Shown += async (_, _) => await LoadAsync(); FormClosing += (_, _) => lifetime.Cancel();
    }

    private static DateTimePicker DatePicker(string label, DateOnly date) => new() { Format = DateTimePickerFormat.Custom, CustomFormat = "dd/MM/yyyy", Width = 132, AccessibleName = label, Value = date.ToDateTime(TimeOnly.MinValue) };
    private bool EndExpired() { if (!session.Expired) return false; Close(); return true; }
    private void UpdateActions()
    {
        var selected = grid.CurrentRow?.Tag;
        add.Visible = section is "users" or "doctors" or "specialties" or "schedules";
        edit.Visible = section is "users" or "doctors" or "specialties" or "schedules";
        role.Visible = section == "users"; detail.Visible = section is "dashboard" or "schedules" or "audit";
        add.Enabled = !loading; edit.Enabled = detail.Enabled = !loading && selected != null;
        role.Enabled = !loading && selected is UserDto user && user.Role != UserRole.Patient;
    }
    private void SetBusy(bool busy)
    {
        loading = busy; refresh.Enabled = profile.Enabled = password.Enabled = !busy;
        search.Enabled = !busy && section != "audit"; dateFilters.Enabled = auditFilter.Enabled = !busy;
        foreach (var (key, button) in navigation) { button.Secondary = key != section; button.Enabled = !busy; }
        previous.Enabled = !busy && page > 1; next.Enabled = !busy && page * 20 < total; UpdateActions();
    }
    private void Rows<T>(IEnumerable<T> rows, string[] columns, Func<T, object[]> values) where T : notnull
    {
        grid.Rows.Clear(); grid.Columns.Clear();
        foreach (var label in columns) grid.Columns.Add(label, label);
        foreach (var item in rows) grid.Rows[grid.Rows.Add(values(item))].Tag = item;
    }
    private async Task LoadAsync(string? success = null)
    {
        if (loading || lifetime.IsCancellationRequested) return;
        SetBusy(true); grid.Rows.Clear(); grid.Visible = empty.Visible = false; total = 0;
        banner.ShowMessage("Đang tải dữ liệu…"); identity.Text = session.FullName;
        dateFilters.Visible = section == "schedules"; auditFilter.Visible = section == "audit";
        subtitle.Text = section switch
        {
            "dashboard" => "Đang tổng hợp dữ liệu hiện có…", "users" => "Quản lý thông tin, trạng thái và quyền truy cập. Khóa tài khoản để giữ lại lịch sử.",
            "doctors" => "Hồ sơ chuyên môn và chuyên khoa chính của đội ngũ bác sĩ.", "specialties" => "Danh mục chuyên khoa dùng chung cho cổng Web và ứng dụng quản trị.",
            "schedules" => "Mặc định hiển thị 7 ngày. Chọn khoảng ngày và bấm Làm mới; tối đa 366 ngày. Giờ Việt Nam (UTC+7).",
            _ => "Lịch sử thay đổi theo thời gian mới nhất. Chọn bản ghi để xem dữ liệu trước và sau. Giờ Việt Nam (UTC+7)."
        };
        try
        {
            var query = $"page={page}&pageSize=20&keyword={Uri.EscapeDataString(search.Text.Trim())}&onlyActive=false";
            switch (section)
            {
                case "users":
                    var users = await session.GetAsync<PagedResult<UserDto>>("api/admin/users?" + query, lifetime.Token); total = users.TotalCount;
                    Rows(users.Items, ["Tài khoản", "Họ tên", "Email", "Vai trò", "Trạng thái"], u => [u.Username, u.FullName, u.Email, AdminSession.Role(u.Role), u.IsActive ? "Hoạt động" : "Đã khóa"]); break;
                case "doctors":
                    var doctors = await session.GetAsync<PagedResult<DoctorDto>>("api/doctors?" + query, lifetime.Token); total = doctors.TotalCount;
                    Rows(doctors.Items, ["Mã bác sĩ", "Họ tên", "Chuyên khoa", "Tài khoản", "Trạng thái"], d => [d.DoctorCode, d.FullName, d.SpecialtyName, d.LinkedUserId.HasValue ? "Đã liên kết" : "Chưa liên kết", d.IsActive ? "Hoạt động" : "Ngừng hoạt động"]); break;
                case "specialties":
                    var specialties = await session.GetAsync<PagedResult<SpecialtyDto>>("api/specialties?" + query, lifetime.Token); total = specialties.TotalCount;
                    Rows(specialties.Items, ["Mã", "Chuyên khoa", "Số bác sĩ", "Trạng thái"], s => [s.Code, s.Name, s.DoctorCount, s.IsActive ? "Hoạt động" : "Ngừng hoạt động"]); break;
                case "audit":
                    var kind = ((Choice<string>)auditFilter.SelectedItem!).Value;
                    var logs = await session.GetAsync<PagedResult<AuditLogDto>>($"api/admin/audit-logs?page={page}&pageSize=20{(kind == "" ? "" : "&entityType=" + kind)}", lifetime.Token); total = logs.TotalCount;
                    Rows(logs.Items, ["Thời gian", "Người thực hiện", "Thao tác", "Đối tượng"], a => [a.CreatedAt.AddHours(7).ToString("dd/MM/yyyy HH:mm:ss"), a.UserName ?? "Hệ thống", AuditDetailForm.ActionLabel(a.Action), AuditDetailForm.EntityLabel(a.EntityType)]); break;
                default:
                    var first = section == "dashboard" ? AdminSession.Today : DateOnly.FromDateTime(from.Value);
                    var last = section == "dashboard" ? first.AddDays(6) : DateOnly.FromDateTime(to.Value);
                    if (last < first || last.DayNumber - first.DayNumber > 366) { banner.ShowMessage("Chọn ngày kết thúc từ ngày bắt đầu đến tối đa 366 ngày sau.", true); pageLabel.Text = "Bộ lọc ngày chưa hợp lệ"; return; }
                    if (section == "dashboard")
                    {
                        var userCount = session.GetAsync<PagedResult<UserDto>>("api/admin/users?pageSize=1", lifetime.Token);
                        var doctorCount = session.GetAsync<PagedResult<DoctorDto>>("api/doctors?onlyActive=false&pageSize=1", lifetime.Token);
                        var specialtyCount = session.GetAsync<PagedResult<SpecialtyDto>>("api/specialties?onlyActive=false&pageSize=1", lifetime.Token);
                        await Task.WhenAll(userCount, doctorCount, specialtyCount);
                        subtitle.Text = $"{userCount.Result.TotalCount} tài khoản · {doctorCount.Result.TotalCount} bác sĩ · {specialtyCount.Result.TotalCount} chuyên khoa\nCa làm việc từ {first:dd/MM} đến {last:dd/MM/yyyy} (UTC+7).";
                    }
                    var schedules = await session.GetAsync<List<WorkingScheduleDto>>($"api/schedules?fromDate={first:yyyy-MM-dd}&toDate={last:yyyy-MM-dd}{(section == "schedules" && selectedDoctor != null ? "&doctorId=" + selectedDoctor.Id : "")}", lifetime.Token);
                    var filtered = schedules.Where(s => $"{s.DoctorName} {s.DoctorCode} {s.Note}".Contains(search.Text.Trim(), StringComparison.CurrentCultureIgnoreCase)).OrderBy(s => s.WorkDate).ThenBy(s => s.StartTime).ThenBy(s => s.DoctorName).ThenBy(s => s.Id).ToList(); total = filtered.Count;
                    page = Math.Min(page, Math.Max(1, (total + 19) / 20));
                    Rows(filtered.Skip((page - 1) * 20).Take(20), ["Ngày", "Bác sĩ", "Ca trực", "Còn trống", "Đã đặt"], s => [s.WorkDate.ToString("dd/MM/yyyy"), s.DoctorName, $"{s.StartTime:HH:mm}–{s.EndTime:HH:mm}", s.OpenSlotsCount, s.OccupiedSlotsCount]); break;
            }
            grid.Visible = grid.Rows.Count > 0; empty.Visible = !grid.Visible; if (empty.Visible) empty.BringToFront();
            pageLabel.Text = $"{total} kết quả · Trang {page} / {Math.Max(1, (total + 19) / 20)}";
            banner.ShowMessage(success ?? "Đã kết nối · Cập nhật lúc " + DateTime.UtcNow.AddHours(7).ToString("HH:mm:ss"));
        }
        catch (HospitalApiException ex) { pageLabel.Text = "Chưa tải được dữ liệu"; if (!EndExpired()) banner.ShowMessage(AdminSession.Error(ex), true); }
        catch (OperationCanceledException) when (lifetime.IsCancellationRequested) { }
        finally { if (!IsDisposed) SetBusy(false); }
    }
    private async Task OpenAsync(string action)
    {
        if (loading || lifetime.IsCancellationRequested) return;
        var selected = grid.CurrentRow?.Tag;
        if (action is "edit" or "role" or "detail" && selected == null) return;
        SetBusy(true);
        var saved = false;
        try
        {
            using Form form = action switch
            {
                "profile" => new ProfileEditorForm(session, dialogs, await session.GetAsync<UserProfileDto>("api/auth/profile", lifetime.Token)),
                "password" => new PasswordEditorForm(session, dialogs),
                "role" => new RoleEditorForm(session, dialogs, await session.GetAsync<UserDto>($"api/admin/users/{((UserDto)selected!).Id}", lifetime.Token)),
                "detail" when selected is WorkingScheduleDto schedule => new ScheduleDetailsForm(session, dialogs, schedule.Id),
                "detail" when selected is AuditLogDto log => new AuditDetailForm(log),
                _ when section == "users" => new UserEditorForm(session, dialogs, action == "add" ? null : await session.GetAsync<UserDto>($"api/admin/users/{((UserDto)selected!).Id}", lifetime.Token)),
                _ when section == "doctors" => new DoctorEditorForm(session, dialogs, await session.AllAsync<SpecialtyDto>("api/specialties?onlyActive=false", lifetime.Token), action == "add" ? null : await session.GetAsync<DoctorDetailDto>($"api/doctors/{((DoctorDto)selected!).Id}", lifetime.Token)),
                _ when section == "specialties" => new SpecialtyEditorForm(session, dialogs, action == "add" ? null : await session.GetAsync<SpecialtyDto>($"api/specialties/{((SpecialtyDto)selected!).Id}", lifetime.Token)),
                _ => new ScheduleEditorForm(session, dialogs, action == "add" ? null : await session.GetAsync<WorkingScheduleDto>($"api/schedules/{((WorkingScheduleDto)selected!).Id}", lifetime.Token), selectedDoctor)
            };
            saved = form.ShowDialog(this) == DialogResult.OK;
        }
        catch (HospitalApiException ex) { if (!EndExpired()) banner.ShowMessage(AdminSession.Error(ex), true); return; }
        catch (OperationCanceledException) when (lifetime.IsCancellationRequested) { }
        finally { if (!IsDisposed) SetBusy(false); }
        if (IsDisposed || EndExpired()) return;
        await LoadAsync(saved ? "Đã lưu thay đổi thành công." : null);
    }
    protected override void Dispose(bool disposing) { if (disposing && !IsDisposed) { clockTimer.Dispose(); lifetime.Cancel(); lifetime.Dispose(); } base.Dispose(disposing); }
}
