-- ИС «ВизаУчет»: справочники, учётные записи и демонстрационные данные
-- Запуск после 01_create_schema.sql: sqlcmd -S .\SQLEXPRESS -E -f 65001 -i 02_seed.sql
-- Пароли демонстрационных учётных записей указаны в README.md; смените их после первого входа

USE VisaUchet;
GO

INSERT INTO Application_Status (Status_Code, Name, Is_Final) VALUES
    ('REGISTERED',  N'Зарегистрирована',  0),
    ('IN_REVIEW',   N'На рассмотрении',   0),
    ('INTERVIEW',   N'Собеседование',     0),
    ('ON_APPROVAL', N'Согласование',      0),
    ('ISSUED',      N'Виза выдана',       0),
    ('RETURNED',    N'Возвращена',        1),
    ('REFUSED',     N'Отказано',          1),
    ('WITHDRAWN',   N'Отозвана',          1),
    ('CANCELLED',   N'Виза аннулирована', 1);

INSERT INTO Employee (Login, Full_Name, Role_Code, Password_Hash) VALUES
    ('admin',    N'Администратор системы',  'ADMIN',          'PBKDF2$100000$1iYv+An0HogIiGTPazie6A==$RgnjHGTcKYYxfWxH20pzxBNRNeq2yB7VB5nVpMEEu9k='),
    ('operator', N'Петрова Мария Ивановна', 'OPERATOR',       'PBKDF2$100000$V2m1hxcHc0Pygd8rWFGDJQ==$hfb0m5XmkQchrcY/Iu6xPs/ermwF9e/DkmUsCdKtixk='),
    ('consul',   N'Сидоров Олег Петрович',  'CONSUL_OFFICER', 'PBKDF2$100000$/gCk9MOvM76uhiNMrBnvdA==$LRKQBaoawR+Vx5v2jzy4msbknIwwbS7sziouuEEHVnw='),
    ('head',     N'Козлова Ирина Сергеевна','HEAD',           'PBKDF2$100000$VIaOvMjIQS3BXh+lwot+TA==$9iqa7iFiCzb0vdvenzn4gpWZh9LMULX5RvUozlUDwu4=');

-- Государственные праздники Республики Беларусь (нерабочие дни)
INSERT INTO Holiday (Holiday_Date, Name) VALUES
    ('2026-01-01', N'Новый год'), ('2026-01-02', N'Новый год'),
    ('2026-01-07', N'Рождество Христово (православное)'), ('2026-03-08', N'День женщин'),
    ('2026-04-21', N'Радуница'), ('2026-05-01', N'Праздник труда'), ('2026-05-09', N'День Победы'),
    ('2026-07-03', N'День Независимости'), ('2026-11-07', N'День Октябрьской революции'),
    ('2026-12-25', N'Рождество Христово (католическое)'),
    ('2027-01-01', N'Новый год'), ('2027-01-02', N'Новый год'),
    ('2027-01-07', N'Рождество Христово (православное)'), ('2027-03-08', N'День женщин'),
    ('2027-05-01', N'Праздник труда'), ('2027-05-09', N'День Победы'), ('2027-05-11', N'Радуница'),
    ('2027-07-03', N'День Независимости'), ('2027-11-07', N'День Октябрьской революции'),
    ('2027-12-25', N'Рождество Христово (католическое)');

-- Демонстрационные заявки (ноябрь 2026)
SET IDENTITY_INSERT Applicant ON;
INSERT INTO Applicant (Applicant_Id, Last_Name, First_Name, Birth_Date, Sex, Citizenship, Passport_Number, Passport_Expiry) VALUES
    (1, 'SMITH',     'JOHN',  '1985-04-12', 'M', 'USA', 'A12345678', '2030-06-30'),
    (2, 'KOWALSKI',  'PIOTR', '1979-09-03', 'M', 'POL', 'EA4561237', '2029-02-14'),
    (3, 'MULLER',    'ANNA',  '1992-01-25', 'F', 'DEU', 'C01X00T47', '2031-08-01'),
    (4, 'TANAKA',    'YUKI',  '1998-11-30', 'F', 'JPN', 'TK7788990', '2032-03-15'),
    (5, 'PETRAITIS', 'JONAS', '1988-07-07', 'M', 'LTU', '23456789',  '2030-12-31'),
    (6, 'ROSSI',     'MARCO', '1975-02-18', 'M', 'ITA', 'YA1234567', '2029-10-10');
SET IDENTITY_INSERT Applicant OFF;

INSERT INTO Visa_Application (Application_Number, Applicant_Id, Registration_Date, Visa_Type, Category, Multiplicity,
    Period_Start, Period_End, Stay_Days, Is_Urgent, Status_Code, Control_Date) VALUES
    ('VZ-2026-000001', 1, '2026-10-05 10:00', 'C', N'турызм',                         'SINGLE',   '2026-11-01', '2026-11-20', 14, 0, 'ISSUED',    '2026-10-12'),
    ('VZ-2026-000002', 2, '2026-10-14 11:00', 'C', N'дзелавыя стасункі',              'DOUBLE',   '2026-11-10', '2026-12-09', 20, 1, 'ISSUED',    '2026-10-16'),
    ('VZ-2026-000003', 3, '2026-10-20 12:30', 'C', N'прыватная',                      'SINGLE',   '2026-12-01', '2026-12-15', 10, 0, 'REFUSED',   '2026-10-27'),
    ('VZ-2026-000004', 4, '2026-11-03 09:15', 'C', N'спорт/культура/навука/адукацыя', 'SINGLE',   '2026-12-05', '2026-12-25', 21, 0, 'CANCELLED', '2026-11-10'),
    ('VZ-2026-000005', 5, '2026-11-09 14:00', 'D', N'праца',                          'MULTIPLE', '2026-12-01', '2027-11-30', 90, 0, 'INTERVIEW', '2026-11-16'),
    ('VZ-2026-000006', 6, '2026-11-02 16:40', 'C', N'турызм',                         'SINGLE',   '2026-11-20', '2026-12-05', 10, 0, 'IN_REVIEW', '2026-11-09'),
    ('VZ-2026-000007', 1, '2026-11-13 10:20', 'C', N'турызм',                         'SINGLE',   '2026-12-20', '2027-01-05', 14, 0, 'REGISTERED','2026-11-20');

INSERT INTO Status_History (Application_Number, Old_Status, New_Status, Changed_At, Employee_Login, Comment) VALUES
    ('VZ-2026-000001', NULL,        'REGISTERED', '2026-10-05 10:00', 'operator', NULL),
    ('VZ-2026-000001', 'REGISTERED','IN_REVIEW',  '2026-10-06 09:30', 'consul',   NULL),
    ('VZ-2026-000001', 'IN_REVIEW', 'ISSUED',     '2026-10-09 11:00', 'consul',   NULL),
    ('VZ-2026-000002', NULL,        'REGISTERED', '2026-10-14 11:00', 'operator', NULL),
    ('VZ-2026-000002', 'REGISTERED','IN_REVIEW',  '2026-10-15 10:00', 'consul',   NULL),
    ('VZ-2026-000002', 'IN_REVIEW', 'ISSUED',     '2026-10-16 15:00', 'consul',   N'Кратность сокращена до однократной'),
    ('VZ-2026-000003', NULL,        'REGISTERED', '2026-10-20 12:30', 'operator', NULL),
    ('VZ-2026-000003', 'REGISTERED','IN_REVIEW',  '2026-10-21 09:00', 'consul',   NULL),
    ('VZ-2026-000003', 'IN_REVIEW', 'REFUSED',    '2026-10-29 16:00', 'consul',   NULL),
    ('VZ-2026-000004', NULL,        'REGISTERED', '2026-11-03 09:15', 'operator', NULL),
    ('VZ-2026-000004', 'REGISTERED','IN_REVIEW',  '2026-11-04 11:00', 'consul',   NULL),
    ('VZ-2026-000004', 'IN_REVIEW', 'ISSUED',     '2026-11-06 14:00', 'consul',   NULL),
    ('VZ-2026-000004', 'ISSUED',    'CANCELLED',  '2026-11-12 10:00', 'consul',   N'Отзыв ходатайства приглашающей организацией'),
    ('VZ-2026-000005', NULL,        'REGISTERED', '2026-11-09 14:00', 'operator', NULL),
    ('VZ-2026-000005', 'REGISTERED','IN_REVIEW',  '2026-11-10 09:00', 'consul',   NULL),
    ('VZ-2026-000005', 'IN_REVIEW', 'INTERVIEW',  '2026-11-11 15:00', 'consul',   N'Собеседование 17.11.2026 в 10:00, каб. 12'),
    ('VZ-2026-000006', NULL,        'REGISTERED', '2026-11-02 16:40', 'operator', NULL),
    ('VZ-2026-000006', 'REGISTERED','IN_REVIEW',  '2026-11-03 09:10', 'consul',   NULL),
    ('VZ-2026-000007', NULL,        'REGISTERED', '2026-11-13 10:20', 'operator', NULL);

INSERT INTO Issued_Visa (Visa_Number, Application_Number, Issue_Date, Holder_Name, Holder_Citizenship, Visa_Type, Category,
    Multiplicity, Valid_From, Valid_To, Stay_Days, Fee, Currency, State, Cancel_Date, Cancel_Reason, Issued_By) VALUES
    ('BLR0000101', 'VZ-2026-000001', '2026-10-09', 'SMITH JOHN',     'USA', 'C', N'турызм',                         'SINGLE', '2026-11-01', '2026-11-20', 14, 60.00, 'EUR', 'VALID',     NULL, NULL, 'consul'),
    ('BLR0000102', 'VZ-2026-000002', '2026-10-16', 'KOWALSKI PIOTR', 'POL', 'C', N'дзелавыя стасункі',              'SINGLE', '2026-11-10', '2026-12-09', 15, 70.00, 'EUR', 'VALID',     NULL, NULL, 'consul'),
    ('BLR0000103', 'VZ-2026-000004', '2026-11-06', 'TANAKA YUKI',    'JPN', 'C', N'спорт/культура/навука/адукацыя', 'SINGLE', '2026-12-05', '2026-12-25', 21,  0.00, 'EUR', 'CANCELLED', '2026-11-12', N'Отзыв ходатайства приглашающей организацией', 'consul');
GO
