using Hospital.Application.DTOs.Appointments;
using Hospital.Domain.Enums;

namespace Hospital.Web.Services;

// UI hints only. The API revalidates every mutation against its own clock and current data.
public sealed class AppointmentActions(TimeProvider clock)
{
    public DateTime LocalNow => clock.GetUtcNow().UtcDateTime.AddHours(7);
    public bool CanChange(AppointmentDto appointment, bool staff) => appointment.Status == AppointmentStatus.Confirmed &&
        (staff ? appointment.AppointmentDate.ToDateTime(appointment.StartTime) > LocalNow
        : appointment.AppointmentDate.ToDateTime(appointment.StartTime) - LocalNow >= TimeSpan.FromHours(2));
    public bool CanCheckIn(AppointmentDto appointment) => appointment.Status == AppointmentStatus.Confirmed &&
        appointment.AppointmentDate == DateOnly.FromDateTime(LocalNow) && appointment.AppointmentDate.ToDateTime(appointment.EndTime) > LocalNow;
    public bool CanMarkNoShow(AppointmentDto appointment) => appointment.Status == AppointmentStatus.Confirmed &&
        appointment.AppointmentDate.ToDateTime(appointment.EndTime) < LocalNow;
}
