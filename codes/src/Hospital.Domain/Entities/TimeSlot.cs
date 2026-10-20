using Hospital.Domain.Enums;

namespace Hospital.Domain.Entities;

public class TimeSlot
{
    public Guid Id { get; set; }
    public Guid WorkingScheduleId { get; set; }
    public WorkingSchedule WorkingSchedule { get; set; } = null!;
    public Guid DoctorId { get; set; }
    public Doctor Doctor { get; set; } = null!;
    public DateOnly SlotDate { get; set; }
    public TimeOnly StartTime { get; set; }
    public TimeOnly EndTime { get; set; }
    public TimeSlotStatus Status { get; set; } = TimeSlotStatus.Open;
    public bool IsActive { get; set; } = true;
    public string? ClosedReason { get; set; }
    public DateTime? ClosedAt { get; set; }
    public Guid? ClosedBy { get; set; }
    public User? ClosedByUser { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public byte[] RowVersion { get; set; } = [];

    public ICollection<Appointment> Appointments { get; set; } = new List<Appointment>();
}
