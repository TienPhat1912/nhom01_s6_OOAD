using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Hospital.Application.Common.Interfaces;
using Hospital.Domain.Entities;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace Hospital.Infrastructure.Services;

public class JwtTokenGenerator : IJwtTokenGenerator
{
    private readonly IConfiguration _configuration;

    public JwtTokenGenerator(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public string GenerateToken(User user, Guid? patientId = null, Guid? doctorId = null)
    {
        var secret = _configuration["JwtSettings:Secret"] ?? throw new InvalidOperationException("Chưa cấu hình khóa ký JWT.");
        var issuer = _configuration["JwtSettings:Issuer"] ?? "HospitalAppointmentApi";
        var audience = _configuration["JwtSettings:Audience"] ?? "HospitalAppointmentClients";
        var expiryMinutesStr = _configuration["JwtSettings:ExpiryMinutes"] ?? "1440";
        var expiryMinutes = double.TryParse(expiryMinutesStr, out var m) ? m : 1440;

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new("tokenVersion", user.TokenVersion.ToString()),
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.FullName),
            new("username", user.Username),
            new(ClaimTypes.Email, user.Email),
            new(ClaimTypes.Role, user.Role?.Name ?? "Patient")
        };

        if (patientId.HasValue)
        {
            claims.Add(new Claim("patientId", patientId.Value.ToString()));
        }

        if (doctorId.HasValue)
        {
            claims.Add(new Claim("doctorId", doctorId.Value.ToString()));
        }

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(expiryMinutes),
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
