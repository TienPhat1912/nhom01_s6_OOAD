using Hospital.Application.Common.Exceptions;
using Hospital.Application.Common.Interfaces;
using Hospital.Application.DTOs.Schedules;
using Hospital.Application.Services.Interfaces;
using Hospital.Domain.Entities;
using Hospital.Domain.Enums;
using Hospital.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Hospital.Infrastructure.Services;

public class ScheduleService(HospitalDbContext db, ICurrentUserService actor, ClinicTime clock) : IScheduleService
{
    private IQueryable<WorkingSchedule> Schedules => db.WorkingSchedules.Include(s => s.Doctor).Include(s => s.TimeSlots);

    public async Task<List<TimeSlotDto>> GetAvailableTimeSlotsAsync(Guid doctorId, DateOnly date)
    {
        actor.RequireRole("Patient", "Staff", "Admin");
        if (!await db.Doctors.AnyAsync(d => d.Id == doctorId && d.IsActive && (d.User == null || d.User.IsActive)
            && d.DoctorSpecialties.Any(ds => ds.Specialty.IsActive)))
            throw new NotFoundException("Bác sĩ", doctorId);
        if (date < clock.Today) return [];
        var now = TimeOnly.FromDateTime(clock.LocalNow);
        var today = clock.Today;
        return await db.TimeSlots.AsNoTracking().Where(s => s.DoctorId == doctorId && s.SlotDate == date
                && s.IsActive && s.Status == TimeSlotStatus.Open && (s.SlotDate > today || s.StartTime > now)
                && !s.Appointments.Any(a => a.Status == AppointmentStatus.Confirmed || a.Status == AppointmentStatus.CheckedIn))
            .OrderBy(s => s.StartTime).Select(s => new TimeSlotDto
            {
                Id = s.Id, WorkingScheduleId = s.WorkingScheduleId, DoctorId = s.DoctorId, DoctorName = s.Doctor.FullName,
                SlotDate = s.SlotDate, StartTime = s.StartTime, EndTime = s.EndTime, Status = s.Status, IsActive = s.IsActive
            }).ToListAsync();
    }

    public async Task<List<WorkingScheduleDto>> GetWorkingSchedulesAsync(Guid? doctorId = null, DateOnly? fromDate = null, DateOnly? toDate = null)
    {
        actor.RequireRole("Doctor", "Admin");
        if (actor.Role == "Doctor") doctorId = actor.DoctorId ?? throw new ForbiddenAccessException();
        if (fromDate > toDate) throw new BusinessRuleException("Khoảng ngày không hợp lệ.");
        var from = fromDate ?? clock.Today;
        var to = toDate ?? (from.DayNumber > DateOnly.MaxValue.DayNumber - 31 ? DateOnly.MaxValue : from.AddDays(31));
        if (to.DayNumber - from.DayNumber > 366) throw new BusinessRuleException("Chỉ xem tối đa 366 ngày mỗi lần.");
        var query = Schedules.AsNoTracking().Where(s => s.WorkDate >= from && s.WorkDate <= to);
        if (doctorId.HasValue) query = query.Where(s => s.DoctorId == doctorId);
        return (await query.OrderBy(s => s.WorkDate).ThenBy(s => s.StartTime).ThenBy(s => s.Id).ToListAsync()).Select(Map).ToList();
    }

    public async Task<WorkingScheduleDto> GetWorkingScheduleByIdAsync(Guid id)
    {
        actor.RequireRole("Doctor", "Admin");
        var query = Schedules.AsNoTracking();
        if (actor.Role == "Doctor")
        {
            if (!actor.DoctorId.HasValue) throw new ForbiddenAccessException();
            query = query.Where(s => s.DoctorId == actor.DoctorId);
        }
        return Map(await query.SingleOrDefaultAsync(s => s.Id == id) ?? throw new NotFoundException("Lịch làm việc", id));
    }

    public async Task<WorkingScheduleDto> CreateWorkingScheduleAsync(Guid adminUserId, CreateWorkingScheduleRequestDto request)
    {
        if (actor.RequireRole("Admin") != adminUserId) throw new ForbiddenAccessException();
        DatabaseWork.Validate(request);
        var id = await db.AtomicAsync(async () =>
        {
            if (!await db.Doctors.AnyAsync(d => d.Id == request.DoctorId && d.IsActive && (d.User == null || d.User.IsActive)
                && d.DoctorSpecialties.Any(ds => ds.Specialty.IsActive))) throw new NotFoundException("Bác sĩ", request.DoctorId);
            await ValidateAsync(request.DoctorId, request.WorkDate, request.StartTime, request.EndTime);
            var schedule = new WorkingSchedule
            {
                Id = Guid.NewGuid(), DoctorId = request.DoctorId, WorkDate = request.WorkDate,
                StartTime = request.StartTime, EndTime = request.EndTime, Note = request.Note?.Trim(),
                CreatedBy = adminUserId, CreatedAt = clock.UtcNow
            };
            db.WorkingSchedules.Add(schedule);
            AddSlots(schedule);
            db.Audit(adminUserId, "CREATE_SCHEDULE", "WorkingSchedule", schedule.Id,
                newValue: new { schedule.WorkDate, schedule.StartTime, schedule.EndTime });
            return schedule.Id;
        });
        return await GetWorkingScheduleByIdAsync(id);
    }

    public async Task<WorkingScheduleDto> UpdateWorkingScheduleAsync(Guid adminUserId, Guid id, UpdateWorkingScheduleRequestDto request)
    {
        if (actor.RequireRole("Admin") != adminUserId) throw new ForbiddenAccessException();
        DatabaseWork.Validate(request);
        await db.AtomicAsync(async () =>
        {
            var s = await Schedules.SingleOrDefaultAsync(s => s.Id == id) ?? throw new NotFoundException("Lịch làm việc", id);
            await ValidateAsync(s.DoctorId, request.WorkDate, request.StartTime, request.EndTime, id);
            var old = new { s.WorkDate, s.StartTime, s.EndTime, s.Note };
            if (s.WorkDate != request.WorkDate || s.StartTime != request.StartTime || s.EndTime != request.EndTime)
            {
                var slotIds = s.TimeSlots.Select(t => t.Id).ToList();
                if (await db.Appointments.AnyAsync(a => slotIds.Contains(a.TimeSlotId)
                    && (a.Status == AppointmentStatus.Confirmed || a.Status == AppointmentStatus.CheckedIn)))
                    throw new BusinessRuleException("Ca trực còn lịch hẹn có hiệu lực; cần xử lý trước.", "ACTIVE_APPOINTMENTS_EXIST");
                // Retain old slots and their history. Only the replacement slots accept new bookings.
                foreach (var slot in s.TimeSlots)
                {
                    slot.IsActive = false; slot.Status = TimeSlotStatus.Closed;
                    slot.ClosedReason = "Điều chỉnh ca trực"; slot.ClosedBy = adminUserId; slot.ClosedAt = clock.UtcNow;
                }
                s.WorkDate = request.WorkDate; s.StartTime = request.StartTime; s.EndTime = request.EndTime;
                AddSlots(s);
            }
            s.Note = request.Note?.Trim(); s.UpdatedAt = clock.UtcNow;
            db.Audit(adminUserId, "UPDATE_SCHEDULE", "WorkingSchedule", id, old,
                new { s.WorkDate, s.StartTime, s.EndTime, s.Note });
            return id;
        });
        return await GetWorkingScheduleByIdAsync(id);
    }

    private async Task ValidateAsync(Guid doctorId, DateOnly date, TimeOnly start, TimeOnly end, Guid? excluded = null)
    {
        if (end <= start || start.Second != 0 || end.Second != 0 || (end - start).TotalMinutes % 30 != 0)
            throw new BusinessRuleException("Ca trực phải gồm các khung 30 phút, giờ kết thúc sau giờ bắt đầu.", "INVALID_TIME_RANGE");
        if (date < clock.Today || clock.ToUtc(date, start) <= clock.UtcNow)
            throw new BusinessRuleException("Không tạo/sửa ca trực đã bắt đầu.", "PAST_DATE_NOT_ALLOWED");
        if (await db.WorkingSchedules.AnyAsync(s => s.Id != excluded && s.DoctorId == doctorId && s.WorkDate == date
            && s.StartTime < end && s.EndTime > start))
            throw new BusinessRuleException("Ca trực chồng lấn với lịch hiện có.", "SCHEDULE_OVERLAP");
    }

    private void AddSlots(WorkingSchedule s)
    {
        for (var minutes = 0; minutes < (s.EndTime - s.StartTime).TotalMinutes; minutes += 30)
            db.TimeSlots.Add(new TimeSlot
            {
                Id = Guid.NewGuid(), WorkingScheduleId = s.Id, DoctorId = s.DoctorId, SlotDate = s.WorkDate,
                StartTime = s.StartTime.AddMinutes(minutes), EndTime = s.StartTime.AddMinutes(minutes + 30), CreatedAt = clock.UtcNow
            });
    }

    public async Task CloseTimeSlotAsync(Guid adminUserId, Guid slotId, CloseTimeSlotRequestDto request)
    {
        if (actor.RequireRole("Admin") != adminUserId) throw new ForbiddenAccessException();
        DatabaseWork.Validate(request);
        await db.AtomicAsync(async () =>
        {
            var slot = await db.TimeSlots.SingleOrDefaultAsync(s => s.Id == slotId) ?? throw new NotFoundException("Khung giờ", slotId);
            if (slot.Status == TimeSlotStatus.Closed) return slotId;
            if (clock.ToUtc(slot.SlotDate, slot.StartTime) <= clock.UtcNow)
                throw new BusinessRuleException("Khung giờ đã bắt đầu.", "SLOT_ALREADY_STARTED");
            if (await db.Appointments.AnyAsync(a => a.TimeSlotId == slotId
                && (a.Status == AppointmentStatus.Confirmed || a.Status == AppointmentStatus.CheckedIn)))
                throw new BusinessRuleException("Khung giờ còn lịch hẹn có hiệu lực.", "CANNOT_CLOSE_SLOT_WITH_CONFIRMED_APPOINTMENT");
            slot.Status = TimeSlotStatus.Closed; slot.IsActive = false;
            slot.ClosedReason = request.Reason.Trim(); slot.ClosedAt = clock.UtcNow; slot.ClosedBy = adminUserId;
            db.Audit(adminUserId, "CLOSE_TIMESLOT", "TimeSlot", slotId, newValue: new { request.Reason });
            return slotId;
        });
    }

    private static WorkingScheduleDto Map(WorkingSchedule s) => new()
    {
        Id = s.Id, DoctorId = s.DoctorId, DoctorName = s.Doctor.FullName, DoctorCode = s.Doctor.DoctorCode,
        WorkDate = s.WorkDate, StartTime = s.StartTime, EndTime = s.EndTime, Note = s.Note,
        SlotsCount = s.TimeSlots.Count(t => t.IsActive),
        OpenSlotsCount = s.TimeSlots.Count(t => t.Status == TimeSlotStatus.Open && t.IsActive),
        OccupiedSlotsCount = s.TimeSlots.Count(t => t.Status == TimeSlotStatus.Occupied),
        TimeSlots = s.TimeSlots.OrderBy(t => t.SlotDate).ThenBy(t => t.StartTime).Select(t => new TimeSlotDto
        {
            Id = t.Id, WorkingScheduleId = t.WorkingScheduleId, DoctorId = t.DoctorId, DoctorName = s.Doctor.FullName,
            SlotDate = t.SlotDate, StartTime = t.StartTime, EndTime = t.EndTime,
            IsActive = t.IsActive, Status = t.Status, ClosedReason = t.ClosedReason
        }).ToList()
    };
}
