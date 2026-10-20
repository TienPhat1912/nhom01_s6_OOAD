namespace Hospital.Domain.Entities;

public class WorkingSchedule
{
    public Guid Id { get; set; }
    public Guid DoctorId { get; set; }
    public Doctor Doctor { get; set; } = null!;
    public DateOnly WorkDate { get; set; }
    public TimeOnly StartTime { get; set; }
    public TimeOnly EndTime { get; set; }
    public string? Note { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public Guid? CreatedBy { get; set; }
    public User? CreatedByUser { get; set; }

    public ICollection<TimeSlot> TimeSlots { get; set; } = new List<TimeSlot>();
}
