using System.Text.Json;
using Hospital.Application.DTOs.Admin;
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
    private sealed record Phase7Fixture(string RunId, Guid DoctorId, string DoctorName, string DoctorUsername,
        int SpecialtyId, string SpecialtyName, Guid TodayScheduleId, Guid TodaySlotId, DateOnly Today,
        Guid FutureScheduleId, DateOnly FutureDate, Guid[] FutureSlots);

    private static async Task RunPhase7Async(string[] args)
    {
        if (args is not ["--phase7", var stage, var file] || stage is not ("setup" or "confirmed" or "received" or "released"))
            throw new ArgumentException("Expected --phase7 setup|confirmed|received|released <fixture.json>.");
        if (Environment.GetEnvironmentVariable("HOSPITAL_PHASE7_ISOLATED") != "true")
            throw new InvalidOperationException("Use the isolated Phase 7 runner; HOSPITAL_PHASE7_ISOLATED=true is required.");
        var fixturePath = Path.GetFullPath(file);
        Output = Path.Combine(Path.GetDirectoryName(fixturePath)!, "desktop"); Directory.CreateDirectory(Output);
        using var http = new HttpClient { BaseAddress = new Uri(Environment.GetEnvironmentVariable("HOSPITAL_API_URL") ?? throw new InvalidOperationException("HOSPITAL_API_URL required")), Timeout = TimeSpan.FromSeconds(15) };
        var api = new HospitalApiClient(http);
        var auth = await api.SendAsync<AuthResponseDto>(HttpMethod.Post, "api/auth/login", new LoginRequestDto { Username = "admin", Password = "Admin@123" });
        var session = new AdminSession(api, auth);
        if (stage == "setup")
        {
            if (File.Exists(fixturePath)) throw new InvalidOperationException("Use a new fixture path for each run.");
            var now = DateTime.UtcNow.AddHours(7);
            var start = now.Date.AddMinutes((Math.Floor(now.TimeOfDay.TotalMinutes / 30) + 2) * 30);
            if (start.AddMinutes(30).Date != now.Date) throw new InvalidOperationException("Run the same-day browser reception scenario before 22:30 Vietnam time; no time branch is skipped.");
            var run = Guid.NewGuid().ToString("N")[..10]; SpecialtyDto specialty;
            using (var form = new SpecialtyEditorForm(session, Dialogs))
            { ShowHidden(form); Fill(form, "Code", "P7" + run); Fill(form, "Name", "Chuyên khoa P7 " + run); await Submit(form); specialty = (SpecialtyDto)form.SavedResult!; }
            var options = await session.AllAsync<SpecialtyDto>("api/specialties?onlyActive=false", default);
            DoctorDetailDto doctor;
            using (var form = new DoctorEditorForm(session, Dialogs, options))
            {
                ShowHidden(form); Fill(form, "FullName", "Bác sĩ P7 " + run); Fill(form, "Title", "BS.CKI");
                var list = Field<CheckedListBox>(form, "SpecialtyIds"); list.SetItemChecked(Enumerable.Range(0, list.Items.Count).Single(i => ((Choice<int>)list.Items[i]).Value == specialty.Id), true);
                Capture(form, "create-doctor"); await Submit(form); doctor = (DoctorDetailDto)form.SavedResult!;
            }
            var username = "p7doctor" + run;
            using (var form = new UserEditorForm(session, Dialogs))
            {
                ShowHidden(form); Fill(form, "Username", username); Fill(form, "Email", username + "@example.test"); Fill(form, "Password", "Doctor@123");
                Select(form, "Role", UserRole.Doctor); await PickDoctor(form, doctor.Id); await Submit(form);
            }
            var doctorRow = (await session.AllAsync<DoctorDto>("api/doctors?onlyActive=false", default)).Single(d => d.Id == doctor.Id);
            async Task<WorkingScheduleDto> Schedule(DateTime begins, DateTime ends)
            {
                using var form = new ScheduleEditorForm(session, Dialogs, selectedDoctor: doctorRow); ShowHidden(form);
                Field<DateTimePicker>(form, "WorkDate").Value = begins.Date;
                Field<DateTimePicker>(form, "StartTime").Value = DateTime.Today + begins.TimeOfDay;
                Field<DateTimePicker>(form, "EndTime").Value = DateTime.Today + ends.TimeOfDay;
                Fill(form, "Note", "Phase 7 browser/Desktop integration " + run); Capture(form, "create-schedule"); await Submit(form);
                return (WorkingScheduleDto)form.SavedResult!;
            }
            var today = await Schedule(start, start.AddMinutes(30));
            var future = await Schedule(now.Date.AddDays(3).AddHours(8), now.Date.AddDays(3).AddHours(10));
            var fixture = new Phase7Fixture(run, doctor.Id, doctor.FullName, username, specialty.Id, specialty.Name,
                today.Id, today.TimeSlots.Single().Id, today.WorkDate, future.Id, future.WorkDate, future.TimeSlots.OrderBy(s => s.StartTime).Select(s => s.Id).ToArray());
            await File.WriteAllTextAsync(fixturePath, JsonSerializer.Serialize(fixture, HospitalApiClient.Json));
            Console.WriteLine("PHASE7 setup: doctor, account, specialty and schedules created using WinForms editors."); return;
        }
        var data = JsonSerializer.Deserialize<Phase7Fixture>(await File.ReadAllTextAsync(fixturePath), HospitalApiClient.Json)!;
        var schedule = await session.GetAsync<WorkingScheduleDto>($"api/schedules/{data.TodayScheduleId}", default);
        var slot = schedule.TimeSlots.Single(s => s.Id == data.TodaySlotId);
        if (stage is "confirmed" or "received")
        {
            Require(slot.Status == TimeSlotStatus.Occupied, "Desktop sees Web booking after " + stage);
            using (var form = new CloseSlotForm(session, Dialogs, slot))
            {
                ShowHidden(form); Fill(form, "Reason", "Phase 7 protection check"); await Rejected(form);
                Require(Banner(form).Contains("lịch hẹn", StringComparison.OrdinalIgnoreCase), "API rejects Desktop close after " + stage); Capture(form, "blocked-" + stage); form.Close();
            }
            using var detail = new ScheduleDetailsForm(session, Dialogs, schedule.Id); ShowHidden(detail);
            await Wait(() => Button(detail, "Làm mới").Enabled && Banner(detail).Contains("hiện hành"));
            Require(!Button(detail, "Đóng khung giờ").Enabled, "Desktop disables close for occupied slot"); Capture(detail, "schedule-" + stage); detail.Close();
        }
        else
        {
            var future = await session.GetAsync<WorkingScheduleDto>($"api/schedules/{data.FutureScheduleId}", default);
            Require(future.TimeSlots.All(s => s.Status == TimeSlotStatus.Open), "Web cancellations released all future slots in Desktop");
            using var close = new CloseSlotForm(session, Dialogs, future.TimeSlots.Single(s => s.Id == data.FutureSlots[0]));
            ShowHidden(close); Fill(close, "Reason", "Đã xử lý lịch hẹn, đóng khung qua Desktop"); await Submit(close);
            var logs = await session.AllAsync<AuditLogDto>("api/admin/audit-logs?entityType=TimeSlot", default);
            Require(logs.Any(a => a.EntityId == data.FutureSlots[0].ToString() && a.Action == "CLOSE_TIMESLOT"), "Successful Desktop closure audited");
        }
        Console.WriteLine($"PHASE7 {stage}: {assertions} Desktop assertions passed.");
    }
}
