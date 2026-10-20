using Hospital.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Hospital.Infrastructure.Data;

public class HospitalDbContext : DbContext
{
    public HospitalDbContext(DbContextOptions<HospitalDbContext> options) : base(options)
    {
    }

    public DbSet<Role> Roles => Set<Role>();
    public DbSet<User> Users => Set<User>();
    public DbSet<Patient> Patients => Set<Patient>();
    public DbSet<Specialty> Specialties => Set<Specialty>();
    public DbSet<Doctor> Doctors => Set<Doctor>();
    public DbSet<DoctorSpecialty> DoctorSpecialties => Set<DoctorSpecialty>();
    public DbSet<WorkingSchedule> WorkingSchedules => Set<WorkingSchedule>();
    public DbSet<TimeSlot> TimeSlots => Set<TimeSlot>();
    public DbSet<Appointment> Appointments => Set<Appointment>();
    public DbSet<AppointmentStatusHistory> AppointmentStatusHistories => Set<AppointmentStatusHistory>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<NotificationOutbox> NotificationOutbox => Set<NotificationOutbox>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // 1. Roles
        modelBuilder.Entity<Role>(entity =>
        {
            entity.ToTable("Roles");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.Name).HasMaxLength(50).IsRequired();
            entity.Property(e => e.Description).HasMaxLength(200);
            entity.HasIndex(e => e.Name).IsUnique();
        });

        // 2. Users
        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("Users");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasDefaultValueSql("NEWSEQUENTIALID()");
            entity.Property(e => e.Username).HasMaxLength(50).IsRequired();
            entity.Property(e => e.PasswordHash).HasMaxLength(256).IsRequired();
            entity.Property(e => e.Email).HasMaxLength(100).IsRequired();
            entity.Property(e => e.PhoneNumber).HasMaxLength(20);
            entity.Property(e => e.FullName).HasMaxLength(100).IsRequired();
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETUTCDATE()");

            entity.HasIndex(e => e.Username).IsUnique();
            entity.HasIndex(e => e.Email).IsUnique();

            entity.HasOne(e => e.Role)
                .WithMany(r => r.Users)
                .HasForeignKey(e => e.RoleId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // 3. Patients
        modelBuilder.Entity<Patient>(entity =>
        {
            entity.ToTable("Patients");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasDefaultValueSql("NEWSEQUENTIALID()");
            entity.Property(e => e.PatientCode).HasMaxLength(20).IsRequired();
            entity.Property(e => e.Address).HasMaxLength(300);

            entity.HasIndex(e => e.PatientCode).IsUnique();
            entity.HasIndex(e => e.UserId).IsUnique();

            entity.HasOne(e => e.User)
                .WithOne(u => u.Patient)
                .HasForeignKey<Patient>(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // 4. Specialties
        modelBuilder.Entity<Specialty>(entity =>
        {
            entity.ToTable("Specialties");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Code).HasMaxLength(20).IsRequired();
            entity.Property(e => e.Name).HasMaxLength(100).IsRequired();
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.IsActive).HasDefaultValue(true);

            entity.HasIndex(e => e.Code).IsUnique();
            entity.HasIndex(e => e.Name).IsUnique();

            entity.HasOne(e => e.CreatedByUser)
                .WithMany()
                .HasForeignKey(e => e.CreatedBy)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // 5. Doctors
        modelBuilder.Entity<Doctor>(entity =>
        {
            entity.ToTable("Doctors");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasDefaultValueSql("NEWSEQUENTIALID()");
            entity.Property(e => e.DoctorCode).HasMaxLength(20).IsRequired();
            entity.Property(e => e.FullName).HasMaxLength(100).IsRequired();
            entity.Property(e => e.Title).HasMaxLength(50);
            entity.Property(e => e.Bio).HasMaxLength(1000);
            entity.Property(e => e.AvatarUrl).HasMaxLength(500);
            entity.Property(e => e.IsActive).HasDefaultValue(true);

            entity.HasIndex(e => e.DoctorCode).IsUnique();
            entity.HasIndex(e => e.UserId).IsUnique();

            entity.HasOne(e => e.User)
                .WithOne(u => u.Doctor)
                .HasForeignKey<Doctor>(e => e.UserId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(e => e.CreatedByUser)
                .WithMany()
                .HasForeignKey(e => e.CreatedBy)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // 6. DoctorSpecialties
        modelBuilder.Entity<DoctorSpecialty>(entity =>
        {
            entity.ToTable("DoctorSpecialties");
            entity.HasKey(e => new { e.DoctorId, e.SpecialtyId });

            entity.HasOne(e => e.Doctor)
                .WithMany(d => d.DoctorSpecialties)
                .HasForeignKey(e => e.DoctorId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Specialty)
                .WithMany(s => s.DoctorSpecialties)
                .HasForeignKey(e => e.SpecialtyId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // 7. WorkingSchedules
        modelBuilder.Entity<WorkingSchedule>(entity =>
        {
            entity.ToTable("WorkingSchedules");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Note).HasMaxLength(500);

            // BR02 / BR03: Bác sĩ không có 2 ca trùng thời gian bắt đầu cùng ngày
            entity.HasIndex(e => new { e.DoctorId, e.WorkDate, e.StartTime }).IsUnique();

            entity.HasOne(e => e.Doctor)
                .WithMany(d => d.WorkingSchedules)
                .HasForeignKey(e => e.DoctorId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.CreatedByUser)
                .WithMany()
                .HasForeignKey(e => e.CreatedBy)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // 8. TimeSlots
        modelBuilder.Entity<TimeSlot>(entity =>
        {
            entity.ToTable("TimeSlots");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.ClosedReason).HasMaxLength(500);
            entity.Property(e => e.RowVersion).IsRowVersion();

            entity.HasIndex(e => new { e.DoctorId, e.SlotDate, e.StartTime });
            entity.HasIndex(e => new { e.DoctorId, e.SlotDate, e.Status });

            entity.HasOne(e => e.WorkingSchedule)
                .WithMany(w => w.TimeSlots)
                .HasForeignKey(e => e.WorkingScheduleId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Doctor)
                .WithMany()
                .HasForeignKey(e => e.DoctorId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.ClosedByUser)
                .WithMany()
                .HasForeignKey(e => e.ClosedBy)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // 9. Appointments
        modelBuilder.Entity<Appointment>(entity =>
        {
            entity.ToTable("Appointments");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.AppointmentCode).HasMaxLength(20).IsRequired();
            entity.Property(e => e.Notes).HasMaxLength(1000);
            entity.Property(e => e.CancellationReason).HasMaxLength(500);
            entity.Property(e => e.RescheduleReason).HasMaxLength(500);
            entity.Property(e => e.RowVersion).IsRowVersion();

            entity.HasIndex(e => e.AppointmentCode).IsUnique();
            entity.Property(e => e.RequestFingerprint).HasMaxLength(64);
            entity.HasIndex(e => new { e.BookedBy, e.RequestKey }).IsUnique()
                .HasFilter("[RequestKey] IS NOT NULL");
            entity.HasIndex(e => new { e.PatientId, e.AppointmentDate });
            entity.HasIndex(e => new { e.DoctorId, e.AppointmentDate });
            entity.HasIndex(e => e.TimeSlotId).IsUnique().HasFilter("[Status] IN (0, 2)");
            entity.HasIndex(e => e.Status);

            entity.HasOne(e => e.Patient)
                .WithMany(p => p.Appointments)
                .HasForeignKey(e => e.PatientId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.Doctor)
                .WithMany(d => d.Appointments)
                .HasForeignKey(e => e.DoctorId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.TimeSlot)
                .WithMany(t => t.Appointments)
                .HasForeignKey(e => e.TimeSlotId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.Specialty)
                .WithMany(s => s.Appointments)
                .HasForeignKey(e => e.SpecialtyId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.BookedByUser)
                .WithMany()
                .HasForeignKey(e => e.BookedBy)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.CheckedInByUser)
                .WithMany()
                .HasForeignKey(e => e.CheckedInBy)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.MarkedNoShowByUser)
                .WithMany()
                .HasForeignKey(e => e.MarkedNoShowBy)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // 10. AppointmentStatusHistories
        modelBuilder.Entity<AppointmentStatusHistory>(entity =>
        {
            entity.ToTable("AppointmentStatusHistories");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Reason).HasMaxLength(500);

            entity.HasIndex(e => e.AppointmentId);

            entity.HasOne(e => e.Appointment)
                .WithMany(a => a.StatusHistories)
                .HasForeignKey(e => e.AppointmentId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.ChangedByUser)
                .WithMany()
                .HasForeignKey(e => e.ChangedBy)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // 11. Notifications
        modelBuilder.Entity<Notification>(entity =>
        {
            entity.ToTable("Notifications");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Title).HasMaxLength(200).IsRequired();
            entity.Property(e => e.Message).HasMaxLength(1000).IsRequired();

            entity.HasIndex(e => new { e.PatientId, e.CreatedAt });
            entity.HasIndex(e => e.OutboxId).IsUnique().HasFilter("[OutboxId] IS NOT NULL");

            entity.HasOne(e => e.Patient)
                .WithMany(p => p.Notifications)
                .HasForeignKey(e => e.PatientId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Appointment)
                .WithMany(a => a.Notifications)
                .HasForeignKey(e => e.AppointmentId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // 12. AuditLogs
        modelBuilder.Entity<AuditLog>(entity =>
        {
            entity.ToTable("AuditLogs");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Action).HasMaxLength(100).IsRequired();
            entity.Property(e => e.EntityType).HasMaxLength(100).IsRequired();
            entity.Property(e => e.EntityId).HasMaxLength(50);
            entity.Property(e => e.IpAddress).HasMaxLength(50);

            entity.HasIndex(e => new { e.UserId, e.CreatedAt });
            entity.HasIndex(e => new { e.EntityType, e.EntityId });

            entity.HasOne(e => e.User)
                .WithMany(u => u.AuditLogs)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<NotificationOutbox>(entity =>
        {
            entity.ToTable("NotificationOutbox");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Title).HasMaxLength(200);
            entity.Property(e => e.Message).HasMaxLength(1000);
            entity.HasIndex(e => new { e.DeliveredAt, e.SuppressedAt, e.ScheduledFor });
            entity.HasOne<Patient>().WithMany().HasForeignKey(e => e.PatientId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<Appointment>().WithMany().HasForeignKey(e => e.AppointmentId).OnDelete(DeleteBehavior.Restrict);
        });
    }
}
