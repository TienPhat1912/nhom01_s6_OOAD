BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261016143621_Phase3Backend'
)
BEGIN
    DROP INDEX [IX_Appointments_BookedBy] ON [Appointments];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261016143621_Phase3Backend'
)
BEGIN
    DROP INDEX [IX_Appointments_TimeSlotId] ON [Appointments];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261016143621_Phase3Backend'
)
BEGIN
    ALTER TABLE [Users] ADD [TokenVersion] int NOT NULL DEFAULT 0;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261016143621_Phase3Backend'
)
BEGIN
    ALTER TABLE [Notifications] ADD [OutboxId] uniqueidentifier NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261016143621_Phase3Backend'
)
BEGIN
    ALTER TABLE [Doctors] ADD [FullName] nvarchar(100) NOT NULL DEFAULT N'';
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261016143621_Phase3Backend'
)
BEGIN
    ALTER TABLE [Appointments] ADD [RequestFingerprint] nvarchar(64) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261016143621_Phase3Backend'
)
BEGIN
    EXEC(N'UPDATE d SET FullName = COALESCE(NULLIF(u.FullName, ''''), d.DoctorCode)
    FROM Doctors d LEFT JOIN Users u ON d.UserId = u.Id;
    UPDATE Notifications SET IsSent = 1 WHERE Type IN (0, 1, 2);');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261016143621_Phase3Backend'
)
BEGIN
    ALTER TABLE [Appointments] ADD [RequestKey] uniqueidentifier NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261016143621_Phase3Backend'
)
BEGIN
    CREATE TABLE [NotificationOutbox] (
        [Id] uniqueidentifier NOT NULL,
        [PatientId] uniqueidentifier NOT NULL,
        [AppointmentId] uniqueidentifier NOT NULL,
        [Type] tinyint NOT NULL,
        [Title] nvarchar(200) NOT NULL,
        [Message] nvarchar(1000) NOT NULL,
        [ScheduledFor] datetime2 NOT NULL,
        [DeliveredAt] datetime2 NULL,
        [SuppressedAt] datetime2 NULL,
        CONSTRAINT [PK_NotificationOutbox] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_NotificationOutbox_Appointments_AppointmentId] FOREIGN KEY ([AppointmentId]) REFERENCES [Appointments] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_NotificationOutbox_Patients_PatientId] FOREIGN KEY ([PatientId]) REFERENCES [Patients] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261016143621_Phase3Backend'
)
BEGIN
    EXEC(N'INSERT INTO NotificationOutbox
        (Id, PatientId, AppointmentId, Type, Title, Message, ScheduledFor)
    SELECT NEWID(), a.PatientId, a.Id, 3, N''Nhắc lịch khám sau 1 giờ'',
        N''Lịch '' + a.AppointmentCode, reminder.At
    FROM Appointments a
    CROSS APPLY (SELECT DATEADD(HOUR, -8, DATEADD(SECOND,
        DATEDIFF(SECOND, CAST(''00:00:00'' AS time), a.StartTime),
        CAST(a.AppointmentDate AS datetime2))) AS At) reminder
    WHERE a.Status = 0 AND reminder.At > GETUTCDATE() AND a.CreatedAt <= reminder.At;');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261016143621_Phase3Backend'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Specialties_Name] ON [Specialties] ([Name]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261016143621_Phase3Backend'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_Notifications_OutboxId] ON [Notifications] ([OutboxId]) WHERE [OutboxId] IS NOT NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261016143621_Phase3Backend'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_Appointments_BookedBy_RequestKey] ON [Appointments] ([BookedBy], [RequestKey]) WHERE [RequestKey] IS NOT NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261016143621_Phase3Backend'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_Appointments_TimeSlotId] ON [Appointments] ([TimeSlotId]) WHERE [Status] IN (0, 2)');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261016143621_Phase3Backend'
)
BEGIN
    CREATE INDEX [IX_NotificationOutbox_AppointmentId] ON [NotificationOutbox] ([AppointmentId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261016143621_Phase3Backend'
)
BEGIN
    CREATE INDEX [IX_NotificationOutbox_DeliveredAt_SuppressedAt_ScheduledFor] ON [NotificationOutbox] ([DeliveredAt], [SuppressedAt], [ScheduledFor]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261016143621_Phase3Backend'
)
BEGIN
    CREATE INDEX [IX_NotificationOutbox_PatientId] ON [NotificationOutbox] ([PatientId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261016143621_Phase3Backend'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261016143621_Phase3Backend', N'9.0.2');
END;

COMMIT;
GO

