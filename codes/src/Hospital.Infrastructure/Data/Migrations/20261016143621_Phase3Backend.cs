using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Hospital.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class Phase3Backend : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Appointments_BookedBy",
                table: "Appointments");

            migrationBuilder.DropIndex(
                name: "IX_Appointments_TimeSlotId",
                table: "Appointments");

            migrationBuilder.AddColumn<int>(
                name: "TokenVersion",
                table: "Users",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<Guid>(
                name: "OutboxId",
                table: "Notifications",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FullName",
                table: "Doctors",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "RequestFingerprint",
                table: "Appointments",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.Sql("""
                EXEC(N'UPDATE d SET FullName = COALESCE(NULLIF(u.FullName, ''''), d.DoctorCode)
                FROM Doctors d LEFT JOIN Users u ON d.UserId = u.Id;
                UPDATE Notifications SET IsSent = 1 WHERE Type IN (0, 1, 2);');
                """);

            migrationBuilder.AddColumn<Guid>(
                name: "RequestKey",
                table: "Appointments",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "NotificationOutbox",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PatientId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AppointmentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Type = table.Column<byte>(type: "tinyint", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Message = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    ScheduledFor = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DeliveredAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    SuppressedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NotificationOutbox", x => x.Id);
                    table.ForeignKey(
                        name: "FK_NotificationOutbox_Appointments_AppointmentId",
                        column: x => x.AppointmentId,
                        principalTable: "Appointments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_NotificationOutbox_Patients_PatientId",
                        column: x => x.PatientId,
                        principalTable: "Patients",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.Sql("""
                EXEC(N'INSERT INTO NotificationOutbox
                    (Id, PatientId, AppointmentId, Type, Title, Message, ScheduledFor)
                SELECT NEWID(), a.PatientId, a.Id, 3, N''Nhắc lịch khám sau 1 giờ'',
                    N''Lịch '' + a.AppointmentCode, reminder.At
                FROM Appointments a
                CROSS APPLY (SELECT DATEADD(HOUR, -8, DATEADD(SECOND,
                    DATEDIFF(SECOND, CAST(''00:00:00'' AS time), a.StartTime),
                    CAST(a.AppointmentDate AS datetime2))) AS At) reminder
                WHERE a.Status = 0 AND reminder.At > GETUTCDATE() AND a.CreatedAt <= reminder.At;');
                """);

            migrationBuilder.CreateIndex(
                name: "IX_Specialties_Name",
                table: "Specialties",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_OutboxId",
                table: "Notifications",
                column: "OutboxId",
                unique: true,
                filter: "[OutboxId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Appointments_BookedBy_RequestKey",
                table: "Appointments",
                columns: new[] { "BookedBy", "RequestKey" },
                unique: true,
                filter: "[RequestKey] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Appointments_TimeSlotId",
                table: "Appointments",
                column: "TimeSlotId",
                unique: true,
                filter: "[Status] IN (0, 2)");

            migrationBuilder.CreateIndex(
                name: "IX_NotificationOutbox_AppointmentId",
                table: "NotificationOutbox",
                column: "AppointmentId");

            migrationBuilder.CreateIndex(
                name: "IX_NotificationOutbox_DeliveredAt_SuppressedAt_ScheduledFor",
                table: "NotificationOutbox",
                columns: new[] { "DeliveredAt", "SuppressedAt", "ScheduledFor" });

            migrationBuilder.CreateIndex(
                name: "IX_NotificationOutbox_PatientId",
                table: "NotificationOutbox",
                column: "PatientId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                IF EXISTS (SELECT TimeSlotId FROM Appointments GROUP BY TimeSlotId HAVING COUNT(*) > 1)
                    THROW 51000, 'Cannot downgrade: slots have multiple historical appointments.', 1;
                """);
            migrationBuilder.DropTable(
                name: "NotificationOutbox");

            migrationBuilder.DropIndex(
                name: "IX_Specialties_Name",
                table: "Specialties");

            migrationBuilder.DropIndex(
                name: "IX_Notifications_OutboxId",
                table: "Notifications");

            migrationBuilder.DropIndex(
                name: "IX_Appointments_BookedBy_RequestKey",
                table: "Appointments");

            migrationBuilder.DropIndex(
                name: "IX_Appointments_TimeSlotId",
                table: "Appointments");

            migrationBuilder.DropColumn(
                name: "TokenVersion",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "OutboxId",
                table: "Notifications");

            migrationBuilder.DropColumn(
                name: "FullName",
                table: "Doctors");

            migrationBuilder.DropColumn(
                name: "RequestFingerprint",
                table: "Appointments");

            migrationBuilder.DropColumn(
                name: "RequestKey",
                table: "Appointments");

            migrationBuilder.CreateIndex(
                name: "IX_Appointments_BookedBy",
                table: "Appointments",
                column: "BookedBy");

            migrationBuilder.CreateIndex(
                name: "IX_Appointments_TimeSlotId",
                table: "Appointments",
                column: "TimeSlotId",
                unique: true);
        }
    }
}
