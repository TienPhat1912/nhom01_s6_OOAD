namespace Hospital.Application.Common.Interfaces;

public interface ICurrentUserService
{
    Guid? UserId { get; }
    string? Username { get; }
    string? Role { get; }
    Guid? PatientId { get; }
    Guid? DoctorId { get; }
    bool IsAuthenticated { get; }
}
