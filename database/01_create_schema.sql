-- ИС «ВизаУчет»: схема базы данных (Microsoft SQL Server 2022 Express)
-- Запуск: sqlcmd -S .\SQLEXPRESS -E -f 65001 -i 01_create_schema.sql

IF DB_ID(N'VisaUchet') IS NULL
    CREATE DATABASE VisaUchet;
GO

USE VisaUchet;
GO

-- Справочник статусов заявки (SRS, таблица 3.2)
CREATE TABLE Application_Status (
    Status_Code VARCHAR(20)  NOT NULL CONSTRAINT PK_Application_Status PRIMARY KEY,
    Name        NVARCHAR(50) NOT NULL,
    Is_Final    BIT          NOT NULL
);

-- Сотрудники (AUT-01, SEC-01, SEC-02)
CREATE TABLE Employee (
    Login           VARCHAR(30)   NOT NULL CONSTRAINT PK_Employee PRIMARY KEY,
    Full_Name       NVARCHAR(100) NOT NULL,
    Role_Code       VARCHAR(20)   NOT NULL
        CONSTRAINT CK_Employee_Role CHECK (Role_Code IN ('OPERATOR', 'CONSUL_OFFICER', 'HEAD', 'ADMIN')),
    Password_Hash   VARCHAR(200)  NOT NULL,
    Is_Blocked      BIT           NOT NULL CONSTRAINT DF_Employee_Blocked DEFAULT 0,
    Failed_Attempts INT           NOT NULL CONSTRAINT DF_Employee_Failed DEFAULT 0,
    Locked_Until    DATETIME2(0)  NULL
);

-- Нерабочие праздничные дни для расчёта контрольного срока (REG-05)
CREATE TABLE Holiday (
    Holiday_Date DATE          NOT NULL CONSTRAINT PK_Holiday PRIMARY KEY,
    Name         NVARCHAR(100) NOT NULL
);

-- Заявитель
CREATE TABLE Applicant (
    Applicant_Id    INT IDENTITY(1, 1) NOT NULL CONSTRAINT PK_Applicant PRIMARY KEY,
    Last_Name       VARCHAR(40) NOT NULL,
    First_Name      VARCHAR(40) NOT NULL,
    Birth_Date      DATE        NOT NULL,
    Sex             CHAR(1)     NOT NULL CONSTRAINT CK_Applicant_Sex CHECK (Sex IN ('M', 'F')),
    Citizenship     CHAR(3)     NOT NULL,
    Passport_Number VARCHAR(9)  NOT NULL,
    Passport_Expiry DATE        NOT NULL
);
CREATE INDEX IX_Applicant_Identity ON Applicant (Last_Name, First_Name, Birth_Date, Citizenship);
CREATE INDEX IX_Applicant_Passport ON Applicant (Passport_Number);

-- Визовая заявка
CREATE TABLE Visa_Application (
    Application_Number VARCHAR(20)  NOT NULL CONSTRAINT PK_Visa_Application PRIMARY KEY,
    Applicant_Id       INT          NOT NULL
        CONSTRAINT FK_Visa_Application_Applicant REFERENCES Applicant (Applicant_Id),
    Registration_Date  DATETIME2(0) NOT NULL,
    Visa_Type          CHAR(1)      NOT NULL CONSTRAINT CK_Visa_Application_Type CHECK (Visa_Type IN ('B', 'C', 'D')),
    Category           NVARCHAR(40) NOT NULL,
    Multiplicity       VARCHAR(10)  NOT NULL
        CONSTRAINT CK_Visa_Application_Mult CHECK (Multiplicity IN ('SINGLE', 'DOUBLE', 'MULTIPLE')),
    Period_Start       DATE         NOT NULL,
    Period_End         DATE         NOT NULL,
    Stay_Days          INT          NOT NULL CONSTRAINT CK_Visa_Application_Stay CHECK (Stay_Days > 0),
    Is_Urgent          BIT          NOT NULL,
    Status_Code        VARCHAR(20)  NOT NULL
        CONSTRAINT FK_Visa_Application_Status REFERENCES Application_Status (Status_Code),
    Control_Date       DATE         NOT NULL,
    CONSTRAINT CK_Visa_Application_Period CHECK (Period_End > Period_Start)
);
CREATE INDEX IX_Visa_Application_Registration ON Visa_Application (Registration_Date);
CREATE INDEX IX_Visa_Application_Applicant ON Visa_Application (Applicant_Id);

-- История статусов: неизменяемые записи (STS-04)
CREATE TABLE Status_History (
    History_Id         INT IDENTITY(1, 1) NOT NULL CONSTRAINT PK_Status_History PRIMARY KEY,
    Application_Number VARCHAR(20)   NOT NULL
        CONSTRAINT FK_Status_History_Application REFERENCES Visa_Application (Application_Number) ON DELETE CASCADE,
    Old_Status         VARCHAR(20)   NULL CONSTRAINT FK_Status_History_Old REFERENCES Application_Status (Status_Code),
    New_Status         VARCHAR(20)   NOT NULL CONSTRAINT FK_Status_History_New REFERENCES Application_Status (Status_Code),
    Changed_At         DATETIME2(0)  NOT NULL,
    Employee_Login     VARCHAR(30)   NOT NULL CONSTRAINT FK_Status_History_Employee REFERENCES Employee (Login),
    Comment            NVARCHAR(500) NULL
);
CREATE INDEX IX_Status_History_Application ON Status_History (Application_Number, Changed_At);

-- Журнал выданных виз; запись переживает удаление заявки (JRN-05)
CREATE TABLE Issued_Visa (
    Visa_Number        CHAR(10)      NOT NULL CONSTRAINT PK_Issued_Visa PRIMARY KEY,
    Application_Number VARCHAR(20)   NULL
        CONSTRAINT FK_Issued_Visa_Application REFERENCES Visa_Application (Application_Number) ON DELETE SET NULL,
    Issue_Date         DATE          NOT NULL,
    Holder_Name        VARCHAR(81)   NOT NULL,
    Holder_Citizenship CHAR(3)       NOT NULL,
    Visa_Type          CHAR(1)       NOT NULL CONSTRAINT CK_Issued_Visa_Type CHECK (Visa_Type IN ('B', 'C', 'D')),
    Category           NVARCHAR(40)  NOT NULL,
    Multiplicity       VARCHAR(10)   NOT NULL
        CONSTRAINT CK_Issued_Visa_Mult CHECK (Multiplicity IN ('SINGLE', 'DOUBLE', 'MULTIPLE')),
    Valid_From         DATE          NOT NULL,
    Valid_To           DATE          NOT NULL,
    Stay_Days          INT           NOT NULL CONSTRAINT CK_Issued_Visa_Stay CHECK (Stay_Days > 0),
    Fee                DECIMAL(6, 2) NOT NULL CONSTRAINT CK_Issued_Visa_Fee CHECK (Fee BETWEEN 0 AND 999.99),
    Currency           CHAR(3)       NOT NULL CONSTRAINT CK_Issued_Visa_Currency CHECK (Currency IN ('EUR', 'USD', 'BYN')),
    State              VARCHAR(10)   NOT NULL CONSTRAINT CK_Issued_Visa_State CHECK (State IN ('VALID', 'CANCELLED')),
    Cancel_Date        DATE          NULL,
    Cancel_Reason      NVARCHAR(500) NULL,
    Issued_By          VARCHAR(30)   NOT NULL CONSTRAINT FK_Issued_Visa_Employee REFERENCES Employee (Login),
    CONSTRAINT CK_Issued_Visa_Number CHECK (Visa_Number LIKE 'BLR[0-9][0-9][0-9][0-9][0-9][0-9][0-9]'),
    CONSTRAINT CK_Issued_Visa_Period CHECK (Valid_To >= Valid_From)
);
CREATE UNIQUE INDEX UX_Issued_Visa_Application ON Issued_Visa (Application_Number) WHERE Application_Number IS NOT NULL;
CREATE INDEX IX_Issued_Visa_Issue_Date ON Issued_Visa (Issue_Date);
GO
