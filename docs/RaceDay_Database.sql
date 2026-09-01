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