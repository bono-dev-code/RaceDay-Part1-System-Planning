/*
    RaceDay Database Script
    PROG6212 POE Part 1
    Student: Bono Nenguda
    Database platform: Microsoft SQL Server
*/

USE master;
GO

IF DB_ID(N'RaceDayDB') IS NULL
BEGIN
    CREATE DATABASE RaceDayDB;
END;
GO

USE RaceDayDB;
GO

SET NOCOUNT ON;
SET XACT_ABORT ON;
GO

-- Tables are dropped in reverse dependency order so the script can be rerun.
DROP TABLE IF EXISTS dbo.[Result];
DROP TABLE IF EXISTS dbo.Enrolment;
DROP TABLE IF EXISTS dbo.Category;
DROP TABLE IF EXISTS dbo.[Event];
DROP TABLE IF EXISTS dbo.[User];
DROP TABLE IF EXISTS dbo.[Role];
GO

  
      -- 1. ROLE
     
CREATE TABLE dbo.[Role]
    (
        RoleID INT IDENTITY(1,1) NOT NULL,
        RoleName VARCHAR(20) NOT NULL,

        CONSTRAINT PK_Role PRIMARY KEY (RoleID),
        CONSTRAINT UQ_Role_RoleName UNIQUE (RoleName),
        CONSTRAINT CK_Role_RoleName
            CHECK (RoleName IN ('Organiser', 'Participant'))
);
GO

   
       -- 2. USER
       
CREATE TABLE dbo.[User]
    (
        UserID INT IDENTITY(1,1) NOT NULL,
        RoleID INT NOT NULL,
        FirstName VARCHAR(50) NOT NULL,
        LastName VARCHAR(50) NOT NULL,
        Email VARCHAR(100) NOT NULL,
        PasswordHash VARCHAR(255) NOT NULL,
        PhoneNumber VARCHAR(20) NULL,
        DateOfBirth DATE NULL,
        ProfilePictureUrl VARCHAR(500) NULL,
        CreatedAt DATETIME2 NOT NULL
            CONSTRAINT DF_User_CreatedAt DEFAULT SYSDATETIME(),

        CONSTRAINT PK_User PRIMARY KEY (UserID),
        CONSTRAINT UQ_User_Email UNIQUE (Email),
        CONSTRAINT FK_User_Role FOREIGN KEY (RoleID)
            REFERENCES dbo.[Role] (RoleID)
);
GO

 -- 3. EVENT
      
CREATE TABLE dbo.[Event]
    (
        EventID INT IDENTITY(1,1) NOT NULL,
        OrganiserID INT NOT NULL,
        EventName VARCHAR(100) NOT NULL,
        Description VARCHAR(1000) NOT NULL,
        EventDate DATETIME2 NOT NULL,
        Location VARCHAR(150) NOT NULL,
        Distance DECIMAL(6,2) NOT NULL,
        EventType VARCHAR(10) NOT NULL,
        BannerImageUrl VARCHAR(500) NULL,
        CreatedAt DATETIME2 NOT NULL
            CONSTRAINT DF_Event_CreatedAt DEFAULT SYSDATETIME(),

        CONSTRAINT PK_Event PRIMARY KEY (EventID),
        CONSTRAINT FK_Event_Organiser FOREIGN KEY (OrganiserID)
            REFERENCES dbo.[User] (UserID),
        CONSTRAINT CK_Event_Distance CHECK (Distance > 0),
        CONSTRAINT CK_Event_EventType
            CHECK (EventType IN ('Run', 'Walk', 'Cycle'))
);
GO

      -- 4. CATEGORY
       
CREATE TABLE dbo.Category
    (
        CategoryID INT IDENTITY(1,1) NOT NULL,
        EventID INT NOT NULL,
        CategoryName VARCHAR(100) NOT NULL,
        CategoryType VARCHAR(20) NOT NULL,
        Description VARCHAR(255) NULL,

        CONSTRAINT PK_Category PRIMARY KEY (CategoryID),
        CONSTRAINT FK_Category_Event FOREIGN KEY (EventID)
            REFERENCES dbo.[Event] (EventID),
        CONSTRAINT CK_Category_CategoryType
            CHECK (CategoryType IN ('Age', 'Distance'))
);
GO

      -- 5. ENROLMENT
      
CREATE TABLE dbo.Enrolment
    (
        EnrolmentID INT IDENTITY(1,1) NOT NULL,
        ParticipantID INT NOT NULL,
        EventID INT NOT NULL,
        CategoryID INT NOT NULL,
        EnrolmentDate DATETIME2 NOT NULL
            CONSTRAINT DF_Enrolment_EnrolmentDate DEFAULT SYSDATETIME(),
        EnrolmentStatus VARCHAR(20) NOT NULL
            CONSTRAINT DF_Enrolment_EnrolmentStatus DEFAULT 'Pending',

        CONSTRAINT PK_Enrolment PRIMARY KEY (EnrolmentID),
        CONSTRAINT FK_Enrolment_Participant FOREIGN KEY (ParticipantID)
            REFERENCES dbo.[User] (UserID),
        CONSTRAINT FK_Enrolment_Event FOREIGN KEY (EventID)
            REFERENCES dbo.[Event] (EventID),
        CONSTRAINT FK_Enrolment_Category FOREIGN KEY (CategoryID)
            REFERENCES dbo.Category (CategoryID),
        CONSTRAINT UQ_Enrolment_Participant_Event
            UNIQUE (ParticipantID, EventID),
        CONSTRAINT CK_Enrolment_Status
            CHECK (EnrolmentStatus IN ('Pending', 'Confirmed', 'Cancelled'))
);
GO

    
      -- 6. RESULT
      
CREATE TABLE dbo.[Result]
    (
        ResultID INT IDENTITY(1,1) NOT NULL,
        EnrolmentID INT NOT NULL,
        FinishTime TIME NOT NULL,
        FinishingPosition INT NOT NULL,
        PublishedAt DATETIME2 NOT NULL
            CONSTRAINT DF_Result_PublishedAt DEFAULT SYSDATETIME(),

        CONSTRAINT PK_Result PRIMARY KEY (ResultID),
        CONSTRAINT UQ_Result_EnrolmentID UNIQUE (EnrolmentID),
        CONSTRAINT FK_Result_Enrolment FOREIGN KEY (EnrolmentID)
            REFERENCES dbo.Enrolment (EnrolmentID),
        CONSTRAINT CK_Result_FinishTime CHECK (FinishTime > '00:00:00'),
        CONSTRAINT CK_Result_FinishingPosition CHECK (FinishingPosition > 0)
);
GO

      -- SEED DATA
       

INSERT INTO dbo.[Role] (RoleName)
    VALUES
        ('Organiser'),
        ('Participant');
GO

    -- Two Organisers and two Participants.
INSERT INTO dbo.[User]
        (RoleID, FirstName, LastName, Email, PasswordHash,
         PhoneNumber, DateOfBirth, ProfilePictureUrl)
    VALUES
        ((SELECT RoleID FROM dbo.[Role] WHERE RoleName = 'Organiser'),
         'Nenguda', 'Bono', 'nenguda.bono@raceday.co.za',
         CONVERT(VARCHAR(64), HASHBYTES('SHA2_256', 'Nenguda@2026'), 2),
         '0825550142', '1988-04-12', NULL),

        ((SELECT RoleID FROM dbo.[Role] WHERE RoleName = 'Organiser'),
         'Sikhwetha', 'Brenda', 'sikhwetha.brenda@raceday.co.za',
         CONVERT(VARCHAR(64), HASHBYTES('SHA2_256', 'Sikhwetha@2026'), 2),
         '0835550198', '1985-09-27', NULL),

        ((SELECT RoleID FROM dbo.[Role] WHERE RoleName = 'Participant'),
         'Mbuyelo', 'Nkuna', 'mbuyelo.nkuna@example.co.za',
         CONVERT(VARCHAR(64), HASHBYTES('SHA2_256', 'Mbuyelo@2026'), 2),
         '0715550123', '1999-06-18', NULL),

        ((SELECT RoleID FROM dbo.[Role] WHERE RoleName = 'Participant'),
         'Mafalo', 'Joy', 'mafalo.joy@example.co.za',
         CONVERT(VARCHAR(64), HASHBYTES('SHA2_256', 'Mafalo@2026'), 2),
         '0765550174', '1992-11-03', NULL);
GO

    -- Three South African RaceDay events.
INSERT INTO dbo.[Event]
        (OrganiserID, EventName, Description, EventDate, Location,
         Distance, EventType, BannerImageUrl)
    VALUES
        ((SELECT UserID FROM dbo.[User] WHERE Email = 'nenguda.bono@raceday.co.za'),
         'Venda Sunrise Heritage Run',
         'A sunrise road race celebrating Venda heritage, community and fitness.',
         '2026-08-23T06:30:00', 'Thohoyandou Stadium, Limpopo',
         21.10, 'Run', NULL),

        ((SELECT UserID FROM dbo.[User] WHERE Email = 'sikhwetha.brenda@raceday.co.za'),
         'FNB Ubuntu Wellness Walk',
         'A family wellness walk promoting healthy and active communities.',
         '2026-10-17T07:00:00', 'FNB Stadium, Johannesburg',
         10.00, 'Walk', NULL),

        ((SELECT UserID FROM dbo.[User] WHERE Email = 'nenguda.bono@raceday.co.za'),
         'Cape Town Coastal Cycle Challenge',
         'A scenic cycling challenge for recreational and experienced cyclists.',
         '2026-12-05T07:30:00', 'Cape Town Stadium, Western Cape',
         80.00, 'Cycle', NULL);
GO

    -- Every event receives age and/or distance categories.
INSERT INTO dbo.Category
        (EventID, CategoryName, CategoryType, Description)
    VALUES
        ((SELECT EventID FROM dbo.[Event]
          WHERE EventName = 'Venda Sunrise Heritage Run'),
         '10km', 'Distance', 'Ten-kilometre road-running category.'),

        ((SELECT EventID FROM dbo.[Event]
          WHERE EventName = 'Venda Sunrise Heritage Run'),
         '21.1km', 'Distance', 'Half-marathon distance category.'),

        ((SELECT EventID FROM dbo.[Event]
          WHERE EventName = 'Venda Sunrise Heritage Run'),
         'Under 20', 'Age', 'Participants younger than twenty years.'),

        ((SELECT EventID FROM dbo.[Event]
          WHERE EventName = 'FNB Ubuntu Wellness Walk'),
         '5km', 'Distance', 'Five-kilometre family walking category.'),

        ((SELECT EventID FROM dbo.[Event]
          WHERE EventName = 'FNB Ubuntu Wellness Walk'),
         '10km', 'Distance', 'Ten-kilometre wellness walking category.'),

        ((SELECT EventID FROM dbo.[Event]
          WHERE EventName = 'Cape Town Coastal Cycle Challenge'),
         '40km', 'Distance', 'Recreational forty-kilometre cycling category.'),

        ((SELECT EventID FROM dbo.[Event]
          WHERE EventName = 'Cape Town Coastal Cycle Challenge'),
         '80km', 'Distance', 'Full eighty-kilometre cycling category.'),

        ((SELECT EventID FROM dbo.[Event]
          WHERE EventName = 'Cape Town Coastal Cycle Challenge'),
         'Senior', 'Age', 'Age category for senior participants.');
GO

    -- Sample enrolments connect Participants, Events and Categories.
INSERT INTO dbo.Enrolment
        (ParticipantID, EventID, CategoryID, EnrolmentDate, EnrolmentStatus)
    VALUES
        ((SELECT UserID FROM dbo.[User] WHERE Email = 'mbuyelo.nkuna@example.co.za'),
         (SELECT EventID FROM dbo.[Event]
          WHERE EventName = 'Venda Sunrise Heritage Run'),
         (SELECT CategoryID FROM dbo.Category
          WHERE EventID = (SELECT EventID FROM dbo.[Event]
                           WHERE EventName = 'Venda Sunrise Heritage Run')
            AND CategoryName = '10km'),
         '2026-07-10T10:15:00', 'Confirmed'),

        ((SELECT UserID FROM dbo.[User] WHERE Email = 'mafalo.joy@example.co.za'),
         (SELECT EventID FROM dbo.[Event]
          WHERE EventName = 'Venda Sunrise Heritage Run'),
         (SELECT CategoryID FROM dbo.Category
          WHERE EventID = (SELECT EventID FROM dbo.[Event]
                           WHERE EventName = 'Venda Sunrise Heritage Run')
            AND CategoryName = '21.1km'),
         '2026-07-12T14:20:00', 'Confirmed'),

        ((SELECT UserID FROM dbo.[User] WHERE Email = 'mbuyelo.nkuna@example.co.za'),
         (SELECT EventID FROM dbo.[Event]
          WHERE EventName = 'FNB Ubuntu Wellness Walk'),
         (SELECT CategoryID FROM dbo.Category
          WHERE EventID = (SELECT EventID FROM dbo.[Event]
                           WHERE EventName = 'FNB Ubuntu Wellness Walk')
            AND CategoryName = '5km'),
         '2026-08-25T09:05:00', 'Pending'),

        ((SELECT UserID FROM dbo.[User] WHERE Email = 'mafalo.joy@example.co.za'),
         (SELECT EventID FROM dbo.[Event]
          WHERE EventName = 'Cape Town Coastal Cycle Challenge'),
         (SELECT CategoryID FROM dbo.Category
          WHERE EventID = (SELECT EventID FROM dbo.[Event]
                           WHERE EventName = 'Cape Town Coastal Cycle Challenge')
            AND CategoryName = '80km'),
         '2026-08-29T16:45:00', 'Confirmed');
GO

    -- Results are linked to completed enrolments only once.
INSERT INTO dbo.[Result]
        (EnrolmentID, FinishTime, FinishingPosition, PublishedAt)
    VALUES
        ((SELECT en.EnrolmentID
          FROM dbo.Enrolment AS en
          INNER JOIN dbo.[User] AS u ON u.UserID = en.ParticipantID
          INNER JOIN dbo.[Event] AS ev ON ev.EventID = en.EventID
          WHERE u.Email = 'mbuyelo.nkuna@example.co.za'
            AND ev.EventName = 'Venda Sunrise Heritage Run'),
         '00:54:18', 1, '2026-08-23T11:30:00'),

        ((SELECT en.EnrolmentID
          FROM dbo.Enrolment AS en
          INNER JOIN dbo.[User] AS u ON u.UserID = en.ParticipantID
          INNER JOIN dbo.[Event] AS ev ON ev.EventID = en.EventID
          WHERE u.Email = 'mafalo.joy@example.co.za'
            AND ev.EventName = 'Venda Sunrise Heritage Run'),
         '01:42:37', 2, '2026-08-23T11:30:00');
GO