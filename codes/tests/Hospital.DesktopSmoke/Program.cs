using System.Diagnostics;
using System.Drawing.Imaging;
using System.Net;
using Hospital.Application.Common.Models;
using Hospital.Application.DTOs.Admin;
using Hospital.Application.DTOs.Appointments;
using Hospital.Application.DTOs.Auth;
using Hospital.Application.DTOs.Catalog;
using Hospital.Application.DTOs.Schedules;
using Hospital.Client;
using Hospital.Desktop.Design;
using Hospital.Desktop.Forms;
using Hospital.Desktop.Services;
using Hospital.Domain.Enums;

internal static partial class Program
{
    private static string Output = Path.GetFullPath("tests/browser/artifacts/phase6");
    private static readonly TestDialogs Dialogs = new();
    private static int assertions;
    [STAThread]
    private static void Main(string[] args)
    {
        Console.OutputEncoding = System.Text.Encoding.UTF8;
        ApplicationConfiguration.Initialize(); Application.SetUnhandledExceptionMode(UnhandledExceptionMode.ThrowException);
        Control.CheckForIllegalCrossThreadCalls = true;
        Exception? failure = null;
        using var host = new Form { Opacity = 0, ShowInTaskbar = false };
        host.Shown += (_, _) => host.BeginInvoke(new Action(async () =>
        { try { if (args.Length == 0) await RunAsync(); else if (args is ["--phase8", var fixture]) await RunPhase8Async(fixture); else await RunPhase7Async(args); } catch (Exception ex) { failure = ex; } finally { host.Close(); } }));
        Application.Run(host);
        if (failure != null) throw new InvalidOperationException("Desktop smoke failed.", failure);
    }
    private static async Task RunAsync()
    {
        Directory.CreateDirectory(Output);
        using (var login = new LoginForm())
        {
            ShowHidden(login); Button(login, "Đăng nhập").PerformClick();
            Require(Find<TextBox>(login, b => b.AccessibleName == "Tên đăng nhập").AccessibleDescription == "Vui lòng nhập tên đăng nhập.", "Login required validation");
            Capture(login, "login"); login.Size = login.MinimumSize; Capture(login, "login-minimum"); login.Close();
        }
        using (var confirm = new ConfirmForm("Xác nhận", "Thao tác có thay đổi dữ liệu."))
        { ShowHidden(confirm); Require(confirm.AcceptButton == confirm.CancelButton, "Confirmation defaults to cancel"); Capture(confirm, "confirmation"); confirm.Close(); }
        using var http = new HttpClient { BaseAddress = new Uri(Environment.GetEnvironmentVariable("HOSPITAL_API_URL") ?? "http://localhost:5034/"), Timeout = TimeSpan.FromSeconds(15) };
        var api = new HospitalApiClient(http);
        async Task<AuthResponseDto> Login(string user, string password) => await api.SendAsync<AuthResponseDto>(HttpMethod.Post, "api/auth/login", new LoginRequestDto { Username = user, Password = password });
        var auth = await Login("admin", "Admin@123"); var session = new AdminSession(api, auth);
        using (var shell = new AdminShellForm(api, auth, Dialogs))
        {
            ShowHidden(shell); await Loaded(shell); Capture(shell, "dashboard");
            foreach (var (name, minimum) in new[] { ("Tài khoản", 6), ("Bác sĩ", 2), ("Chuyên khoa", 5), ("Lịch làm việc", 1), ("Nhật ký", 0) })
            {
                Button(shell, name).PerformClick(); await Loaded(shell);
                Require(Find<UiDataGrid>(shell, _ => true).Rows.Count >= minimum, name + " real API list"); Capture(shell, name == "Lịch làm việc" ? "schedules" : name == "Tài khoản" ? "users" : "list-" + minimum);
            }
            Button(shell, "Chuyên khoa").PerformClick(); await Loaded(shell);
            await Modal<SpecialtyEditorForm>(() => Button(shell, "Thêm mới").PerformClick(), async form =>
            {
                var key = "NAV" + DateTime.UtcNow.ToString("HHmmssfff");
                Fill(form, "Code", key); Fill(form, "Name", "Chuyên khoa điều hướng " + key); await Submit(form);
            });
            await Loaded(shell);
            Require(Banner(shell).Contains("thành công"), "Shell opens editor and refreshes after successful save");
            Find<TextBox>(shell, b => b.AccessibleName == "Từ khóa tìm kiếm").Text = "NO_MATCH_PHASE6"; Button(shell, "Tìm / Làm mới").PerformClick(); await Loaded(shell);
            Require(Find<UiDataGrid>(shell, _ => true).Rows.Count == 0 && !Button(shell, "Sửa thông tin").Enabled, "Empty search disables edit"); Capture(shell, "empty");
            Button(shell, "Lịch làm việc").PerformClick(); await Loaded(shell);
            Find<DateTimePicker>(shell, b => b.AccessibleName == "Đến ngày").Value = AdminSession.Today.AddDays(-1).ToDateTime(TimeOnly.MinValue);
            Button(shell, "Tìm / Làm mới").PerformClick(); await Loaded(shell);
            Require(Banner(shell).Contains("366"), "Invalid date range explained");
            shell.Size = shell.MinimumSize; Capture(shell, "shell-minimum"); shell.Close();
        }

        var suffix = DateTime.UtcNow.ToString("HHmmssfff");
        SpecialtyDto specialty;
        using (var form = new SpecialtyEditorForm(session, Dialogs))
        {
            ShowHidden(form); Save(form).PerformClick(); Require(Field<TextBox>(form, "Code").AccessibleDescription!.Length > 0, "Specialty required field validation");
            Fill(form, "Code", "P6" + suffix); Fill(form, "Name", "Chuyên khoa Phase 6 " + suffix); Fill(form, "Description", "Kiểm thử WinForms trên SQL Server");
            Dialogs.Answer = false; Save(form).PerformClick(); Require(form.SavedResult == null && Save(form).Enabled, "Cancelled confirmation does not save"); Dialogs.Answer = true;
            Capture(form, "specialty-create"); await Submit(form); specialty = (SpecialtyDto)form.SavedResult!;
        }
        Require((await session.GetAsync<SpecialtyDto>($"api/specialties/{specialty.Id}", default)).Code == specialty.Code, "Specialty persisted through API");
        using (var duplicate = new SpecialtyEditorForm(session, Dialogs))
        {
            ShowHidden(duplicate); Fill(duplicate, "Code", specialty.Code); Fill(duplicate, "Name", "Duplicate " + suffix);
            await Rejected(duplicate); Require(Banner(duplicate).Contains("Mã hỗ trợ"), "Duplicate specialty API error keeps trace"); duplicate.Close();
        }
        DoctorDetailDto doctor;
        var options = await session.AllAsync<SpecialtyDto>("api/specialties?onlyActive=false", default);
        using (var form = new DoctorEditorForm(session, Dialogs, options))
        {
            ShowHidden(form); Fill(form, "FullName", "Bác sĩ Phase 6 " + suffix); Fill(form, "Title", "BS.CKI");
            Select(form, "SpecialtyId", specialty.Id);
            Require(((Choice<int>)Field<ComboBox>(form, "SpecialtyId").SelectedItem!).Value == specialty.Id, "Doctor has one selected specialty");
            Capture(form, "doctor-create"); form.Size = form.MinimumSize; Capture(form, "doctor-minimum"); await Submit(form); doctor = (DoctorDetailDto)form.SavedResult!;
        }
        var doctorRow = (await session.AllAsync<DoctorDto>("api/doctors?onlyActive=false", default)).Single(d => d.Id == doctor.Id);
        UserDto doctorUser;
        using (var form = new UserEditorForm(session, Dialogs))
        {
            ShowHidden(form); Fill(form, "Username", "p6doctor" + suffix); Fill(form, "Email", "p6doctor" + suffix + "@test.example"); Fill(form, "Password", "Doctor@123");
            Select(form, "Role", UserRole.Doctor); await PickDoctor(form, doctor.Id);
            Require(Field<TextBox>(form, "FullName").ReadOnly && Field<TextBox>(form, "FullName").Text == doctor.FullName, "Doctor account uses selected profile name");
            Capture(form, "user-create"); await Submit(form); doctorUser = (UserDto)form.SavedResult!;
        }
        Require(doctorUser.DoctorId == doctor.Id, "Doctor account linked to existing profile");
        using (var picker = new DoctorPickerForm(session, unlinkedOnly: true))
        {
            ShowHidden(picker); await Wait(() => Button(picker, "Tìm kiếm").Enabled && Banner(picker).Contains("hồ sơ"));
            var grid = Find<UiDataGrid>(picker, _ => true); grid.CurrentCell = grid.Rows.Cast<DataGridViewRow>().Single(r => ((DoctorDto)r.Tag!).Id == doctor.Id).Cells[0];
            Require(!Button(picker, "Chọn bác sĩ").Enabled, "Linked doctor cannot be assigned to a second account"); Capture(picker, "doctor-picker"); picker.Close();
        }
        using (var form = new DoctorEditorForm(session, Dialogs, options, doctor))
        { ShowHidden(form); Fill(form, "FullName", doctor.FullName + " cập nhật"); await Submit(form); doctor = (DoctorDetailDto)form.SavedResult!; }
        Require((await session.GetAsync<UserDto>($"api/admin/users/{doctorUser.Id}", default)).FullName == doctor.FullName, "Doctor name synchronized to account");

        UserDto staff;
        using (var form = new UserEditorForm(session, Dialogs))
        { ShowHidden(form); Fill(form, "Username", "p6staff" + suffix); Fill(form, "FullName", "Nhân viên Phase 6"); Fill(form, "Email", "p6staff" + suffix + "@test.example"); Fill(form, "Password", "Staff@123"); await Submit(form); staff = (UserDto)form.SavedResult!; }
        var staffAuth = await Login(staff.Username, "Staff@123");
        using (var form = new RoleEditorForm(session, Dialogs, staff))
        { ShowHidden(form); Select(form, "Role", UserRole.Admin); await Submit(form); staff = (UserDto)form.SavedResult!; }
        await Unauthorized(api, staffAuth, "Role change revokes old token");
        using (var form = new UserEditorForm(session, Dialogs, staff))
        { ShowHidden(form); Field<CheckBox>(form, "IsActive").Checked = false; await Submit(form); staff = (UserDto)form.SavedResult!; }
        Require(!(await session.GetAsync<UserDto>($"api/admin/users/{staff.Id}", default)).IsActive, "Account lock persisted");
        var admin = await session.GetAsync<UserDto>($"api/admin/users/{auth.UserId}", default);
        using (var form = new UserEditorForm(session, Dialogs, admin))
        { ShowHidden(form); Field<CheckBox>(form, "IsActive").Checked = false; await Rejected(form); Require(Banner(form).Contains("cuối", StringComparison.OrdinalIgnoreCase), "Last active admin protected"); form.Close(); }
        using (var form = new UserEditorForm(session, Dialogs, staff))
        { ShowHidden(form); Field<CheckBox>(form, "IsActive").Checked = true; await Submit(form); staff = (UserDto)form.SavedResult!; }

        WorkingScheduleDto schedule;
        using (var form = new ScheduleEditorForm(session, Dialogs, selectedDoctor: doctorRow))
        {
            ShowHidden(form); Field<DateTimePicker>(form, "WorkDate").Value = AdminSession.Today.AddDays(2).ToDateTime(TimeOnly.MinValue);
            SetTime(form, "StartTime", 14); SetTime(form, "EndTime", 14); Save(form).PerformClick(); Require(Field<DateTimePicker>(form, "EndTime").AccessibleDescription!.Length > 0, "Invalid schedule duration validated");
            SetTime(form, "EndTime", 16); Fill(form, "Note", "Ca kiểm thử Phase 6"); Capture(form, "schedule-create"); await Submit(form); schedule = (WorkingScheduleDto)form.SavedResult!;
        }
        Require(schedule.TimeSlots.Count == 4, "Schedule creates four 30-minute slots");
        using (var form = new ScheduleEditorForm(session, Dialogs, selectedDoctor: doctorRow))
        { ShowHidden(form); Field<DateTimePicker>(form, "WorkDate").Value = schedule.WorkDate.ToDateTime(TimeOnly.MinValue); SetTime(form, "StartTime", 14); SetTime(form, "EndTime", 16); await Rejected(form); Require(Banner(form).Contains("trùng", StringComparison.OrdinalIgnoreCase) || Banner(form).Contains("chồng", StringComparison.OrdinalIgnoreCase), "Overlapping schedule rejected by API"); form.Close(); }
        using (var form = new ScheduleEditorForm(session, Dialogs, schedule))
        { ShowHidden(form); SetTime(form, "StartTime", 15); SetTime(form, "EndTime", 17); await Submit(form); schedule = (WorkingScheduleDto)form.SavedResult!; }
        Require(schedule.TimeSlots.Count(s => !s.IsActive) == 4 && schedule.TimeSlots.Count(s => s.IsActive) == 4, "Schedule edit preserves historical slots");
        var closed = schedule.TimeSlots.First(s => s.IsAvailable);
        using (var form = new CloseSlotForm(session, Dialogs, closed))
        { ShowHidden(form); Save(form).PerformClick(); Require(Field<TextBox>(form, "Reason").AccessibleDescription!.Length > 0, "Close slot requires reason"); Fill(form, "Reason", "Bác sĩ nghỉ đột xuất"); await Submit(form); }
        schedule = await session.GetAsync<WorkingScheduleDto>($"api/schedules/{schedule.Id}", default);
        Require(schedule.TimeSlots.Single(s => s.Id == closed.Id) is { Status: TimeSlotStatus.Closed, IsActive: false }, "Closed slot retained and unavailable");
        var patientAuth = await Login("patient1", "Patient@123");
        var occupied = schedule.TimeSlots.First(s => s.IsAvailable);
        var appointment = await api.SendAsync<AppointmentDto>(HttpMethod.Post, "api/appointments", new BookAppointmentRequest { SpecialtyId = specialty.Id, TimeSlotId = occupied.Id, RequestKey = Guid.NewGuid(), Notes = "Phase 6 shared API check" }, patientAuth.Token);
        using (var form = new CloseSlotForm(session, Dialogs, occupied))
        { ShowHidden(form); Fill(form, "Reason", "Cannot close booked slot"); await Rejected(form); Require(Banner(form).Contains("lịch hẹn", StringComparison.OrdinalIgnoreCase), "API protects occupied slot"); Capture(form, "occupied-error"); form.Close(); }
        using (var form = new ScheduleEditorForm(session, Dialogs, schedule))
        { ShowHidden(form); SetTime(form, "StartTime", 16); SetTime(form, "EndTime", 18); await Rejected(form); Require(Banner(form).Contains("lịch hẹn", StringComparison.OrdinalIgnoreCase), "API protects booked schedule from time changes"); form.Close(); }
        using (var form = new DoctorEditorForm(session, Dialogs, options, doctor))
        { ShowHidden(form); Field<CheckBox>(form, "IsActive").Checked = false; await Rejected(form); Require(Banner(form).Contains("lịch", StringComparison.OrdinalIgnoreCase), "Cannot deactivate doctor with future booking"); form.Close(); }
        using (var form = new SpecialtyEditorForm(session, Dialogs, specialty))
        { ShowHidden(form); Field<CheckBox>(form, "IsActive").Checked = false; await Rejected(form); Require(Banner(form).Contains("lịch", StringComparison.OrdinalIgnoreCase), "Cannot deactivate specialty with future booking"); form.Close(); }
        using (var details = new ScheduleDetailsForm(session, Dialogs, schedule.Id))
        {
            ShowHidden(details); await Wait(() => Button(details, "Làm mới").Enabled && Banner(details).Contains("hiện hành"));
            var grid = Find<UiDataGrid>(details, _ => true); grid.CurrentCell = grid.Rows.Cast<DataGridViewRow>().Single(r => ((TimeSlotDto)r.Tag!).Id == occupied.Id).Cells[0];
            Require(!Button(details, "Đóng khung giờ").Enabled, "Occupied slot action disabled"); Capture(details, "schedule-details"); details.Close();
        }
        await api.SendAsync<object>(HttpMethod.Post, $"api/appointments/{appointment.Id}/cancel", new AppointmentMutationRequest { Reason = "Complete Phase 6 smoke", RowVersion = appointment.RowVersion }, patientAuth.Token);
        using (var form = new DoctorEditorForm(session, Dialogs, options, doctor))
        { ShowHidden(form); Field<CheckBox>(form, "IsActive").Checked = false; await Submit(form); }
        using (var form = new SpecialtyEditorForm(session, Dialogs, specialty))
        { ShowHidden(form); Field<CheckBox>(form, "IsActive").Checked = false; await Submit(form); }
        var patientDoctors = await api.SendAsync<PagedResult<DoctorDto>>(HttpMethod.Get, "api/doctors?keyword=" + Uri.EscapeDataString(doctor.FullName), token: patientAuth.Token);
        Require(patientDoctors.TotalCount == 0, "Patient catalog reflects Desktop deactivation through shared API");
        var logs = await session.GetAsync<PagedResult<AuditLogDto>>("api/admin/audit-logs?entityType=WorkingSchedule", default);
        Require(logs.Items.Any(l => l.EntityId == schedule.Id.ToString() && l.UserName != null), "Audit records actual Desktop mutations and actor");
        using (var audit = new AuditDetailForm(logs.Items.First(l => l.EntityId == schedule.Id.ToString())))
        { ShowHidden(audit); Require(Descendants(audit).OfType<TextBox>().All(t => t.ReadOnly), "Audit detail is read-only"); Capture(audit, "audit-detail"); audit.Close(); }
        using (var form = new ProfileEditorForm(session, Dialogs, await session.GetAsync<UserProfileDto>("api/auth/profile", default)))
        { ShowHidden(form); Fill(form, "FullName", "Quản trị Phase 6"); await Submit(form); }
        Require(session.FullName == (await session.GetAsync<UserProfileDto>("api/auth/profile", default)).FullName, "Profile updates current session and API");

        await FaultChecks(auth);
        var secondAuth = await Login(staff.Username, "Staff@123"); var secondSession = new AdminSession(api, secondAuth);
        using (var form = new RoleEditorForm(secondSession, Dialogs, staff))
        { ShowHidden(form); Select(form, "Role", UserRole.Staff); await Submit(form); Require(secondSession.Expired, "Self role change ends Desktop session"); }
        using (var shell = new AdminShellForm(api, secondAuth, Dialogs))
        { ShowHidden(shell); await Wait(() => shell.IsDisposed); Require(shell.SessionEndedMessage.Contains("đăng nhập lại"), "Revoked session closes shell without stale data"); }
        using (var form = new PasswordEditorForm(session, Dialogs))
        {
            ShowHidden(form); Fill(form, "OldPassword", "Admin@123"); Fill(form, "NewPassword", "Phase6Admin@123"); Fill(form, "ConfirmPassword", "Mismatch@123"); Save(form).PerformClick();
            Require(Field<TextBox>(form, "ConfirmPassword").AccessibleDescription!.Contains("khớp"), "Password confirmation validation");
            Fill(form, "ConfirmPassword", "Phase6Admin@123"); await Submit(form); Require(session.Expired, "Password change ends Desktop session");
        }
        await Unauthorized(api, auth, "Password change revokes prior token"); Require((await Login("admin", "Phase6Admin@123")).Role == "Admin", "New password logs in successfully");
        Console.WriteLine($"Desktop Phase 6 smoke passed: {assertions} assertions, real WinForms message loop, API and SQL Server workflows.");
    }

    private static async Task FaultChecks(AuthResponseDto auth)
    {
        using var handler = new OfflineHandler(); using var http = new HttpClient(handler) { BaseAddress = new Uri("http://desktop-test/") };
        var api = new HospitalApiClient(http); var session = new AdminSession(api, auth);
        using (var form = new SpecialtyEditorForm(session, Dialogs))
        { ShowHidden(form); Fill(form, "Code", "OFFLINE"); Fill(form, "Name", "Offline"); Save(form).PerformClick(); await Wait(() => Banner(form).Contains("Chưa rõ")); Require(!Save(form).Enabled && handler.Count == 1, "Unknown write result blocks accidental duplicate submission"); Capture(form, "connection-error"); form.Close(); }
        using (var shell = new AdminShellForm(api, auth, Dialogs))
        { ShowHidden(shell); await Loaded(shell); Require(Banner(shell).Contains("kết nối") && Find<UiDataGrid>(shell, _ => true).Rows.Count == 0, "Read outage clears stale rows and enables refresh"); shell.Close(); }
    }
    private static async Task Unauthorized(HospitalApiClient api, AuthResponseDto auth, string name)
    { try { await api.SendAsync<object>(HttpMethod.Get, "api/auth/profile", token: auth.Token); throw new Exception("Token remained valid"); } catch (HospitalApiException ex) { Require(ex.Status == HttpStatusCode.Unauthorized, name); } }
    private static async Task PickDoctor(Form owner, Guid id)
    {
        Exception? failure = null;
        using var timer = new System.Windows.Forms.Timer { Interval = 20 };
        var watch = Stopwatch.StartNew();
        timer.Tick += (_, _) =>
        {
            var picker = Application.OpenForms.OfType<DoctorPickerForm>().FirstOrDefault(); if (picker == null) return;
            picker.Opacity = 0; picker.ShowInTaskbar = false;
            try
            {
                if (watch.Elapsed > TimeSpan.FromSeconds(30)) throw new TimeoutException("Doctor picker did not load");
                if (!Button(picker, "Tìm kiếm").Enabled) return;
                var grid = Find<UiDataGrid>(picker, _ => true); var row = grid.Rows.Cast<DataGridViewRow>().Single(r => ((DoctorDto)r.Tag!).Id == id);
                var linked = grid.Rows.Cast<DataGridViewRow>().First(r => ((DoctorDto)r.Tag!).LinkedUserId.HasValue);
                grid.CurrentCell = linked.Cells[0]; Require(!Button(picker, "Chọn bác sĩ").Enabled, "Linked row disables picker action");
                grid.CurrentCell = row.Cells[0]; Require(Button(picker, "Chọn bác sĩ").Enabled, "Switching to unlinked row enables picker action");
                timer.Stop(); Button(picker, "Chọn bác sĩ").PerformClick();
            }
            catch (Exception ex) { failure = ex; timer.Stop(); picker.Close(); }
        };
        timer.Start(); Field<UiButton>(owner, "DoctorId").PerformClick(); timer.Stop();
        if (failure != null) throw failure; await Task.CompletedTask;
    }
    private static async Task Submit(EditorForm form)
    {
        var title = form.Text;
        Save(form).PerformClick(); await Wait(() => form.IsDisposed || Save(form).Enabled);
        Require(form.DialogResult == DialogResult.OK, title + " saved: " + (form.IsDisposed ? "OK" : Banner(form)));
    }
    private static async Task Rejected(EditorForm form)
    { Save(form).PerformClick(); await Wait(() => form.IsDisposed || Save(form).Enabled); Require(!form.IsDisposed && form.SavedResult == null && Banner(form).Length > 0, form.Text + " rejected safely"); }
    private static Task Loaded(Form form) => Wait(() => Button(form, "Tìm / Làm mới").Enabled && Banner(form).Length > 0);
    private static void SetTime(Form form, string key, int hour) => Field<DateTimePicker>(form, key).Value = DateTime.Today.AddHours(hour);
    private static void Select<T>(Form form, string key, T value) => Field<ComboBox>(form, key).SelectedItem = Field<ComboBox>(form, key).Items.Cast<Choice<T>>().Single(c => EqualityComparer<T>.Default.Equals(c.Value, value));
    private static void Fill(Form form, string key, string value) => Field<TextBox>(form, key).Text = value;
    private static T Field<T>(Control root, string key) where T : Control => Find<T>(root, c => c.Name == key);
    private static UiButton Button(Control root, string text) => Find<UiButton>(root, b => b.Text == text);
    private static UiButton Save(Control root) => Find<UiButton>(root, b => b.Text is "Lưu thay đổi" or "Đang lưu…");
    private static string Banner(Control root) => Find<UiBanner>(root, _ => true).AccessibleName ?? "";
    private static void ShowHidden(Form form) { form.Opacity = 0; form.ShowInTaskbar = false; form.Show(); }
    private static async Task Modal<T>(Action open, Func<T, Task> interact) where T : Form
    {
        var completion = new TaskCompletionSource();
        using var timer = new System.Windows.Forms.Timer { Interval = 20 };
        timer.Tick += async (_, _) =>
        {
            var form = Application.OpenForms.OfType<T>().FirstOrDefault(); if (form == null) return;
            timer.Stop(); form.Opacity = 0; form.ShowInTaskbar = false;
            try { await interact(form); completion.TrySetResult(); }
            catch (Exception ex) { completion.TrySetException(ex); }
            finally { if (!form.IsDisposed) form.Close(); }
        };
        timer.Start(); open(); await completion.Task.WaitAsync(TimeSpan.FromSeconds(30));
    }
    private static void Capture(Form form, string name)
    {
        form.PerformLayout();
        var flowButtons = Descendants(form).OfType<UiButton>().Where(b => b.Visible && b.Parent is FlowLayoutPanel).ToList();
        if (flowButtons.Count > 0) Require(flowButtons.All(b => b.Parent!.ClientRectangle.Contains(b.Bounds)), "Action buttons fully visible: " + name);
        using var bitmap = new Bitmap(form.Width, form.Height); form.DrawToBitmap(bitmap, new Rectangle(Point.Empty, form.Size)); bitmap.Save(Path.Combine(Output, name + ".png"), ImageFormat.Png);
    }
    private static IEnumerable<Control> Descendants(Control parent) => parent.Controls.Cast<Control>().SelectMany(c => new[] { c }.Concat(Descendants(c)));
    private static T Find<T>(Control root, Func<T, bool> predicate) where T : Control => Descendants(root).OfType<T>().First(predicate);
    private static void Require(bool success, string name) { if (!success) throw new InvalidOperationException("Failed: " + name); assertions++; Console.WriteLine("PASS " + name); }
    private static async Task Wait(Func<bool> ready)
    { var timer = Stopwatch.StartNew(); while (!ready()) { if (timer.Elapsed > TimeSpan.FromSeconds(30)) throw new TimeoutException("Desktop UI did not finish loading."); await Task.Delay(10); } }
    private sealed class TestDialogs : IDesktopDialogs
    { public bool Answer { get; set; } = true; public bool Confirm(IWin32Window owner, string title, string message) => Answer; }
    private sealed class OfflineHandler : HttpMessageHandler
    { public int Count { get; private set; } protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) { Count++; throw new HttpRequestException("Simulated connection loss"); } }
}
