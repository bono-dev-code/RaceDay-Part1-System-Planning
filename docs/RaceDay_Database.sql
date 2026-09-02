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