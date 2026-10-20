using Hospital.Application.Common.Models;
using Hospital.Domain.Enums;

namespace Hospital.Application.Services.Interfaces;

public record NotificationDto(Guid Id, Guid? AppointmentId, NotificationType Type, string Title,
    string Message, bool IsRead, DateTime CreatedAt);

public interface INotificationService
{
    Task<PagedResult<NotificationDto>> ListAsync(PageQuery page, bool unreadOnly);
    Task ReadAsync(Guid id);
}
