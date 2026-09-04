/*

    ERD alignment:
      USERS 1---M EVENTS
      EVENT_TYPES 1---M EVENTS
      EVENTS 1---M CATEGORIES
      USERS 1---M ENROLMENTS (ParticipantId)
      EVENTS 1---M ENROLMENTS
      CATEGORIES 1---M ENROLMENTS
      ENROLMENTS 1---0..1 RESULTS

    This script is intended for SQL Server / SSMS and can be run on a clean instance.
*/

IF DB_ID(N'RaceDayDB') IS NULL
BEGIN
    CREATE DATABASE RaceDayDB;
END;
GO

USE RaceDayDB;
GO

/* Re-runnable table creation for a development database. */
IF OBJECT_ID(N'dbo.Results', N'U') IS NOT NULL DROP TABLE dbo.Results;
IF OBJECT_ID(N'dbo.Enrolments', N'U') IS NOT NULL DROP TABLE dbo.Enrolments;
IF OBJECT_ID(N'dbo.Categories', N'U') IS NOT NULL DROP TABLE dbo.Categories;
IF OBJECT_ID(N'dbo.Events', N'U') IS NOT NULL DROP TABLE dbo.Events;
IF OBJECT_ID(N'dbo.EventTypes', N'U') IS NOT NULL DROP TABLE dbo.EventTypes;
IF OBJECT_ID(N'dbo.Users', N'U') IS NOT NULL DROP TABLE dbo.Users;
GO

CREATE TABLE dbo.Users
(
    UserId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Users PRIMARY KEY,
    FirstName NVARCHAR(50) NOT NULL,
    LastName NVARCHAR(50) NOT NULL,
    Email NVARCHAR(255) NOT NULL CONSTRAINT UQ_Users_Email UNIQUE,
    PasswordHash NVARCHAR(500) NOT NULL,
    Role NVARCHAR(20) NOT NULL,
    Phone NVARCHAR(30) NULL,
    DateOfBirth DATE NULL,
    ProfilePictureUrl NVARCHAR(500) NULL,
    CreatedAt DATETIME2(0) NOT NULL CONSTRAINT DF_Users_CreatedAt DEFAULT SYSUTCDATETIME(),
    CONSTRAINT CK_Users_Role CHECK (Role IN (N'Organiser', N'Participant'))
);
GO

CREATE TABLE dbo.EventTypes
(
    EventTypeId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_EventTypes PRIMARY KEY,
    Name NVARCHAR(20) NOT NULL CONSTRAINT UQ_EventTypes_Name UNIQUE,
    CONSTRAINT CK_EventTypes_Name CHECK (Name IN (N'Run', N'Walk', N'Cycle'))
);
GO

CREATE TABLE dbo.Events
(
    EventId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Events PRIMARY KEY,
    OrganiserId INT NOT NULL,
    EventTypeId INT NOT NULL,
    Name NVARCHAR(150) NOT NULL,
    Description NVARCHAR(1000) NOT NULL,
    EventDate DATETIME2(0) NOT NULL,
    Location NVARCHAR(200) NOT NULL,
    DistanceKm DECIMAL(6,2) NOT NULL,
    CreatedAt DATETIME2(0) NOT NULL CONSTRAINT DF_Events_CreatedAt DEFAULT SYSUTCDATETIME(),
    CONSTRAINT CK_Events_Distance CHECK (DistanceKm > 0),
    CONSTRAINT FK_Events_Users_Organiser FOREIGN KEY (OrganiserId)
        REFERENCES dbo.Users(UserId),
    CONSTRAINT FK_Events_EventTypes FOREIGN KEY (EventTypeId)
        REFERENCES dbo.EventTypes(EventTypeId)
);
GO

CREATE TABLE dbo.Categories
(
    CategoryId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Categories PRIMARY KEY,
    EventId INT NOT NULL,
    CategoryType NVARCHAR(20) NOT NULL,
    Name NVARCHAR(100) NOT NULL,
    MinAge INT NULL,
    MaxAge INT NULL,
    DistanceKm DECIMAL(6,2) NULL,
    CONSTRAINT FK_Categories_Events FOREIGN KEY (EventId)
        REFERENCES dbo.Events(EventId) ON DELETE CASCADE,
    CONSTRAINT CK_Categories_Type CHECK (CategoryType IN (N'Age', N'Distance')),
    CONSTRAINT CK_Categories_Age CHECK
    (
        (CategoryType = N'Age' AND MinAge IS NOT NULL AND MaxAge IS NOT NULL AND MinAge >= 0 AND MaxAge >= MinAge)
        OR
        (CategoryType = N'Distance' AND DistanceKm IS NOT NULL AND DistanceKm > 0 AND MinAge IS NULL AND MaxAge IS NULL)
    )
);
GO

CREATE TABLE dbo.Enrolments
(
    EnrolmentId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Enrolments PRIMARY KEY,
    EventId INT NOT NULL,
    ParticipantId INT NOT NULL,
    CategoryId INT NOT NULL,
    EnrolmentStatus NVARCHAR(20) NOT NULL CONSTRAINT DF_Enrolments_Status DEFAULT N'Confirmed',
    EnrolledAt DATETIME2(0) NOT NULL CONSTRAINT DF_Enrolments_EnrolledAt DEFAULT SYSUTCDATETIME(),
    CONSTRAINT FK_Enrolments_Events FOREIGN KEY (EventId)
        REFERENCES dbo.Events(EventId) ON DELETE CASCADE,
    CONSTRAINT FK_Enrolments_Users_Participant FOREIGN KEY (ParticipantId)
        REFERENCES dbo.Users(UserId),
    CONSTRAINT FK_Enrolments_Categories FOREIGN KEY (CategoryId)
        REFERENCES dbo.Categories(CategoryId),
    CONSTRAINT UQ_Enrolments_Event_Participant UNIQUE (EventId, ParticipantId),
    CONSTRAINT CK_Enrolments_Status CHECK (EnrolmentStatus IN (N'Pending', N'Confirmed', N'Cancelled'))
);
GO

CREATE TABLE dbo.Results
(
    ResultId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Results PRIMARY KEY,
    EnrolmentId INT NOT NULL CONSTRAINT UQ_Results_Enrolment UNIQUE,
    FinishTime TIME(0) NOT NULL,
    FinishingPosition INT NOT NULL,
    RecordedAt DATETIME2(0) NOT NULL CONSTRAINT DF_Results_RecordedAt DEFAULT SYSUTCDATETIME(),
    CONSTRAINT FK_Results_Enrolments FOREIGN KEY (EnrolmentId)
        REFERENCES dbo.Enrolments(EnrolmentId) ON DELETE CASCADE,
    CONSTRAINT CK_Results_Position CHECK (FinishingPosition > 0)
);
GO

/* Seed event types. */
INSERT INTO dbo.EventTypes (Name)
VALUES (N'Run'), (N'Walk'), (N'Cycle');
GO

/* Seed users.
   The password values are clearly labelled demonstration hashes for Part 1 data.
   Part 2 should generate/store the final ASP.NET-compatible password hashes. */
INSERT INTO dbo.Users
    (FirstName, LastName, Email, PasswordHash, Role, Phone, DateOfBirth, ProfilePictureUrl)
VALUES
    (N'Lerato', N'Mokoena', N'lerato.organiser@raceday.test', N'PART1_DEMO_HASH_ORG_001', N'Organiser', N'0825550101', N'1990-04-12', NULL),
    (N'Thabo', N'Nkosi', N'thabo.organiser@raceday.test', N'PART1_DEMO_HASH_ORG_002', N'Organiser', N'0835550102', N'1987-11-03', NULL),
    (N'Ayanda', N'Dlamini', N'ayanda.participant@raceday.test', N'PART1_DEMO_HASH_PART_001', N'Participant', N'0845550103', N'2001-06-20', NULL),
    (N'Kabelo', N'Molefe', N'kabelo.participant@raceday.test', N'PART1_DEMO_HASH_PART_002', N'Participant', N'0855550104', N'1998-09-17', NULL);
GO

/* Three sample events. */
INSERT INTO dbo.Events
    (OrganiserId, EventTypeId, Name, Description, EventDate, Location, DistanceKm)
VALUES
    (1, 1, N'Polokwane Spring Run', N'A community road running event with age and distance categories.', N'2026-10-10T07:00:00', N'Polokwane, Limpopo', 10.00),
    (1, 2, N'Soweto Community Walk', N'An inclusive community walking event for recreational participants.', N'2026-10-24T08:00:00', N'Soweto, Gauteng', 5.00),
    (2, 3, N'Cape Winelands Cycle Challenge', N'A road cycling event through selected Cape Winelands routes.', N'2026-11-07T06:30:00', N'Stellies, Western Cape', 42.00);
GO

/* Categories for each event: age and/or distance based. */
INSERT INTO dbo.Categories
    (EventId, CategoryType, Name, MinAge, MaxAge, DistanceKm)
VALUES
    (1, N'Age',      N'Under 20',       0,   19,   NULL),
    (1, N'Age',      N'Senior',         20,  39,   NULL),
    (1, N'Distance', N'10km Open',      NULL, NULL, 10.00),
    (2, N'Age',      N'Junior Walk',    0,   19,   NULL),
    (2, N'Age',      N'Open Walk',      20,  99,   NULL),
    (3, N'Distance', N'42km Open',      NULL, NULL, 42.00),
    (3, N'Distance', N'21km Challenge', NULL, NULL, 21.00);
GO

/* Sample participant enrolments. */
INSERT INTO dbo.Enrolments
    (EventId, ParticipantId, CategoryId, EnrolmentStatus)
VALUES
    (1, 3, 2, N'Confirmed'),
    (1, 4, 1, N'Confirmed'),
    (2, 3, 5, N'Confirmed'),
    (3, 4, 6, N'Confirmed');
GO

/* Sample results for completed/demo records. */
INSERT INTO dbo.Results
    (EnrolmentId, FinishTime, FinishingPosition)
VALUES
    (1, '00:52:18', 47),
    (2, '01:04:35', 102);
GO

/* Basic verification queries for SSMS demo. */
SELECT * FROM dbo.Users;
SELECT * FROM dbo.EventTypes;
SELECT * FROM dbo.Events;
SELECT * FROM dbo.Categories;
SELECT * FROM dbo.Enrolments;
SELECT * FROM dbo.Results;
GO
