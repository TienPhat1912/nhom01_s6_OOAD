using System.Collections.Concurrent;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Hospital.Application.Common.Interfaces;
using Hospital.Application.Common.Models;
using Hospital.Application.DTOs.Auth;
using Hospital.Domain.Entities;
using Hospital.Infrastructure.Data;
using Hospital.Infrastructure.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;

namespace Hospital.IntegrationTests;

public sealed class TestClock : TimeProvider
{
    public DateTimeOffset Now { get; set; } = new(2026, 10, 16, 0, 0, 0, TimeSpan.Zero);
    public override DateTimeOffset GetUtcNow() => Now;
}

public sealed class TestActor : ICurrentUserService
{
    public Guid? UserId { get; set; }
    public string? Username { get; set; }
    public string? Role { get; set; }
    public Guid? PatientId { get; set; }
    public Guid? DoctorId { get; set; }
    public bool IsAuthenticated => UserId.HasValue;
}

public sealed class CachedTestHasher : IPasswordHasher
{
    private static readonly ConcurrentDictionary<string, Lazy<string>> Hashes = new();
    private readonly PasswordHasher inner = new();
    public string HashPassword(string password) => Hashes.GetOrAdd(password, p => new Lazy<string>(() => inner.HashPassword(p))).Value;
    public bool VerifyPassword(string password, string hash) => inner.VerifyPassword(password, hash);
}

// SQLite does not generate SQL Server rowversions. The test adapter generates tokens on every write.
internal sealed class SqliteTestContext(DbContextOptions<HospitalDbContext> options) : HospitalDbContext(options)
{
    protected override void OnModelCreating(ModelBuilder model)
    {
        base.OnModelCreating(model);
        model.Entity<User>().Property(u => u.Id).HasDefaultValueSql(null);
        model.Entity<User>().Property(u => u.CreatedAt).HasDefaultValueSql(null);
        model.Entity<Patient>().Property(p => p.Id).HasDefaultValueSql(null);
        model.Entity<Doctor>().Property(d => d.Id).HasDefaultValueSql(null);
        model.Entity<Appointment>().Property(a => a.RowVersion).ValueGeneratedNever().IsConcurrencyToken();
        model.Entity<TimeSlot>().Property(a => a.RowVersion).ValueGeneratedNever().IsConcurrencyToken();
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        foreach (var entry in ChangeTracker.Entries().Where(e => e.State is EntityState.Added or EntityState.Modified))
        {
            if (entry.Entity is Appointment a) a.RowVersion = Guid.NewGuid().ToByteArray();
            if (entry.Entity is TimeSlot s) s.RowVersion = Guid.NewGuid().ToByteArray();
        }
        return base.SaveChangesAsync(cancellationToken);
    }
}

public sealed class TestFixture : IDisposable
{
    public static System.Text.Json.JsonSerializerOptions Json { get; } = new(System.Text.Json.JsonSerializerDefaults.Web)
    { Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() } };
    public const string JwtKey = "Phase3Tests_Only_TestSigningSecretAtLeast32Bytes!";
    private readonly string file;
    private WebApplicationFactory<Program>? factory;
    private readonly string? sqlConnection;
    public TestClock Provider { get; } = new();
    public ClinicTime Clock => new(Provider);
    public CachedTestHasher Hasher { get; } = new();
    public bool DatabaseUnavailable { get; set; }

    public TestFixture(string? sqlServer = null)
    {
        file = Path.Combine(Path.GetTempPath(), "HospitalPhase3-" + Guid.NewGuid().ToString("N") + ".db");
        if (sqlServer != null)
        {
            var cs = new Microsoft.Data.SqlClient.SqlConnectionStringBuilder(sqlServer)
            { InitialCatalog = "HospitalPhase3Tests_" + Guid.NewGuid().ToString("N") };
            sqlConnection = cs.ConnectionString;
        }
        using var db = CreateDb();
        if (sqlConnection == null) db.Database.EnsureCreated(); else db.Database.Migrate();
        DbSeeder.SeedAsync(db, Hasher, Clock).GetAwaiter().GetResult();
    }

    public HospitalDbContext CreateDb(params IInterceptor[] interceptors) => sqlConnection == null
        ? new SqliteTestContext(new DbContextOptionsBuilder<HospitalDbContext>()
            .UseSqlite(DatabaseUnavailable ? $"Data Source={file}.missing;Mode=ReadOnly;Pooling=False"
                : $"Data Source={file};Default Timeout=10;Pooling=False").AddInterceptors(interceptors).Options)
        : CreateSqlServerDb(interceptors);

    private HospitalDbContext CreateSqlServerDb(IInterceptor[] interceptors)
    {
        var connection = new Microsoft.Data.SqlClient.SqlConnectionStringBuilder(sqlConnection!);
        if (DatabaseUnavailable)
        {
            // Fail a real SQL connection without stopping the shared SQL Server service.
            connection.InitialCatalog += "_Unavailable";
            connection.Pooling = false;
            connection.ConnectTimeout = 1;
            connection.ConnectRetryCount = 0;
        }
        return new HospitalDbContext(new DbContextOptionsBuilder<HospitalDbContext>()
            .UseSqlServer(connection.ConnectionString, sql =>
            {
                if (DatabaseUnavailable) sql.EnableRetryOnFailure(1, TimeSpan.Zero, null);
                else sql.EnableRetryOnFailure();
            }).AddInterceptors(interceptors).Options);
    }

    public async Task<TimeSlot> ExtraSlotAsync(string doctor = "doctor2", int day = 1, int hour = 9, int minute = 0)
    {
        using var db = CreateDb();
        var s = new WorkingSchedule { Id = Guid.NewGuid(), DoctorId = Actor(doctor).DoctorId!.Value,
            WorkDate = Clock.Today.AddDays(day), StartTime = new(hour, minute), EndTime = new TimeOnly(hour, minute).AddMinutes(30),
            CreatedBy = Actor("admin").UserId };
        db.WorkingSchedules.Add(s);
        var slot = new TimeSlot { Id = Guid.NewGuid(), WorkingScheduleId = s.Id, DoctorId = s.DoctorId,
            SlotDate = s.WorkDate, StartTime = s.StartTime, EndTime = s.EndTime };
        db.TimeSlots.Add(slot); await db.SaveChangesAsync(); return slot;
    }

    public TestActor Actor(string username)
    {
        using var db = CreateDb();
        var user = db.Users.Include(u => u.Role).Include(u => u.Patient).Include(u => u.Doctor).Single(u => u.Username == username);
        return new() { UserId = user.Id, Username = username, Role = user.Role.Name, PatientId = user.Patient?.Id, DoctorId = user.Doctor?.Id };
    }

    public AppointmentService Appointments(HospitalDbContext db, TestActor actor) =>
        new(db, actor, Clock, new NotificationDispatcher(db, Clock), NullLogger<AppointmentService>.Instance);

    public AdminService Admin(HospitalDbContext db, TestActor? actor = null)
    {
        actor ??= Actor("admin");
        return new(db, actor, Hasher, new CatalogService(db, actor, Clock), Clock);
    }

    public TimeSlot Slot(string doctor = "doctor1", int day = 1, int hour = 9, int minute = 0)
    {
        using var db = CreateDb();
        var doctorId = Actor(doctor).DoctorId;
        var date = Clock.Today.AddDays(day);
        return db.TimeSlots.AsNoTracking().Single(s => s.DoctorId == doctorId && s.SlotDate == date && s.StartTime == new TimeOnly(hour, minute));
    }

    public int Specialty(string code = "NOI") { using var db = CreateDb(); return db.Specialties.Single(s => s.Code == code).Id; }

    public async Task<HttpClient> ClientAsync(string? username = null)
    {
        factory ??= new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Testing");
            builder.UseSetting("JwtSettings:Secret", JwtKey);
            builder.UseSetting("Database:Initialize", "false");
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<HospitalDbContext>();
                services.AddScoped(_ => CreateDb());
                services.RemoveAll<TimeProvider>(); services.AddSingleton<TimeProvider>(Provider);
                foreach (var descriptor in services.Where(d => d.ServiceType == typeof(IHostedService)
                    && d.ImplementationType == typeof(Hospital.Api.Services.NotificationWorker)).ToList()) services.Remove(descriptor);
            });
        });
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions { BaseAddress = new Uri("https://localhost") });
        if (username != null)
        {
            var password = username.StartsWith("patient") ? "Patient@123" : username.StartsWith("doctor") ? "Doctor@123" : username == "admin" ? "Admin@123" : "Staff@123";
            var response = await client.PostAsJsonAsync("/api/auth/login", new { username, password });
            response.EnsureSuccessStatusCode();
            var auth = (await response.Content.ReadFromJsonAsync<ApiResponse<AuthResponseDto>>())!.Data!;
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth.Token);
        }
        return client;
    }

    public void Dispose()
    {
        factory?.Dispose();
        if (sqlConnection != null)
        {
            var name = new Microsoft.Data.SqlClient.SqlConnectionStringBuilder(sqlConnection).InitialCatalog;
            if (!name.StartsWith("HospitalPhase3Tests_", StringComparison.Ordinal)) throw new InvalidOperationException("Unsafe test database name.");
            using var db = CreateDb(); db.Database.EnsureDeleted();
        }
        else foreach (var suffix in new[] { "", "-wal", "-shm", "-journal" }) File.Delete(file + suffix);
    }
}
