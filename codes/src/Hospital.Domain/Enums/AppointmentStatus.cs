namespace Hospital.Domain.Enums;

public enum AppointmentStatus : byte
{
    Confirmed = 0,
    Cancelled = 1,
    CheckedIn = 2,
    NoShow = 3
}
