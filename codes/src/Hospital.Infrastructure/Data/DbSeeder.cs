using Hospital.Application.Common.Interfaces;
using Hospital.Domain.Entities;
using Hospital.Domain.Enums;
using Hospital.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;

namespace Hospital.Infrastructure.Data;

public static class DbSeeder
{
    public static async Task SeedAsync(HospitalDbContext context, IPasswordHasher passwordHasher, ClinicTime? clinicTime = null)
    {
        var clock = clinicTime ?? new ClinicTime(TimeProvider.System);
        // 1. Seed Roles
        if (!await context.Roles.AnyAsync())
        {
            var roles = new List<Role>
            {
                new() { Id = (int)UserRole.Patient, Name = "Patient", Description = "Bệnh nhân tự đặt và quản lý lịch khám" },
                new() { Id = (int)UserRole.Doctor, Name = "Doctor", Description = "Bác sĩ xem lịch làm việc và danh sách khám" },
                new() { Id = (int)UserRole.Staff, Name = "Staff", Description = "Nhân viên tiếp đón, hỗ trợ và điều phối lịch" },
                new() { Id = (int)UserRole.Admin, Name = "Admin", Description = "Quản trị viên quản lý danh mục, lịch LV và phân quyền" }
            };

            await context.Roles.AddRangeAsync(roles);
            await context.SaveChangesAsync();
        }

        // 2. Seed Users
        if (!await context.Users.AnyAsync())
        {
            var adminId = Guid.NewGuid();
            var staffId = Guid.NewGuid();
            var doc1UserId = Guid.NewGuid();
            var doc2UserId = Guid.NewGuid();
            var pat1UserId = Guid.NewGuid();
            var pat2UserId = Guid.NewGuid();

            var defaultPasswordHash = passwordHasher.HashPassword("Admin@123");
            var staffPasswordHash = passwordHasher.HashPassword("Staff@123");
            var doctorPasswordHash = passwordHasher.HashPassword("Doctor@123");
            var patientPasswordHash = passwordHasher.HashPassword("Patient@123");

            var adminUser = new User
            {
                Id = adminId,
                Username = "admin",
                PasswordHash = defaultPasswordHash,
                Email = "admin@hospital.com",
                PhoneNumber = "0901000001",
                FullName = "Quản trị viên Hệ thống",
                RoleId = (int)UserRole.Admin,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            var staffUser = new User
            {
                Id = staffId,
                Username = "staff1",
                PasswordHash = staffPasswordHash,
                Email = "staff1@hospital.com",
                PhoneNumber = "0902000002",
                FullName = "Nguyễn Văn An (Tiếp tân)",
                RoleId = (int)UserRole.Staff,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            var doc1User = new User
            {
                Id = doc1UserId,
                Username = "doctor1",
                PasswordHash = doctorPasswordHash,
                Email = "doctor1@hospital.com",
                PhoneNumber = "0903000003",
                FullName = "BS. CKII Trần Minh Đức",
                RoleId = (int)UserRole.Doctor,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            var doc2User = new User
            {
                Id = doc2UserId,
                Username = "doctor2",
                PasswordHash = doctorPasswordHash,
                Email = "doctor2@hospital.com",
                PhoneNumber = "0904000004",
                FullName = "ThS. BS Lê Thị Mai Hoa",
                RoleId = (int)UserRole.Doctor,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            var pat1User = new User
            {
                Id = pat1UserId,
                Username = "patient1",
                PasswordHash = patientPasswordHash,
                Email = "patient1@gmail.com",
                PhoneNumber = "0912345678",
                FullName = "Phạm Văn Hùng",
                RoleId = (int)UserRole.Patient,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            var pat2User = new User
            {
                Id = pat2UserId,
                Username = "patient2",
                PasswordHash = patientPasswordHash,
                Email = "patient2@gmail.com",
                PhoneNumber = "0987654321",
                FullName = "Hoàng Thị Lan",
                RoleId = (int)UserRole.Patient,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            await context.Users.AddRangeAsync(adminUser, staffUser, doc1User, doc2User, pat1User, pat2User);
            await context.SaveChangesAsync();

            // 3. Seed Patients
            var pat1 = new Patient
            {
                Id = Guid.NewGuid(),
                UserId = pat1UserId,
                PatientCode = "BN-2026-0001",
                DateOfBirth = new DateOnly(1990, 5, 15),
                Gender = Gender.Male,
                Address = "123 Nguyễn Trãi, Quận 1, TP. Hồ Chí Minh",
                CreatedAt = DateTime.UtcNow
            };

            var pat2 = new Patient
            {
                Id = Guid.NewGuid(),
                UserId = pat2UserId,
                PatientCode = "BN-2026-0002",
                DateOfBirth = new DateOnly(1995, 10, 20),
                Gender = Gender.Female,
                Address = "456 Lê Lợi, Quận 5, TP. Hồ Chí Minh",
                CreatedAt = DateTime.UtcNow
            };

            await context.Patients.AddRangeAsync(pat1, pat2);

            // 4. Seed Specialties
            var sp1 = new Specialty { Code = "NOI", Name = "Nội tổng quát", Description = "Khám và điều trị các bệnh lý nội khoa người lớn", IsActive = true, CreatedBy = adminId };
            var sp2 = new Specialty { Code = "NHI", Name = "Nhi khoa", Description = "Chăm sóc sức khỏe toàn diện và khám bệnh cho trẻ em", IsActive = true, CreatedBy = adminId };
            var sp3 = new Specialty { Code = "DL", Name = "Da liễu", Description = "Khám, tư vấn và điều trị các bệnh ngoài da và thẩm mỹ da", IsActive = true, CreatedBy = adminId };
            var sp4 = new Specialty { Code = "TMH", Name = "Tai Mũi Họng", Description = "Chẩn đoán và điều trị bệnh lý tai, mũi, xoang, họng và thanh quản", IsActive = true, CreatedBy = adminId };
            var sp5 = new Specialty { Code = "MAT", Name = "Mắt", Description = "Thăm khám thị lực và các bệnh lý chuyên sâu về nhãn khoa", IsActive = true, CreatedBy = adminId };

            await context.Specialties.AddRangeAsync(sp1, sp2, sp3, sp4, sp5);
            await context.SaveChangesAsync();

            // 5. Seed Doctors
            var doc1Id = Guid.NewGuid();
            var doc2Id = Guid.NewGuid();

            var doc1 = new Doctor
            {
                Id = doc1Id,
                UserId = doc1UserId,
                DoctorCode = "BS-0001",
                FullName = doc1User.FullName,
                Title = "Bác sĩ Chuyên khoa II",
                Bio = "Hơn 15 năm kinh nghiệm trong lĩnh vực Nội khoa và Tim mạch. Từng công tác tại BV Chợ Rẫy.",
                AvatarUrl = "/images/doctors/doc1.jpg",
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = adminId
            };

            var doc2 = new Doctor
            {
                Id = doc2Id,
                UserId = doc2UserId,
                DoctorCode = "BS-0002",
                FullName = doc2User.FullName,
                Title = "Thạc sĩ Bác sĩ",
                Bio = "Hơn 10 năm kinh nghiệm trong chăm sóc sức khỏe nhi đồng, tâm lý trẻ nhỏ và bệnh lý đường hô hấp.",
                AvatarUrl = "/images/doctors/doc2.jpg",
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = adminId
            };

            await context.Doctors.AddRangeAsync(doc1, doc2);
            await context.SaveChangesAsync();

            // 6. Seed DoctorSpecialties
            await context.DoctorSpecialties.AddRangeAsync(
                new DoctorSpecialty { DoctorId = doc1Id, SpecialtyId = sp1.Id, IsPrimary = true },
                new DoctorSpecialty { DoctorId = doc2Id, SpecialtyId = sp2.Id, IsPrimary = true }
            );
            await context.SaveChangesAsync();

            // 7. Seed Working Schedules & TimeSlots for next 7 days
            var today = clock.Today;
            var schedules = new List<WorkingSchedule>();
            var slots = new List<TimeSlot>();

            for (int i = 0; i < 7; i++)
            {
                var workDate = today.AddDays(i);

                // Morning shift: 08:00 - 11:30
                var sched1 = new WorkingSchedule
                {
                    Id = Guid.NewGuid(),
                    DoctorId = doc1Id,
                    WorkDate = workDate,
                    StartTime = new TimeOnly(8, 0),
                    EndTime = new TimeOnly(11, 30),
                    Note = "Ca sáng khám Nội tổng quát",
                    CreatedBy = adminId,
                    CreatedAt = DateTime.UtcNow
                };
                schedules.Add(sched1);

                // Afternoon shift for doc2: 13:30 - 17:00
                var sched2 = new WorkingSchedule
                {
                    Id = Guid.NewGuid(),
                    DoctorId = doc2Id,
                    WorkDate = workDate,
                    StartTime = new TimeOnly(13, 30),
                    EndTime = new TimeOnly(17, 0),
                    Note = "Ca chiều khám Nhi khoa",
                    CreatedBy = adminId,
                    CreatedAt = DateTime.UtcNow
                };
                schedules.Add(sched2);

                // Generate 30-min slots for sched1
                var slotTime = new TimeOnly(8, 0);
                while (slotTime < new TimeOnly(11, 30))
                {
                    var slotEnd = slotTime.AddMinutes(30);
                    slots.Add(new TimeSlot
                    {
                        Id = Guid.NewGuid(),
                        WorkingScheduleId = sched1.Id,
                        DoctorId = doc1Id,
                        SlotDate = workDate,
                        StartTime = slotTime,
                        EndTime = slotEnd,
                        Status = TimeSlotStatus.Open,
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow
                    });
                    slotTime = slotEnd;
                }

                // Generate 30-min slots for sched2
                slotTime = new TimeOnly(13, 30);
                while (slotTime < new TimeOnly(17, 0))
                {
                    var slotEnd = slotTime.AddMinutes(30);
                    slots.Add(new TimeSlot
                    {
                        Id = Guid.NewGuid(),
                        WorkingScheduleId = sched2.Id,
                        DoctorId = doc2Id,
                        SlotDate = workDate,
                        StartTime = slotTime,
                        EndTime = slotEnd,
                        Status = TimeSlotStatus.Open,
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow
                    });
                    slotTime = slotEnd;
                }
            }

            await context.WorkingSchedules.AddRangeAsync(schedules);
            await context.TimeSlots.AddRangeAsync(slots);
            await context.SaveChangesAsync();

            // 8. Seed sample Appointments
            // Let's take a tomorrow slot for doc1 to book a Confirmed appointment for pat1
            var tomorrow = today.AddDays(1);
            var targetSlot1 = slots.FirstOrDefault(s => s.DoctorId == doc1Id && s.SlotDate == tomorrow && s.StartTime == new TimeOnly(8, 30));
            if (targetSlot1 != null)
            {
                targetSlot1.Status = TimeSlotStatus.Occupied;

                var appt1 = new Appointment
                {
                    Id = Guid.NewGuid(),
                    AppointmentCode = $"APT-{tomorrow:yyyyMMdd}-0001",
                    PatientId = pat1.Id,
                    DoctorId = doc1Id,
                    TimeSlotId = targetSlot1.Id,
                    SpecialtyId = sp1.Id,
                    AppointmentDate = targetSlot1.SlotDate,
                    StartTime = targetSlot1.StartTime,
                    EndTime = targetSlot1.EndTime,
                    Status = AppointmentStatus.Confirmed,
                    Notes = "Bệnh nhân có triệu chứng đau đầu nhẹ và chóng mặt vào buổi sáng.",
                    BookedBy = pat1UserId,
                    CreatedAt = DateTime.UtcNow
                };

                var hist1 = new AppointmentStatusHistory
                {
                    AppointmentId = appt1.Id,
                    OldStatus = null,
                    NewStatus = AppointmentStatus.Confirmed,
                    Reason = "Bệnh nhân tự đặt lịch trực tuyến thành công",
                    ChangedBy = pat1UserId,
                    ChangedAt = DateTime.UtcNow,
                    NewTimeSlotId = targetSlot1.Id
                };

                var notif1 = new Notification
                {
                    Id = Guid.NewGuid(),
                    PatientId = pat1.Id,
                    AppointmentId = appt1.Id,
                    Type = NotificationType.Confirmed,
                    Title = "Đặt lịch khám thành công",
                    Message = $"Lịch hẹn khám Nội tổng quát với BS. Trần Minh Đức vào lúc 08:30 ngày {tomorrow:dd/MM/yyyy} đã được xác nhận. Mã lịch hẹn: {appt1.AppointmentCode}.",
                    IsRead = false,
                    IsSent = true,
                    CreatedAt = DateTime.UtcNow
                };

                await context.Appointments.AddAsync(appt1);
                await context.AppointmentStatusHistories.AddAsync(hist1);
                await context.Notifications.AddAsync(notif1);
            }

            // Seed sample past appointment for pat2 (CheckedIn)
            var targetSlot2 = slots.FirstOrDefault(s => s.DoctorId == doc2Id && s.SlotDate == tomorrow && s.StartTime == new TimeOnly(14, 0));
            if (targetSlot2 != null)
            {
                targetSlot2.Status = TimeSlotStatus.Occupied;

                var appt2 = new Appointment
                {
                    Id = Guid.NewGuid(),
                    AppointmentCode = $"APT-{tomorrow:yyyyMMdd}-0002",
                    PatientId = pat2.Id,
                    DoctorId = doc2Id,
                    TimeSlotId = targetSlot2.Id,
                    SpecialtyId = sp2.Id,
                    AppointmentDate = targetSlot2.SlotDate,
                    StartTime = targetSlot2.StartTime,
                    EndTime = targetSlot2.EndTime,
                    Status = AppointmentStatus.Confirmed,
                    Notes = "Khám sức khỏe định kỳ cho bé 5 tuổi.",
                    BookedBy = staffId,
                    CreatedAt = DateTime.UtcNow
                };

                var hist2 = new AppointmentStatusHistory
                {
                    AppointmentId = appt2.Id,
                    OldStatus = null,
                    NewStatus = AppointmentStatus.Confirmed,
                    Reason = "Nhân viên tiếp tân hỗ trợ đặt lịch qua điện thoại",
                    ChangedBy = staffId,
                    ChangedAt = DateTime.UtcNow,
                    NewTimeSlotId = targetSlot2.Id
                };

                await context.Appointments.AddAsync(appt2);
                await context.AppointmentStatusHistories.AddAsync(hist2);
            }

            // Seed reminder outbox entries using the same clinic timezone as live booking.
            foreach (var appointment in context.ChangeTracker.Entries<Appointment>().Select(e => e.Entity).ToList())
            {
                var at = clock.ToUtc(appointment.AppointmentDate, appointment.StartTime).AddHours(-1);
                if (appointment.Status == AppointmentStatus.Confirmed && at > clock.UtcNow)
                    context.NotificationOutbox.Add(new NotificationOutbox
                    {
                        Id = Guid.NewGuid(), PatientId = appointment.PatientId, AppointmentId = appointment.Id,
                        Type = NotificationType.Reminder, Title = "Nhắc lịch khám sau 1 giờ",
                        Message = $"Lịch {appointment.AppointmentCode}: {appointment.StartTime:HH:mm} ngày {appointment.AppointmentDate:dd/MM/yyyy}.",
                        ScheduledFor = at
                    });
            }

            // 9. Initial Audit Log
            await context.AuditLogs.AddAsync(new AuditLog
            {
                UserId = adminId,
                Action = "SEED_INITIAL_DATA",
                EntityType = "System",
                EntityId = "Init",
                NewValues = "Khởi tạo dữ liệu mẫu thành công với vai trò, tài khoản, chuyên khoa, lịch làm việc và khung giờ.",
                IpAddress = "127.0.0.1",
                CreatedAt = DateTime.UtcNow
            });

            await context.SaveChangesAsync();
        }
    }
}
