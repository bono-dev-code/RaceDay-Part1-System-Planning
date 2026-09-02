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