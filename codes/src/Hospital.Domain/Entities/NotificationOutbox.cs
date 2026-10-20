using Hospital.Domain.Enums;

namespace Hospital.Domain.Entities;

public class NotificationOutbox
{
    public Guid Id { get; set; }
    public Guid PatientId { get; set; }
    public Guid AppointmentId { get; set; }
    public NotificationType Type { get; set; }
    public string Title { get; set; } = "";
    public string Message { get; set; } = "";
    public DateTime ScheduledFor { get; set; }
    public DateTime? DeliveredAt { get; set; }
    public DateTime? SuppressedAt { get; set; }
}
