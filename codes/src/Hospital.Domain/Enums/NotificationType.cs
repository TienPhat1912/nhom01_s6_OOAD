namespace Hospital.Domain.Enums;

public enum NotificationType : byte
{
    Confirmed = 0,
    Rescheduled = 1,
    Cancelled = 2,
    Reminder = 3
}
