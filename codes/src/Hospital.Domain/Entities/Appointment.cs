using Hospital.Domain.Enums;

namespace Hospital.Domain.Entities;

public class Appointment
{
    public Guid Id { get; set; }
    public string AppointmentCode { get; set; } = string.Empty;
    public Guid? RequestKey { get; set; }
    public string? RequestFingerprint { get; set; }
    public Guid PatientId { get; set; }
    public Patient Patient { get; set; } = null!;
    public Guid DoctorId { get; set; }
    public Doctor Doctor { get; set; } = null!;
    public Guid TimeSlotId { get; set; }
    public TimeSlot TimeSlot { get; set; } = null!;
    public int SpecialtyId { get; set; }
    public Specialty Specialty { get; set; } = null!;
    public DateOnly AppointmentDate { get; set; }
    public TimeOnly StartTime { get; set; }
    public TimeOnly EndTime { get; set; }
    public AppointmentStatus Status { get; set; } = AppointmentStatus.Confirmed;
    public string? Notes { get; set; }
    public string? CancellationReason { get; set; }
    public string? RescheduleReason { get; set; }
    public Guid BookedBy { get; set; }
    public User BookedByUser { get; set; } = null!;
    public Guid? CheckedInBy { get; set; }
    public User? CheckedInByUser { get; set; }
    public DateTime? CheckedInAt { get; set; }
    public Guid? MarkedNoShowBy { get; set; }
    public User? MarkedNoShowByUser { get; set; }
    public DateTime? MarkedNoShowAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public byte[] RowVersion { get; set; } = [];

    public ICollection<AppointmentStatusHistory> StatusHistories { get; set; } = new List<AppointmentStatusHistory>();
    public ICollection<Notification> Notifications { get; set; } = new List<Notification>();
}
