using Hospital.Application.Common.Models;
using Hospital.Application.DTOs.Appointments;

namespace Hospital.Application.Services.Interfaces;

public interface IAppointmentService
{
    Task<AppointmentDto> BookAsync(BookAppointmentRequest request);
    Task<PagedResult<AppointmentDto>> ListAsync(AppointmentQuery query);
    Task<AppointmentDto> GetAsync(Guid id);
    Task<AppointmentDto> RescheduleAsync(Guid id, RescheduleAppointmentRequest request);
    Task<AppointmentDto> CancelAsync(Guid id, AppointmentMutationRequest request);
    Task<AppointmentDto> CheckInAsync(Guid id, AppointmentMutationRequest request);
    Task<AppointmentDto> NoShowAsync(Guid id, AppointmentMutationRequest request);
}
