using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Hospital.Application.Common.Exceptions;
using Hospital.Application.Common.Interfaces;
using Hospital.Application.Common.Models;
using Hospital.Application.DTOs.Appointments;
using Hospital.Application.Services.Interfaces;
using Hospital.Domain.Entities;
using Hospital.Domain.Enums;
using Hospital.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Hospital.Infrastructure.Services;

public sealed class AppointmentService(HospitalDbContext db, ICurrentUserService actor, ClinicTime clock,
    NotificationDispatcher dispatcher, ILogger<AppointmentService> logger) : IAppointmentService
{
    private IQueryable<Appointment> Details => db.Appointments.Include(a => a.Patient).ThenInclude(p => p.User)
        .Include(a => a.Doctor).Include(a => a.Specialty).Include(a => a.StatusHistories);

    private IQueryable<Appointment> Scoped()
    {
        actor.RequireRole("Patient", "Doctor", "Staff");
        return actor.Role switch
        {
            "Patient" when actor.PatientId.HasValue => Details.Where(a => a.PatientId == actor.PatientId),
            "Doctor" when actor.DoctorId.HasValue => Details.Where(a => a.DoctorId == actor.DoctorId),
            "Staff" => Details,
            _ => throw new ForbiddenAccessException()
        };
    }

    public async Task<AppointmentDto> GetAsync(Guid id) => Map(await Scoped().AsNoTracking()
        .SingleOrDefaultAsync(a => a.Id == id) ?? throw new NotFoundException("Lịch hẹn", id));

    public async Task<PagedResult<AppointmentDto>> ListAsync(AppointmentQuery request)
    {
        DatabaseWork.Validate(request);
        if (request.FromDate > request.ToDate) throw new BusinessRuleException("Khoảng ngày không hợp lệ.");
        if (request.Status.HasValue && !Enum.IsDefined(request.Status.Value))
            throw new BusinessRuleException("Trạng thái không hợp lệ.");
        var query = Scoped().AsNoTracking();
        if (request.FromDate.HasValue) query = query.Where(a => a.AppointmentDate >= request.FromDate);
        if (request.ToDate.HasValue) query = query.Where(a => a.AppointmentDate <= request.ToDate);
        if (request.Status.HasValue) query = query.Where(a => a.Status == request.Status);
        if (request.DoctorId.HasValue) query = query.Where(a => a.DoctorId == request.DoctorId);
        if (request.PatientId.HasValue) query = query.Where(a => a.PatientId == request.PatientId);
        if (!string.IsNullOrWhiteSpace(request.Keyword))
        {
            var k = request.Keyword.Trim();
            query = query.Where(a => a.AppointmentCode.Contains(k) || a.Patient.User.FullName.Contains(k)
                || (a.Patient.User.PhoneNumber != null && a.Patient.User.PhoneNumber.Contains(k)));
        }
        query = request.Descending
            ? query.OrderByDescending(a => a.AppointmentDate).ThenByDescending(a => a.StartTime).ThenBy(a => a.Id)
            : query.OrderBy(a => a.AppointmentDate).ThenBy(a => a.StartTime).ThenBy(a => a.Id);
        var total = await query.CountAsync();
        var rows = await query.Skip((request.Page - 1) * request.PageSize).Take(request.PageSize).ToListAsync();
        return new(rows.Select(Map).ToList(), total, request.Page, request.PageSize);
    }

    public async Task<AppointmentDto> BookAsync(BookAppointmentRequest request)
    {
        var userId = actor.RequireRole("Patient", "Staff");
        DatabaseWork.Validate(request);
        if (request.RequestKey == Guid.Empty) throw new BusinessRuleException("Cần RequestKey để chống gửi lặp.", "REQUEST_KEY_REQUIRED");
        var patientId = actor.Role == "Patient" ? actor.PatientId : request.PatientId;
        if (!patientId.HasValue || (actor.Role == "Patient" && request.PatientId.HasValue && request.PatientId != patientId))
            throw new ForbiddenAccessException("Hồ sơ bệnh nhân không hợp lệ.");
        var fingerprint = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(
            new { PatientId = patientId.Value, request.TimeSlotId, request.SpecialtyId, request.Notes }))));
        var id = await db.AtomicAsync(async () =>
        {
            var existing = await db.Appointments.SingleOrDefaultAsync(a => a.BookedBy == userId && a.RequestKey == request.RequestKey);
            if (existing != null)
            {
                if (existing.RequestFingerprint != fingerprint)
                    throw new BusinessRuleException("RequestKey đã được dùng cho nội dung khác.", "IDEMPOTENCY_KEY_REUSED");
                return existing.Id;
            }
            var slot = await ValidateSlotAsync(patientId.Value, request.TimeSlotId, request.SpecialtyId);
            var newId = Guid.NewGuid();
            var appointment = new Appointment
            {
                Id = newId, AppointmentCode = "LH-" + newId.ToString("N")[..17].ToUpperInvariant(),
                PatientId = patientId.Value, BookedBy = userId, Notes = request.Notes?.Trim(),
                RequestKey = request.RequestKey, RequestFingerprint = fingerprint, CreatedAt = clock.UtcNow
            };
            Assign(appointment, slot, request.SpecialtyId);
            db.Appointments.Add(appointment);
            History(appointment, null, null, slot.Id, "Đặt lịch khám");
            db.Audit(userId, "BOOK_APPOINTMENT", "Appointment", newId, newValue: new { slot.Id, patientId });
            Queue(appointment, NotificationType.Confirmed, "Đặt lịch thành công");
            Reminder(appointment);
            return newId;
        });
        await DeliverAsync();
        return await GetAsync(id);
    }

    // UC14 is shared by booking and rescheduling and runs inside the saving transaction.
    private async Task<TimeSlot> ValidateSlotAsync(Guid patientId, Guid slotId, int specialtyId, Guid? excludedId = null)
    {
        if (!await db.Patients.AnyAsync(p => p.Id == patientId && p.User.IsActive && p.User.RoleId == (int)UserRole.Patient))
            throw new BusinessRuleException("Bệnh nhân không tồn tại hoặc tài khoản không hoạt động.", "PATIENT_INACTIVE");
        var slot = await db.TimeSlots.Include(s => s.Doctor).ThenInclude(d => d.User)
            .Include(s => s.WorkingSchedule).SingleOrDefaultAsync(s => s.Id == slotId)
            ?? throw new NotFoundException("Khung giờ", slotId);
        if (!slot.Doctor.IsActive || (slot.Doctor.User != null && !slot.Doctor.User.IsActive)
            || !await db.Doctors.AnyAsync(d => d.Id == slot.DoctorId
                && d.SpecialtyId == specialtyId && d.Specialty.IsActive))
            throw new BusinessRuleException("Bác sĩ hoặc chuyên khoa không khả dụng.", "DOCTOR_SPECIALTY_INACTIVE");
        if (!slot.IsActive || slot.Status != TimeSlotStatus.Open || clock.ToUtc(slot.SlotDate, slot.StartTime) <= clock.UtcNow
            || slot.WorkingSchedule.WorkDate != slot.SlotDate || slot.WorkingSchedule.DoctorId != slot.DoctorId
            || slot.StartTime < slot.WorkingSchedule.StartTime || slot.EndTime > slot.WorkingSchedule.EndTime)
            throw new BusinessRuleException("Khung giờ không khả dụng.", "TIMESLOT_UNAVAILABLE");
        if (await db.Appointments.AnyAsync(a => a.TimeSlotId == slot.Id && a.Id != excludedId
            && (a.Status == AppointmentStatus.Confirmed || a.Status == AppointmentStatus.CheckedIn)))
            throw new BusinessRuleException("Khung giờ đã có người đặt.", "TIMESLOT_OCCUPIED");
        if (await db.Appointments.AnyAsync(a => a.PatientId == patientId && a.Id != excludedId
            && a.AppointmentDate == slot.SlotDate && a.StartTime < slot.EndTime && a.EndTime > slot.StartTime
            && (a.Status == AppointmentStatus.Confirmed || a.Status == AppointmentStatus.CheckedIn)))
            throw new BusinessRuleException("Bệnh nhân có lịch hẹn chồng lấn.", "PATIENT_SCHEDULE_OVERLAP");
        return slot;
    }

    private async Task<Appointment> EditableAsync(Guid id, AppointmentMutationRequest request, bool staffOnly = false)
    {
        actor.RequireRole(staffOnly ? ["Staff"] : ["Patient", "Staff"]);
        DatabaseWork.Validate(request);
        return await Scoped().Include(a => a.TimeSlot).SingleOrDefaultAsync(a => a.Id == id)
            ?? throw new NotFoundException("Lịch hẹn", id);
    }

    private void ValidateChange(Appointment appointment, AppointmentMutationRequest request)
    {
        if (appointment.Status != AppointmentStatus.Confirmed)
            throw new BusinessRuleException("Chỉ được đổi/hủy lịch Đã xác nhận.", "INVALID_APPOINTMENT_STATUS");
        var start = clock.ToUtc(appointment.AppointmentDate, appointment.StartTime);
        if (actor.Role == "Patient" ? start - clock.UtcNow < TimeSpan.FromHours(2) : start <= clock.UtcNow)
            throw new BusinessRuleException("Đã quá thời hạn đổi/hủy lịch.", "CHANGE_DEADLINE_PASSED");
        if (actor.Role == "Staff" && string.IsNullOrWhiteSpace(request.Reason))
            throw new BusinessRuleException("Nhân viên phải cung cấp lý do hỗ trợ.", "REASON_REQUIRED");
        DatabaseWork.CheckVersion(appointment.RowVersion, request.RowVersion);
    }

    public async Task<AppointmentDto> RescheduleAsync(Guid id, RescheduleAppointmentRequest request)
    {
        await db.AtomicAsync(async () =>
        {
            var a = await EditableAsync(id, request);
            ValidateChange(a, request);
            if (a.TimeSlotId == request.TimeSlotId && a.SpecialtyId == request.SpecialtyId) return id;
            if (a.TimeSlotId == request.TimeSlotId)
                throw new BusinessRuleException("Đổi lịch cần chọn khung giờ mới.");
            var slot = await ValidateSlotAsync(a.PatientId, request.TimeSlotId, request.SpecialtyId, id);
            var oldSlotId = a.TimeSlotId;
            a.TimeSlot.Status = TimeSlotStatus.Open;
            Assign(a, slot, request.SpecialtyId);
            a.RescheduleReason = request.Reason?.Trim();
            a.UpdatedAt = clock.UtcNow;
            History(a, a.Status, oldSlotId, slot.Id, request.Reason);
            db.Audit(actor.UserId!.Value, "RESCHEDULE_APPOINTMENT", "Appointment", id,
                new { TimeSlotId = oldSlotId }, new { TimeSlotId = slot.Id, request.Reason });
            await SuppressRemindersAsync(id);
            Queue(a, NotificationType.Rescheduled, "Đổi lịch thành công");
            Reminder(a);
            return id;
        });
        await DeliverAsync();
        return await GetAsync(id);
    }

    public async Task<AppointmentDto> CancelAsync(Guid id, AppointmentMutationRequest request)
    {
        await db.AtomicAsync(async () =>
        {
            var a = await EditableAsync(id, request);
            if (a.Status == AppointmentStatus.Cancelled) return id;
            ValidateChange(a, request);
            var old = a.Status;
            a.Status = AppointmentStatus.Cancelled;
            a.CancellationReason = request.Reason?.Trim();
            a.UpdatedAt = clock.UtcNow;
            a.TimeSlot.Status = TimeSlotStatus.Open;
            History(a, old, a.TimeSlotId, null, request.Reason);
            db.Audit(actor.UserId!.Value, "CANCEL_APPOINTMENT", "Appointment", id, new { Status = old },
                new { a.Status, request.Reason });
            await SuppressRemindersAsync(id);
            Queue(a, NotificationType.Cancelled, "Hủy lịch thành công");
            return id;
        });
        await DeliverAsync();
        return await GetAsync(id);
    }

    public Task<AppointmentDto> CheckInAsync(Guid id, AppointmentMutationRequest request) => ReceiveAsync(id, request, false);
    public Task<AppointmentDto> NoShowAsync(Guid id, AppointmentMutationRequest request) => ReceiveAsync(id, request, true);

    private async Task<AppointmentDto> ReceiveAsync(Guid id, AppointmentMutationRequest request, bool noShow)
    {
        await db.AtomicAsync(async () =>
        {
            var a = await EditableAsync(id, request, staffOnly: true);
            var status = noShow ? AppointmentStatus.NoShow : AppointmentStatus.CheckedIn;
            if (a.Status == status) return id;
            DatabaseWork.CheckVersion(a.RowVersion, request.RowVersion);
            if (a.Status != AppointmentStatus.Confirmed)
                throw new BusinessRuleException("Chỉ xử lý lịch Đã xác nhận.", "INVALID_APPOINTMENT_STATUS");
            var end = clock.ToUtc(a.AppointmentDate, a.EndTime);
            if (noShow ? end >= clock.UtcNow : a.AppointmentDate != clock.Today || end <= clock.UtcNow)
                throw new BusinessRuleException(noShow ? "Khung giờ chưa kết thúc." : "Chỉ tiếp nhận đúng ngày khám, trước khi hết khung giờ.", "INVALID_RECEPTION_TIME");
            a.Status = status;
            a.UpdatedAt = clock.UtcNow;
            if (noShow)
            {
                a.MarkedNoShowAt = clock.UtcNow;
                a.MarkedNoShowBy = actor.UserId;
                a.TimeSlot.Status = TimeSlotStatus.Open;
            }
            else
            {
                a.CheckedInAt = clock.UtcNow;
                a.CheckedInBy = actor.UserId;
            }
            History(a, AppointmentStatus.Confirmed, a.TimeSlotId, a.TimeSlotId, request.Reason);
            db.Audit(actor.UserId!.Value, noShow ? "MARK_NO_SHOW" : "CHECK_IN", "Appointment", id,
                new { Status = AppointmentStatus.Confirmed }, new { a.Status });
            await SuppressRemindersAsync(id);
            return id;
        });
        return await GetAsync(id);
    }

    private static void Assign(Appointment a, TimeSlot slot, int specialtyId)
    {
        a.TimeSlotId = slot.Id; a.TimeSlot = slot; a.DoctorId = slot.DoctorId; a.SpecialtyId = specialtyId;
        a.AppointmentDate = slot.SlotDate; a.StartTime = slot.StartTime; a.EndTime = slot.EndTime;
        slot.Status = TimeSlotStatus.Occupied;
    }

    private void History(Appointment a, AppointmentStatus? old, Guid? oldSlot, Guid? newSlot, string? reason) =>
        db.AppointmentStatusHistories.Add(new AppointmentStatusHistory
        {
            AppointmentId = a.Id, OldStatus = old, NewStatus = a.Status, ChangedBy = actor.UserId!.Value,
            ChangedAt = clock.UtcNow, OldTimeSlotId = oldSlot, NewTimeSlotId = newSlot, Reason = reason?.Trim()
        });

    private void Queue(Appointment a, NotificationType type, string title, DateTime? at = null) =>
        db.NotificationOutbox.Add(new NotificationOutbox
        {
            Id = Guid.NewGuid(), PatientId = a.PatientId, AppointmentId = a.Id, Type = type,
            Title = title, Message = $"Lịch {a.AppointmentCode}: {a.StartTime:HH:mm} ngày {a.AppointmentDate:dd/MM/yyyy}.",
            ScheduledFor = at ?? clock.UtcNow
        });

    private void Reminder(Appointment a)
    {
        var at = clock.ToUtc(a.AppointmentDate, a.StartTime).AddHours(-1);
        if (at > clock.UtcNow) Queue(a, NotificationType.Reminder, "Nhắc lịch khám sau 1 giờ", at);
    }

    private async Task SuppressRemindersAsync(Guid appointmentId)
    {
        var pending = await db.NotificationOutbox.Where(n => n.AppointmentId == appointmentId
            && n.Type == NotificationType.Reminder && n.DeliveredAt == null && n.SuppressedAt == null).ToListAsync();
        foreach (var item in pending) item.SuppressedAt = clock.UtcNow;
    }

    private async Task DeliverAsync()
    {
        try { await dispatcher.DispatchAsync(); }
        catch (Exception ex) { logger.LogWarning(ex, "Lịch đã lưu; thông báo sẽ được xử lý lại từ outbox."); }
    }

    private static AppointmentDto Map(Appointment a) => new()
    {
        Id = a.Id, AppointmentCode = a.AppointmentCode, PatientId = a.PatientId,
        PatientName = a.Patient.User.FullName, PatientPhone = a.Patient.User.PhoneNumber,
        DoctorId = a.DoctorId, DoctorName = a.Doctor.FullName, TimeSlotId = a.TimeSlotId,
        SpecialtyId = a.SpecialtyId, SpecialtyName = a.Specialty.Name, AppointmentDate = a.AppointmentDate,
        StartTime = a.StartTime, EndTime = a.EndTime, Status = a.Status, Notes = a.Notes,
        BookedBy = a.BookedBy, CheckedInAt = a.CheckedInAt, CheckedInBy = a.CheckedInBy,
        MarkedNoShowAt = a.MarkedNoShowAt, MarkedNoShowBy = a.MarkedNoShowBy, RowVersion = a.RowVersion,
        History = a.StatusHistories.OrderBy(h => h.ChangedAt).ThenBy(h => h.Id).Select(h =>
            new AppointmentHistoryDto(h.OldStatus, h.NewStatus, h.Reason, h.ChangedBy, h.ChangedAt, h.OldTimeSlotId, h.NewTimeSlotId)).ToList()
    };
}
