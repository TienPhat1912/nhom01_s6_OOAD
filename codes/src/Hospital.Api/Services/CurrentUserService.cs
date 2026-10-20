using System.Security.Claims;
using Hospital.Application.Common.Interfaces;

namespace Hospital.Api.Services;

public class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    private ClaimsPrincipal? User => _httpContextAccessor.HttpContext?.User;

    public Guid? UserId
    {
        get
        {
            var idClaim = User?.FindFirstValue(ClaimTypes.NameIdentifier);
            return Guid.TryParse(idClaim, out var id) ? id : null;
        }
    }

    public string? Username => User?.FindFirstValue("username") ?? User?.Identity?.Name;

    public string? Role => User?.FindFirstValue(ClaimTypes.Role);

    public Guid? PatientId
    {
        get
        {
            var pClaim = User?.FindFirstValue("patientId");
            return Guid.TryParse(pClaim, out var id) ? id : null;
        }
    }

    public Guid? DoctorId
    {
        get
        {
            var dClaim = User?.FindFirstValue("doctorId");
            return Guid.TryParse(dClaim, out var id) ? id : null;
        }
    }

    public bool IsAuthenticated => User?.Identity?.IsAuthenticated ?? false;
}
