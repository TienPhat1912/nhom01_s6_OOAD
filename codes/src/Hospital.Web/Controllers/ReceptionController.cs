using System.Net;
using Hospital.Application.DTOs.Appointments;
using Hospital.Client;
using Hospital.Web.Models;
using Hospital.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Hospital.Web.Controllers;

[Authorize(Roles = "Patient,Staff")]
public class ReceptionController(PortalApi api) : Controller
{
    [HttpGet] public Task<IActionResult> Cancel(Guid id) => Show(id, nameof(Cancel));
    [HttpPost] public Task<IActionResult> Cancel(Guid id, AppointmentActionModel model) => Save(id, model, nameof(Cancel), "cancel");
    [HttpGet, Authorize(Roles = "Staff")] public Task<IActionResult> CheckIn(Guid id) => Show(id, nameof(CheckIn));
    [HttpPost, Authorize(Roles = "Staff")] public Task<IActionResult> CheckIn(Guid id, AppointmentActionModel model) => Save(id, model, nameof(CheckIn), "check-in");
    [HttpGet, Authorize(Roles = "Staff")] public Task<IActionResult> NoShow(Guid id) => Show(id, nameof(NoShow));
    [HttpPost, Authorize(Roles = "Staff")] public Task<IActionResult> NoShow(Guid id, AppointmentActionModel model) => Save(id, model, nameof(NoShow), "no-show");

    private async Task<IActionResult> Show(Guid id, string operation)
    {
        var appointment = await api.GetAsync<AppointmentDto>($"api/appointments/{id}");
        return View("Action", new AppointmentActionModel { Appointment = appointment, Operation = operation,
            RowVersion = Convert.ToBase64String(appointment.RowVersion) });
    }

    private async Task<IActionResult> Save(Guid id, AppointmentActionModel model, string operation, string endpoint)
    {
        model.Appointment = await api.GetAsync<AppointmentDto>($"api/appointments/{id}");
        model.Operation = operation;
        byte[] version = [];
        try { version = Convert.FromBase64String(model.RowVersion ?? ""); if (version.Length == 0 || version.Length > 64) throw new FormatException(); }
        catch (FormatException) { ModelState.AddModelError("", "Thông tin phiên bản không hợp lệ. Vui lòng tải lại lịch hẹn."); }
        if (operation == nameof(Cancel) && User.IsInRole("Staff") && string.IsNullOrWhiteSpace(model.Reason))
            ModelState.AddModelError(nameof(model.Reason), "Nhân viên cần ghi lý do hủy lịch.");
        if (operation == nameof(CheckIn) && !model.PatientVerified)
            ModelState.AddModelError(nameof(model.PatientVerified), "Cần đối chiếu bệnh nhân trước khi tiếp nhận.");
        if (!ModelState.IsValid) return View("Action", model);
        try
        {
            await api.SendAsync<AppointmentDto>(HttpMethod.Post, $"api/appointments/{id}/{endpoint}",
                new AppointmentMutationRequest { RowVersion = version, Reason = model.Reason });
            TempData["Success"] = operation switch
            { nameof(Cancel) => "Lịch hẹn đã được hủy.", nameof(CheckIn) => "Đã tiếp nhận bệnh nhân.", _ => "Đã ghi nhận bệnh nhân không đến." };
            return RedirectToAction("Detail", "Appointments", new { id });
        }
        catch (HospitalApiException ex) when (ex.Status is not HttpStatusCode.Unauthorized and not HttpStatusCode.Forbidden)
        {
            ModelState.AddModelError("", ex.Message);
            ViewData["TraceId"] = ex.TraceId;
            // Keep the version the user confirmed. Refreshing it here could overwrite a concurrent change.
            return View("Action", model);
        }
    }
}
