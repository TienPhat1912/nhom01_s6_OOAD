using Hospital.Domain.Entities;

namespace Hospital.Application.Common.Interfaces;

public interface IJwtTokenGenerator
{
    string GenerateToken(User user, Guid? patientId = null, Guid? doctorId = null);
}
