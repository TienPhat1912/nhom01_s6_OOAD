using Hospital.Application.Common.Interfaces;
using Hospital.Infrastructure.Data;
using Hospital.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Hospital.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection") 
            ?? "Server=.\\SQLEXPRESS;Database=HospitalDb;Trusted_Connection=True;TrustServerCertificate=True;";

        services.AddDbContext<HospitalDbContext>(options =>
            options.UseSqlServer(connectionString, sqlOptions =>
            {
                sqlOptions.MigrationsAssembly(typeof(HospitalDbContext).Assembly.FullName);
                sqlOptions.EnableRetryOnFailure(maxRetryCount: 3, maxRetryDelay: TimeSpan.FromSeconds(5), errorNumbersToAdd: null);
            }));

        services.AddSingleton<IPasswordHasher, PasswordHasher>();
        services.AddSingleton<IJwtTokenGenerator, JwtTokenGenerator>();
        services.AddSingleton(TimeProvider.System);
        services.AddSingleton<ClinicTime>();
        services.AddScoped<NotificationDispatcher>();
        services.AddScoped<Hospital.Application.Services.Interfaces.IAppointmentService, AppointmentService>();
        services.AddScoped<Hospital.Application.Services.Interfaces.IAdminService, AdminService>();
        services.AddScoped<Hospital.Application.Services.Interfaces.INotificationService, NotificationService>();
        services.AddScoped<Hospital.Application.Services.Interfaces.IAuthService, AuthService>();
        services.AddScoped<Hospital.Application.Services.Interfaces.ICatalogService, CatalogService>();
        services.AddScoped<Hospital.Application.Services.Interfaces.IScheduleService, ScheduleService>();

        return services;
    }
}
