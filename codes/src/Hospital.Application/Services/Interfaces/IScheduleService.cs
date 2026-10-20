using Hospital.Application.DTOs.Schedules;

namespace Hospital.Application.Services.Interfaces;

public interface IScheduleService
{
    Task<List<TimeSlotDto>> GetAvailableTimeSlotsAsync(Guid doctorId, DateOnly date);
    Task<List<WorkingScheduleDto>> GetWorkingSchedulesAsync(Guid? doctorId = null, DateOnly? fromDate = null, DateOnly? toDate = null);
    Task<WorkingScheduleDto> GetWorkingScheduleByIdAsync(Guid id);
    Task<WorkingScheduleDto> CreateWorkingScheduleAsync(Guid adminUserId, CreateWorkingScheduleRequestDto request);
    Task<WorkingScheduleDto> UpdateWorkingScheduleAsync(Guid adminUserId, Guid id, UpdateWorkingScheduleRequestDto request);
    Task CloseTimeSlotAsync(Guid adminUserId, Guid slotId, CloseTimeSlotRequestDto request);
}
