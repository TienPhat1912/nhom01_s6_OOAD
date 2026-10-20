using Hospital.Domain.Enums;

namespace Hospital.Domain.Entities;

public class AppointmentStatusHistory
{
    public long Id { get; set; }
    public Guid AppointmentId { get; set; }
    public Appointment Appointment { get; set; } = null!;
    public AppointmentStatus? OldStatus { get; set; }
    public AppointmentStatus NewStatus { get; set; }
    public string? Reason { get; set; }
    public Guid ChangedBy { get; set; }
    public User ChangedByUser { get; set; } = null!;
    public DateTime ChangedAt { get; set; } = DateTime.UtcNow;
    public Guid? OldTimeSlotId { get; set; }
    public Guid? NewTimeSlotId { get; set; }
}
