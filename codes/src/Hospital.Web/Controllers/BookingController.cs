using System.Net;
using System.Security.Claims;
using System.Security.Cryptography;
using Hospital.Application.Common.Models;
using Hospital.Application.DTOs.Appointments;
using Hospital.Application.DTOs.Auth;
using Hospital.Application.DTOs.Catalog;
using Hospital.Application.DTOs.Schedules;
using Hospital.Client;
using Hospital.Web.Models;
using Hospital.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Hospital.Web.Controllers;

[Authorize(Roles = "Patient,Staff")]
public class BookingController(PortalApi api, BookingTokens tokens, AppointmentActions actions) : Controller
{
    private Guid ActorId => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpGet]
    public async Task<IActionResult> Start(Guid? patientId, Guid? appointmentId, int? specialtyId, Guid? doctorId)
    {
        AppointmentDto? original = null;
        if (appointmentId.HasValue)
        {
            original = await api.GetAsync<AppointmentDto>($"api/appointments/{appointmentId}");
            if (!actions.CanChange(original, User.IsInRole("Staff")))
                return Invalid("Lịch hẹn không còn đủ điều kiện đổi. Vui lòng xem lại chi tiết lịch hẹn.");
            patientId = original.PatientId;
        }
        if (User.IsInRole("Staff") && !patientId.HasValue) return RedirectToAction("Index", "Patients", new { specialtyId, doctorId });
        var patient = await Patient(patientId);
        var context = tokens.Protect(new BookingContext(ActorId, patient.PatientId!.Value, original?.Id, original?.RowVersion ?? []));
        if (doctorId.HasValue && specialtyId.HasValue)
            return RedirectToAction(nameof(Slots), new { context, specialtyId, doctorId });
        if (specialtyId.HasValue) return RedirectToAction(nameof(Doctors), new { context, specialtyId });
        return RedirectToAction(nameof(Index), new { context });
    }

    [HttpGet]
    public async Task<IActionResult> Index(string? context, string? keyword, int page = 1)
    {
        if (context == null) return RedirectToAction(nameof(Start));
        try
        {
            var model = await Page(context);
            ViewData["Keyword"] = keyword;
            model.Specialties = await api.GetAsync<PagedResult<SpecialtyDto>>(
                $"api/specialties?page={Math.Clamp(page, 1, 1000000)}&pageSize=9&keyword={Uri.EscapeDataString(keyword ?? "")}");
            return View(model);
        }
        catch (CryptographicException) { return Expired(); }
    }

    [HttpGet]
    public async Task<IActionResult> Doctors(string context, int specialtyId, string? keyword, int page = 1)
    {
        try
        {
            var model = await Page(context);
            model.SpecialtyId = specialtyId;
            model.Specialty = await api.GetAsync<SpecialtyDto>($"api/specialties/{specialtyId}");
            model.Doctors = await api.GetAsync<PagedResult<DoctorDto>>($"api/doctors?specialtyId={specialtyId}&page={Math.Clamp(page, 1, 1000000)}&pageSize=9&keyword={Uri.EscapeDataString(keyword ?? "")}");
            ViewData["Keyword"] = keyword;
            return View(model);
        }
        catch (CryptographicException) { return Expired(); }
    }

    [HttpGet]
    public async Task<IActionResult> Slots(string context, int specialtyId, Guid doctorId, DateOnly? date)
    {
        if (!ModelState.IsValid) return Invalid("Ngày hoặc thông tin lựa chọn không hợp lệ.");
        try { return View(await SlotPage(new SlotSelection { Context = context, SpecialtyId = specialtyId, DoctorId = doctorId, Date = date ?? default })); }
        catch (CryptographicException) { return Expired(); }
    }

    [HttpPost]
    public async Task<IActionResult> Review(SlotSelection selection)
    {
        try
        {
            var model = await SlotPage(selection);
            var slot = model.Slots.SingleOrDefault(s => s.Id == selection.TimeSlotId);
            if (slot == null) ModelState.AddModelError(nameof(selection.TimeSlotId), "Khung giờ không còn trống. Vui lòng chọn lại.");
            if (model.State.AppointmentId.HasValue && User.IsInRole("Staff") && string.IsNullOrWhiteSpace(selection.Reason))
                ModelState.AddModelError(nameof(selection.Reason), "Nhân viên cần ghi lý do đổi lịch.");
            if (!ModelState.IsValid) return View("Slots", model);
            var draft = new BookingDraft(model.State, Guid.NewGuid(), slot!.Id, model.Specialty.Id, model.Doctor.Id,
                model.Patient.FullName, model.Patient.PhoneNumber, model.Doctor.FullName, model.Specialty.Name,
                slot.SlotDate, slot.StartTime, slot.EndTime, selection.Notes, selection.Reason);
            return View(new BookingReview(tokens.Protect(draft), draft));
        }
        catch (CryptographicException) { return Expired(); }
    }

    [HttpPost]
    public async Task<IActionResult> Confirm(string draftToken)
    {
        BookingDraft draft;
        try
        {
            draft = tokens.Read<BookingDraft>(draftToken);
            CheckActor(draft.State);
        }
        catch (CryptographicException) { return Expired(); }
        try
        {
            // Retry exactly this protected request, including RequestKey and the original RowVersion.
            // A second availability check would incorrectly reject a successful booking whose response was lost.
            AppointmentDto result;
            if (draft.State.AppointmentId is Guid id)
                result = await api.SendAsync<AppointmentDto>(HttpMethod.Post, $"api/appointments/{id}/reschedule",
                    new RescheduleAppointmentRequest { TimeSlotId = draft.TimeSlotId, SpecialtyId = draft.SpecialtyId,
                        RowVersion = draft.State.RowVersion, Reason = draft.Reason });
            else
                result = await api.SendAsync<AppointmentDto>(HttpMethod.Post, "api/appointments", new BookAppointmentRequest
                { PatientId = draft.State.PatientId, TimeSlotId = draft.TimeSlotId, SpecialtyId = draft.SpecialtyId, RequestKey = draft.RequestKey, Notes = draft.Notes });
            TempData["Success"] = draft.State.AppointmentId.HasValue ? "Lịch hẹn đã được đổi thành công." : "Lịch hẹn đã được đặt thành công.";
            return RedirectToAction(nameof(Receipt), new { id = result.Id });
        }
        catch (HospitalApiException ex) when (ex.Status is not HttpStatusCode.Unauthorized and not HttpStatusCode.Forbidden)
        {
            ModelState.AddModelError("", ex.Message);
            ViewData["TraceId"] = ex.TraceId;
            ViewData["OutcomeUncertain"] = (int)ex.Status >= 500;
            return View("Review", new BookingReview(draftToken, draft));
        }
    }

    [HttpGet]
    public async Task<IActionResult> Receipt(Guid id) => View(await api.GetAsync<AppointmentDto>($"api/appointments/{id}"));

    private async Task<UserProfileDto> Patient(Guid? patientId) => User.IsInRole("Staff")
        ? await api.GetAsync<UserProfileDto>($"api/patients/{patientId}")
        : await api.GetAsync<UserProfileDto>("api/auth/profile");

    private async Task<BookingPage> Page(string context)
    {
        var state = tokens.Read<BookingContext>(context);
        CheckActor(state);
        var patient = await Patient(state.PatientId);
        if (patient.PatientId != state.PatientId) throw new CryptographicException();
        return new BookingPage { Context = context, State = state, Patient = patient };
    }

    private async Task<BookingPage> SlotPage(SlotSelection selection)
    {
        var model = await Page(selection.Context);
        model.SpecialtyId = selection.SpecialtyId; model.DoctorId = selection.DoctorId;
        model.Doctor = await api.GetAsync<DoctorDetailDto>($"api/doctors/{selection.DoctorId}");
        model.Specialty = model.Doctor.Specialties.SingleOrDefault(s => s.Id == selection.SpecialtyId && s.IsActive)
            ?? throw new HospitalApiException(HttpStatusCode.BadRequest, "Bác sĩ không thuộc chuyên khoa đã chọn.", null, []);
        model.Date = selection.Date == default ? model.Doctor.AvailableDates.FirstOrDefault(DisplayText.Today) : selection.Date;
        model.Slots = await api.GetAsync<List<TimeSlotDto>>($"api/timeslots/available?doctorId={selection.DoctorId}&date={model.Date:yyyy-MM-dd}");
        model.TimeSlotId = selection.TimeSlotId; model.Notes = selection.Notes; model.Reason = selection.Reason;
        return model;
    }

    private void CheckActor(BookingContext context)
    { if (context.ActorId != ActorId) throw new CryptographicException(); }
    private IActionResult Expired() => Invalid("Phiên chọn lịch đã hết hạn hoặc không hợp lệ. Vui lòng bắt đầu lại từ mục Đặt lịch.");
    private IActionResult Invalid(string message)
    {
        Response.StatusCode = 400;
        return View("ApiError", new UiMessage("Không thể tiếp tục đặt lịch", message, "error"));
    }
}
