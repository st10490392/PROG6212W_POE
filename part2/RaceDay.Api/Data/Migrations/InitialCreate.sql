IF OBJECT_ID(N'[__EFMigrationsHistory]') IS NULL
BEGIN
    CREATE TABLE [__EFMigrationsHistory] (
        [MigrationId] nvarchar(150) NOT NULL,
        [ProductVersion] nvarchar(32) NOT NULL,
        CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
    );
END;
GO

BEGIN TRANSACTION;
CREATE TABLE [EventTypes] (
    [EventTypeId] int NOT NULL IDENTITY,
    [Name] nvarchar(20) NOT NULL,
    CONSTRAINT [PK_EventTypes] PRIMARY KEY ([EventTypeId]),
    CONSTRAINT [CK_EventTypes_Name] CHECK (Name IN ('Run', 'Walk', 'Cycle'))
);

CREATE TABLE [Users] (
    [UserId] int NOT NULL IDENTITY,
    [FirstName] nvarchar(50) NOT NULL,
    [LastName] nvarchar(50) NOT NULL,
    [Email] nvarchar(255) NOT NULL,
    [PasswordHash] nvarchar(500) NOT NULL,
    [Role] nvarchar(20) NOT NULL,
    [Phone] nvarchar(30) NULL,
    [DateOfBirth] date NULL,
    [ProfilePictureUrl] nvarchar(500) NULL,
    [CreatedAt] datetime2(0) NOT NULL DEFAULT (SYSUTCDATETIME()),
    CONSTRAINT [PK_Users] PRIMARY KEY ([UserId]),
    CONSTRAINT [CK_Users_Role] CHECK (Role IN ('Organiser', 'Participant'))
);

CREATE TABLE [Events] (
    [EventId] int NOT NULL IDENTITY,
    [OrganiserId] int NOT NULL,
    [EventTypeId] int NOT NULL,
    [Name] nvarchar(150) NOT NULL,
    [Description] nvarchar(1000) NOT NULL,
    [EventDate] datetime2(0) NOT NULL,
    [Location] nvarchar(200) NOT NULL,
    [DistanceKm] decimal(6,2) NOT NULL,
    [CreatedAt] datetime2(0) NOT NULL DEFAULT (SYSUTCDATETIME()),
    CONSTRAINT [PK_Events] PRIMARY KEY ([EventId]),
    CONSTRAINT [CK_Events_Distance] CHECK (DistanceKm > 0),
    CONSTRAINT [FK_Events_EventTypes_EventTypeId] FOREIGN KEY ([EventTypeId]) REFERENCES [EventTypes] ([EventTypeId]),
    CONSTRAINT [FK_Events_Users_OrganiserId] FOREIGN KEY ([OrganiserId]) REFERENCES [Users] ([UserId])
);

CREATE TABLE [Categories] (
    [CategoryId] int NOT NULL IDENTITY,
    [EventId] int NOT NULL,
    [CategoryType] nvarchar(20) NOT NULL,
    [Name] nvarchar(100) NOT NULL,
    [MinAge] int NULL,
    [MaxAge] int NULL,
    [DistanceKm] decimal(6,2) NULL,
    CONSTRAINT [PK_Categories] PRIMARY KEY ([CategoryId]),
    CONSTRAINT [CK_Categories_Age] CHECK ((CategoryType = 'Age' AND MinAge IS NOT NULL AND MaxAge IS NOT NULL AND MinAge >= 0 AND MaxAge >= MinAge) OR (CategoryType = 'Distance' AND DistanceKm IS NOT NULL AND DistanceKm > 0 AND MinAge IS NULL AND MaxAge IS NULL)),
    CONSTRAINT [CK_Categories_Type] CHECK (CategoryType IN ('Age', 'Distance')),
    CONSTRAINT [FK_Categories_Events_EventId] FOREIGN KEY ([EventId]) REFERENCES [Events] ([EventId]) ON DELETE CASCADE
);

CREATE TABLE [Enrolments] (
    [EnrolmentId] int NOT NULL IDENTITY,
    [EventId] int NOT NULL,
    [ParticipantId] int NOT NULL,
    [CategoryId] int NOT NULL,
    [EnrolmentStatus] nvarchar(20) NOT NULL DEFAULT N'Confirmed',
    [EnrolledAt] datetime2(0) NOT NULL DEFAULT (SYSUTCDATETIME()),
    CONSTRAINT [PK_Enrolments] PRIMARY KEY ([EnrolmentId]),
    CONSTRAINT [CK_Enrolments_Status] CHECK (EnrolmentStatus IN ('Pending', 'Confirmed', 'Cancelled')),
    CONSTRAINT [FK_Enrolments_Categories_CategoryId] FOREIGN KEY ([CategoryId]) REFERENCES [Categories] ([CategoryId]),
    CONSTRAINT [FK_Enrolments_Events_EventId] FOREIGN KEY ([EventId]) REFERENCES [Events] ([EventId]) ON DELETE CASCADE,
    CONSTRAINT [FK_Enrolments_Users_ParticipantId] FOREIGN KEY ([ParticipantId]) REFERENCES [Users] ([UserId])
);

CREATE TABLE [Results] (
    [ResultId] int NOT NULL IDENTITY,
    [EnrolmentId] int NOT NULL,
    [FinishTime] time(0) NOT NULL,
    [FinishingPosition] int NOT NULL,
    [RecordedAt] datetime2(0) NOT NULL DEFAULT (SYSUTCDATETIME()),
    CONSTRAINT [PK_Results] PRIMARY KEY ([ResultId]),
    CONSTRAINT [CK_Results_Position] CHECK (FinishingPosition > 0),
    CONSTRAINT [FK_Results_Enrolments_EnrolmentId] FOREIGN KEY ([EnrolmentId]) REFERENCES [Enrolments] ([EnrolmentId]) ON DELETE CASCADE
);

IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'EventTypeId', N'Name') AND [object_id] = OBJECT_ID(N'[EventTypes]'))
    SET IDENTITY_INSERT [EventTypes] ON;
INSERT INTO [EventTypes] ([EventTypeId], [Name])
VALUES (1, N'Run'),
(2, N'Walk'),
(3, N'Cycle');
IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'EventTypeId', N'Name') AND [object_id] = OBJECT_ID(N'[EventTypes]'))
    SET IDENTITY_INSERT [EventTypes] OFF;

CREATE INDEX [IX_Categories_EventId] ON [Categories] ([EventId]);

CREATE INDEX [IX_Enrolments_CategoryId] ON [Enrolments] ([CategoryId]);

CREATE INDEX [IX_Enrolments_ParticipantId] ON [Enrolments] ([ParticipantId]);

CREATE UNIQUE INDEX [UQ_Enrolments_Event_Participant] ON [Enrolments] ([EventId], [ParticipantId]);

CREATE INDEX [IX_Events_EventTypeId] ON [Events] ([EventTypeId]);

CREATE INDEX [IX_Events_OrganiserId] ON [Events] ([OrganiserId]);

CREATE UNIQUE INDEX [UQ_EventTypes_Name] ON [EventTypes] ([Name]);

CREATE UNIQUE INDEX [UQ_Results_Enrolment] ON [Results] ([EnrolmentId]);

CREATE UNIQUE INDEX [UQ_Users_Email] ON [Users] ([Email]);

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20261017091246_InitialCreate', N'10.0.13');

COMMIT;
GO
