using System.Text.Json;
using Hospital.Application.DTOs.Admin;
using Hospital.Application.DTOs.Auth;
using Hospital.Application.DTOs.Catalog;
using Hospital.Application.DTOs.Schedules;
using Hospital.Client;
using Hospital.Desktop.Design;
using Hospital.Desktop.Forms;
using Hospital.Desktop.Services;

internal static partial class Program
{
    private static async Task RunPhase8Async(string file)
    {
        if (Environment.GetEnvironmentVariable("HOSPITAL_PHASE7_ISOLATED") != "true") throw new InvalidOperationException("Use the isolated final QA runner.");
        Output = Path.Combine(Path.GetDirectoryName(Path.GetFullPath(file))!, "final-desktop"); Directory.CreateDirectory(Output);
        var data = JsonSerializer.Deserialize<Phase7Fixture>(await File.ReadAllTextAsync(file), HospitalApiClient.Json)!;
        using var http = new HttpClient { BaseAddress = new Uri(Environment.GetEnvironmentVariable("HOSPITAL_API_URL")!), Timeout = TimeSpan.FromSeconds(15) };
        var api = new HospitalApiClient(http);
        var auth = await api.SendAsync<AuthResponseDto>(HttpMethod.Post, "api/auth/login", new LoginRequestDto { Username = "admin", Password = "Admin@123" });
        var session = new AdminSession(api, auth);
        var screens = new List<object>();
        void CaptureSizes(Form form, string name)
        {
            Capture(form, name); var standard = form.Size; form.Size = form.MinimumSize; Capture(form, name + "-minimum");
            screens.Add(new { name, dpi = form.DeviceDpi, standardWidth = standard.Width, standardHeight = standard.Height, minimumWidth = form.Width, minimumHeight = form.Height });
            form.Size = standard;
        }
        async Task Review(Form form, string name, Func<Task>? loaded = null)
        {
            using (form) { ShowHidden(form); if (loaded != null) await loaded(); CaptureSizes(form, name); form.Close(); }
        }
        await Review(new LoginForm(), "login");
        using (var shell = new AdminShellForm(api, auth, Dialogs))
        {
            ShowHidden(shell); await Loaded(shell); CaptureSizes(shell, "dashboard");
            foreach (var (label, name) in new[] { ("Tài khoản", "users"), ("Bác sĩ", "doctors"), ("Chuyên khoa", "specialties"), ("Lịch làm việc", "schedules"), ("Nhật ký", "audit") })
            { Button(shell, label).PerformClick(); await Loaded(shell); CaptureSizes(shell, name); }
            shell.Close();
        }
        var specialties = await session.AllAsync<SpecialtyDto>("api/specialties?onlyActive=false", default);
        var doctor = await session.GetAsync<DoctorDetailDto>($"api/doctors/{data.DoctorId}", default);
        var user = (await session.AllAsync<UserDto>("api/admin/users", default)).Single(u => u.Username == data.DoctorUsername);
        var schedule = await session.GetAsync<WorkingScheduleDto>($"api/schedules/{data.FutureScheduleId}", default);
        var profile = await session.GetAsync<UserProfileDto>("api/auth/profile", default);
        var audit = (await session.AllAsync<AuditLogDto>("api/admin/audit-logs", default)).First();
        await Review(new UserEditorForm(session, Dialogs), "user-create");
        await Review(new UserEditorForm(session, Dialogs, user), "user-edit");
        await Review(new RoleEditorForm(session, Dialogs, user), "role");
        await Review(new SpecialtyEditorForm(session, Dialogs, specialties.Single(s => s.Id == data.SpecialtyId)), "specialty-edit");
        await Review(new DoctorEditorForm(session, Dialogs, specialties), "doctor-create");
        await Review(new DoctorEditorForm(session, Dialogs, specialties, doctor), "doctor-edit");
        var picker = new DoctorPickerForm(session);
        await Review(picker, "doctor-picker", () => Wait(() => Button(picker, "Tìm kiếm").Enabled && Banner(picker).Contains("hồ sơ")));
        await Review(new ScheduleEditorForm(session, Dialogs), "schedule-create");
        await Review(new ScheduleEditorForm(session, Dialogs, schedule), "schedule-edit");
        var details = new ScheduleDetailsForm(session, Dialogs, schedule.Id);
        await Review(details, "schedule-details", () => Wait(() => Button(details, "Làm mới").Enabled && Banner(details).Contains("hiện hành")));
        await Review(new CloseSlotForm(session, Dialogs, schedule.TimeSlots.First()), "close-slot");
        await Review(new ProfileEditorForm(session, Dialogs, profile), "profile");
        await Review(new PasswordEditorForm(session, Dialogs), "password");
        await Review(new AuditDetailForm(audit), "audit-detail");
        await Review(new ConfirmForm("Xác nhận thay đổi", "Kiểm tra thông tin và lưu thay đổi này vào hệ thống?"), "confirmation");
        await File.WriteAllTextAsync(Path.Combine(Output, "screens.json"), JsonSerializer.Serialize(screens, HospitalApiClient.Json));
        Console.WriteLine($"PHASE8 Desktop visual review: {screens.Count} screens, {assertions} assertions passed; native message loop, recorded DPI and real API data.");
    }
}
