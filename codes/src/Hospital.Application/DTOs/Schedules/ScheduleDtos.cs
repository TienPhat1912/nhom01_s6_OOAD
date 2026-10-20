using System.ComponentModel.DataAnnotations;
using Hospital.Domain.Enums;

namespace Hospital.Application.DTOs.Schedules;

public class TimeSlotDto
{
    public Guid Id { get; set; }
    public Guid WorkingScheduleId { get; set; }
    public Guid DoctorId { get; set; }
    public string DoctorName { get; set; } = string.Empty;
    public DateOnly SlotDate { get; set; }
    public TimeOnly StartTime { get; set; }
    public TimeOnly EndTime { get; set; }
    public TimeSlotStatus Status { get; set; }
    public bool IsActive { get; set; }
    public string? ClosedReason { get; set; }
    public bool IsAvailable => Status == TimeSlotStatus.Open && IsActive;
}

public class WorkingScheduleDto
{
    public Guid Id { get; set; }
    public Guid DoctorId { get; set; }
    public string DoctorName { get; set; } = string.Empty;
    public string DoctorCode { get; set; } = string.Empty;
    public DateOnly WorkDate { get; set; }
    public TimeOnly StartTime { get; set; }
    public TimeOnly EndTime { get; set; }
    [StringLength(500)] public string? Note { get; set; }
    public int SlotsCount { get; set; }
    public int OpenSlotsCount { get; set; }
    public int OccupiedSlotsCount { get; set; }
    public List<TimeSlotDto> TimeSlots { get; set; } = new();
}

public class CreateWorkingScheduleRequestDto
{
    [Required(ErrorMessage = "Bác sĩ không được để trống")]
    public Guid DoctorId { get; set; }

    [Required(ErrorMessage = "Ngày làm việc không được để trống")]
    public DateOnly WorkDate { get; set; }

    [Required(ErrorMessage = "Giờ bắt đầu không được để trống")]
    public TimeOnly StartTime { get; set; }

    [Required(ErrorMessage = "Giờ kết thúc không được để trống")]
    public TimeOnly EndTime { get; set; }

    [StringLength(500)] public string? Note { get; set; }
}

public class UpdateWorkingScheduleRequestDto
{
    [Required(ErrorMessage = "Ngày làm việc không được để trống")]
    public DateOnly WorkDate { get; set; }

    [Required(ErrorMessage = "Giờ bắt đầu không được để trống")]
    public TimeOnly StartTime { get; set; }

    [Required(ErrorMessage = "Giờ kết thúc không được để trống")]
    public TimeOnly EndTime { get; set; }

    [StringLength(500)] public string? Note { get; set; }
}

public class CloseTimeSlotRequestDto
{
    [Required(ErrorMessage = "Lý do đóng khung giờ không được để trống")]
    [StringLength(500, ErrorMessage = "Lý do tối đa 500 ký tự")]
    public string Reason { get; set; } = string.Empty;
}
