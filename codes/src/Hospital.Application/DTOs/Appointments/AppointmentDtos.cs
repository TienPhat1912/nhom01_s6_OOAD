using System.ComponentModel.DataAnnotations;
using Hospital.Application.Common.Models;
using Hospital.Domain.Enums;

namespace Hospital.Application.DTOs.Appointments;

public class BookAppointmentRequest
{
    public Guid? PatientId { get; set; }
    public Guid TimeSlotId { get; set; }
    public int SpecialtyId { get; set; }
    public Guid RequestKey { get; set; }
    [StringLength(1000)] public string? Notes { get; set; }
}

public class AppointmentMutationRequest
{
    [Required, MinLength(1)] public byte[] RowVersion { get; set; } = [];
    [StringLength(500)] public string? Reason { get; set; }
}

public class RescheduleAppointmentRequest : AppointmentMutationRequest
{
    public Guid TimeSlotId { get; set; }
    public int SpecialtyId { get; set; }
}

public class AppointmentQuery : PageQuery
{
    [StringLength(100)] public string? Keyword { get; set; }
    public DateOnly? FromDate { get; set; }
    public DateOnly? ToDate { get; set; }
    public AppointmentStatus? Status { get; set; }
    public Guid? DoctorId { get; set; }
    public Guid? PatientId { get; set; }
    public bool Descending { get; set; }
}

public record AppointmentHistoryDto(AppointmentStatus? OldStatus, AppointmentStatus NewStatus,
    string? Reason, Guid ChangedBy, DateTime ChangedAt, Guid? OldTimeSlotId, Guid? NewTimeSlotId);

public class AppointmentDto
{
    public Guid Id { get; set; }
    public string AppointmentCode { get; set; } = "";
    public Guid PatientId { get; set; }
    public string PatientName { get; set; } = "";
    public string? PatientPhone { get; set; }
    public Guid DoctorId { get; set; }
    public string DoctorName { get; set; } = "";
    public Guid TimeSlotId { get; set; }
    public int SpecialtyId { get; set; }
    public string SpecialtyName { get; set; } = "";
    public DateOnly AppointmentDate { get; set; }
    public TimeOnly StartTime { get; set; }
    public TimeOnly EndTime { get; set; }
    public AppointmentStatus Status { get; set; }
    public string? Notes { get; set; }
    public Guid BookedBy { get; set; }
    public DateTime? CheckedInAt { get; set; }
    public Guid? CheckedInBy { get; set; }
    public DateTime? MarkedNoShowAt { get; set; }
    public Guid? MarkedNoShowBy { get; set; }
    public byte[] RowVersion { get; set; } = [];
    public List<AppointmentHistoryDto> History { get; set; } = [];
}
