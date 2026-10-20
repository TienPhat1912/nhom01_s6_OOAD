using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Hospital.Domain.Entities;
using Hospital.Domain.Enums;
using Hospital.Infrastructure.Data;
using Hospital.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace Hospital.UnitTests;

public class FoundationTests
{
    [Fact]
    public void PasswordHasher_ShouldHashAndVerifySuccessfully()
    {
        // Arrange
        var hasher = new PasswordHasher();
        var rawPassword = "SecurePassword@123";

        // Act
        var hash = hasher.HashPassword(rawPassword);
        var isValid = hasher.VerifyPassword(rawPassword, hash);
        var isInvalid = hasher.VerifyPassword("WrongPassword", hash);

        // Assert
        Assert.NotEmpty(hash);
        Assert.NotEqual(rawPassword, hash);
        Assert.True(isValid);
        Assert.False(isInvalid);
    }

    [Fact]
    public void JwtTokenGenerator_ShouldGenerateTokenWithClaims()
    {
        // Arrange
        var inMemorySettings = new Dictionary<string, string?>
        {
            {"JwtSettings:Secret", "SuperSecretTestingKey_MustBeLongEnoughForHmacSha256!"},
            {"JwtSettings:Issuer", "TestIssuer"},
            {"JwtSettings:Audience", "TestAudience"},
            {"JwtSettings:ExpiryMinutes", "60"}
        };
        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(inMemorySettings)
            .Build();

        var generator = new JwtTokenGenerator(configuration);
        var user = new User
        {
            Id = Guid.NewGuid(),
            Username = "doctor_john",
            FullName = "Dr. John Doe",
            Email = "john@hospital.com",
            Role = new Role { Id = (int)UserRole.Doctor, Name = "Doctor" }
        };
        var doctorId = Guid.NewGuid();

        // Act
        var token = generator.GenerateToken(user, doctorId: doctorId);

        // Assert
        Assert.NotNull(token);
        var handler = new JwtSecurityTokenHandler();
        var jwt = handler.ReadJwtToken(token);

        Assert.Equal("TestIssuer", jwt.Issuer);
        Assert.Contains(jwt.Claims, c => c.Type == ClaimTypes.NameIdentifier && c.Value == user.Id.ToString());
        Assert.Contains(jwt.Claims, c => c.Type == ClaimTypes.Role && c.Value == "Doctor");
        Assert.Contains(jwt.Claims, c => c.Type == "doctorId" && c.Value == doctorId.ToString());
    }

    [Fact]
    public async Task DbSeeder_ShouldSeedDataSuccessfully()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<HospitalDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        using var context = new HospitalDbContext(options);
        var hasher = new PasswordHasher();

        // Act
        await DbSeeder.SeedAsync(context, hasher);

        // Assert
        var rolesCount = await context.Roles.CountAsync();
        var usersCount = await context.Users.CountAsync();
        var patientsCount = await context.Patients.CountAsync();
        var doctorsCount = await context.Doctors.CountAsync();
        var specialtiesCount = await context.Specialties.CountAsync();
        var schedulesCount = await context.WorkingSchedules.CountAsync();
        var slotsCount = await context.TimeSlots.CountAsync();
        var appointmentsCount = await context.Appointments.CountAsync();

        Assert.Equal(4, rolesCount);
        Assert.Equal(6, usersCount);
        Assert.Equal(2, patientsCount);
        Assert.Equal(2, doctorsCount);
        Assert.Equal(5, specialtiesCount);
        Assert.True(schedulesCount > 0);
        Assert.True(slotsCount > 0);
        Assert.True(appointmentsCount > 0);

        // Verify admin exists and password can be verified
        var admin = await context.Users.FirstOrDefaultAsync(u => u.Username == "admin");
        Assert.NotNull(admin);
        Assert.True(hasher.VerifyPassword("Admin@123", admin.PasswordHash));
    }
}
