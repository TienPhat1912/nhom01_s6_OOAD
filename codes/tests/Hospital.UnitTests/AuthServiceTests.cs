using Hospital.Application.Common.Exceptions;
using Hospital.Application.DTOs.Auth;
using Hospital.Domain.Enums;
using Hospital.Infrastructure.Data;
using Hospital.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace Hospital.UnitTests;

public class AuthServiceTests
{
    private (HospitalDbContext Context, AuthService Service) CreateSut()
    {
        var options = new DbContextOptionsBuilder<HospitalDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        var context = new HospitalDbContext(options);
        var hasher = new PasswordHasher();

        var inMemorySettings = new Dictionary<string, string?>
        {
            {"JwtSettings:Secret", "SuperSecretTestingKey_MustBeLongEnoughForHmacSha256!"},
            {"JwtSettings:Issuer", "TestIssuer"},
            {"JwtSettings:Audience", "TestAudience"},
            {"JwtSettings:ExpiryMinutes", "60"}
        };
        IConfiguration config = new ConfigurationBuilder().AddInMemoryCollection(inMemorySettings).Build();
        var jwtGen = new JwtTokenGenerator(config);

        // Seed roles
        context.Roles.AddRange(
            new Domain.Entities.Role { Id = (int)UserRole.Patient, Name = "Patient" },
            new Domain.Entities.Role { Id = (int)UserRole.Doctor, Name = "Doctor" },
            new Domain.Entities.Role { Id = (int)UserRole.Staff, Name = "Staff" },
            new Domain.Entities.Role { Id = (int)UserRole.Admin, Name = "Admin" }
        );
        context.SaveChanges();

        var service = new AuthService(context, hasher, jwtGen);
        return (context, service);
    }

    [Fact]
    public async Task RegisterAsync_ShouldCreatePatientAndReturnToken()
    {
        var (context, service) = CreateSut();
        var req = new RegisterRequestDto
        {
            Username = "newpatient",
            Password = "Password@123",
            FullName = "Nguyễn Văn Mới",
            Email = "newpatient@test.com",
            PhoneNumber = "0900000000",
            DateOfBirth = new DateOnly(1995, 1, 1),
            Gender = Gender.Male
        };

        var res = await service.RegisterAsync(req);

        Assert.NotNull(res);
        Assert.NotEmpty(res.Token);
        Assert.Equal("newpatient", res.Username);
        Assert.NotNull(res.PatientId);

        var userInDb = await context.Users.Include(u => u.Patient).FirstOrDefaultAsync(u => u.Username == "newpatient");
        Assert.NotNull(userInDb);
        Assert.NotNull(userInDb.Patient);
        Assert.StartsWith("BN-", userInDb.Patient.PatientCode);
    }

    [Fact]
    public async Task RegisterAsync_DuplicateUsername_ShouldThrowBusinessRuleException()
    {
        var (_, service) = CreateSut();
        var req = new RegisterRequestDto
        {
            Username = "dupuser",
            Password = "Password@123",
            FullName = "User 1",
            Email = "user1@test.com",
            DateOfBirth = new DateOnly(1990, 1, 1)
        };

        await service.RegisterAsync(req);

        // Attempt duplicate username
        var req2 = new RegisterRequestDto
        {
            Username = "dupuser",
            Password = "Password@456",
            FullName = "User 2",
            Email = "user2@test.com",
            DateOfBirth = new DateOnly(1992, 1, 1)
        };

        var ex = await Assert.ThrowsAsync<BusinessRuleException>(() => service.RegisterAsync(req2));
        Assert.Equal("USERNAME_ALREADY_EXISTS", ex.ErrorCode);
    }

    [Fact]
    public async Task LoginAsync_ValidCredentials_ShouldReturnAuthResponse()
    {
        var (_, service) = CreateSut();
        await service.RegisterAsync(new RegisterRequestDto
        {
            Username = "loginuser",
            Password = "Password@123",
            FullName = "Login User",
            Email = "loginuser@test.com",
            DateOfBirth = new DateOnly(1990, 1, 1)
        });

        var res = await service.LoginAsync(new LoginRequestDto
        {
            Username = "loginuser",
            Password = "Password@123"
        });

        Assert.NotNull(res);
        Assert.NotEmpty(res.Token);
        Assert.Equal("Patient", res.Role);
    }

    [Fact]
    public async Task LoginAsync_InvalidPassword_ShouldThrowException()
    {
        var (_, service) = CreateSut();
        await service.RegisterAsync(new RegisterRequestDto
        {
            Username = "testpass",
            Password = "Password@123",
            FullName = "Test Pass",
            Email = "testpass@test.com",
            DateOfBirth = new DateOnly(1990, 1, 1)
        });

        var ex = await Assert.ThrowsAsync<BusinessRuleException>(() => service.LoginAsync(new LoginRequestDto
        {
            Username = "testpass",
            Password = "WrongPassword"
        }));

        Assert.Equal("INVALID_CREDENTIALS", ex.ErrorCode);
    }

    [Fact]
    public async Task ChangePasswordAsync_ShouldSucceedWhenOldPasswordCorrect()
    {
        var (context, service) = CreateSut();
        var authRes = await service.RegisterAsync(new RegisterRequestDto
        {
            Username = "chgpass",
            Password = "OldPassword@123",
            FullName = "Change Pass",
            Email = "chgpass@test.com",
            DateOfBirth = new DateOnly(1990, 1, 1)
        });

        await service.ChangePasswordAsync(authRes.UserId, new ChangePasswordRequestDto
        {
            OldPassword = "OldPassword@123",
            NewPassword = "NewPassword@123"
        });

        // Now login with new password should succeed
        var loginRes = await service.LoginAsync(new LoginRequestDto
        {
            Username = "chgpass",
            Password = "NewPassword@123"
        });

        Assert.NotNull(loginRes);

        // Login with old password should fail
        await Assert.ThrowsAsync<BusinessRuleException>(() => service.LoginAsync(new LoginRequestDto
        {
            Username = "chgpass",
            Password = "OldPassword@123"
        }));
    }
}
