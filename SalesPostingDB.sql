-- Create Database
CREATE DATABASE SalesPostingDB;
GO

USE SalesPostingDB;
GO

-- Main Sales Payload Table
CREATE TABLE SalesPayload (
    Id BIGINT IDENTITY(1,1) PRIMARY KEY,
    TransactionId NVARCHAR(100) NOT NULL,
    TerminalId NVARCHAR(50) NOT NULL,
    PayloadJson NVARCHAR(MAX) NOT NULL,
    Status NVARCHAR(20) NOT NULL DEFAULT 'Pending',
    CreatedDate DATETIME2(7) NOT NULL DEFAULT GETUTCDATE(),
    ProcessedDate DATETIME2(7) NULL,
    ErrorMessage NVARCHAR(MAX) NULL,
    RetryCount INT NOT NULL DEFAULT 0,
    
    -- Constraints
    CONSTRAINT UQ_SalesPayload_TransactionId UNIQUE (TransactionId)
);
GO

-- Error Logging Table
CREATE TABLE ProcessingError (
    Id BIGINT IDENTITY(1,1) PRIMARY KEY,
    SalesPayloadId BIGINT NOT NULL,
    ErrorMessage NVARCHAR(MAX) NULL,
    StackTrace NVARCHAR(MAX) NULL,
    OccurredAt DATETIME2(7) NOT NULL DEFAULT GETUTCDATE(),
    
    CONSTRAINT FK_ProcessingError_SalesPayload 
        FOREIGN KEY (SalesPayloadId) REFERENCES SalesPayload(Id)
);
GO

-- Performance Indexes
CREATE NONCLUSTERED INDEX IX_SalesPayload_Status_CreatedDate 
    ON SalesPayload(Status, CreatedDate)
    INCLUDE (Id, TerminalId, TransactionId);
GO