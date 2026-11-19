using System.Data.Common;
using VisaUchet.Application;
using VisaUchet.Domain;

namespace VisaUchet.Infrastructure;

// Applications, history and journal in SQL Server; only parameterized queries (SEC-03)
public sealed class SqlApplicationRepository : IApplicationRepository
{
    private const string ApplicationColumns = """
        a.Application_Number, a.Registration_Date, a.Visa_Type, a.Category, a.Multiplicity, a.Period_Start,
        a.Period_End, a.Stay_Days, a.Is_Urgent, a.Control_Date, p.Applicant_Id, p.Last_Name, p.First_Name,
        p.Birth_Date, p.Sex, p.Citizenship, p.Passport_Number, p.Passport_Expiry
        FROM Visa_Application a JOIN Applicant p ON p.Applicant_Id = a.Applicant_Id
        """;

    private const string VisaColumns = """
        Visa_Number, Application_Number, Issue_Date, Holder_Name, Holder_Citizenship, Visa_Type, Category,
        Multiplicity, Valid_From, Valid_To, Stay_Days, Fee, Currency, State, Cancel_Date, Cancel_Reason, Issued_By
        """;

    private readonly DbConnectionFactory _connections;

    public SqlApplicationRepository(DbConnectionFactory connections) =>
        _connections = connections ?? throw new ArgumentNullException(nameof(connections));

    public VisaApplication? FindByNumber(string number)
    {
        using var connection = _connections.Open();
        var application = ReadApplications(
            Db.Command(connection, $"SELECT {ApplicationColumns} WHERE a.Application_Number = @number").With("@number", number))
            .SingleOrDefault();
        application?.LoadHistory(ReadHistory(connection, "WHERE h.Application_Number = @number", ("@number", number)));
        return application;
    }

    public IReadOnlyList<VisaApplication> FindRegistered(DateOnly from, DateOnly to)
    {
        const string Filter = "CAST(a.Registration_Date AS DATE) BETWEEN @from AND @to";
        using var connection = _connections.Open();
        var applications = ReadApplications(
            Db.Command(connection, $"SELECT {ApplicationColumns} WHERE {Filter}").With("@from", from).With("@to", to));
        var history = ReadHistory(
            connection,
            $"JOIN Visa_Application a ON a.Application_Number = h.Application_Number WHERE {Filter}",
            ("@from", from),
            ("@to", to)).ToLookup(h => h.ApplicationNumber);
        foreach (var application in applications)
        {
            application.LoadHistory(history[application.Number]);
        }

        return applications;
    }

    public IReadOnlyList<ApplicationSummary> Search(string text, int limit)
    {
        const string Sql = """
            SELECT TOP (@limit) a.Application_Number, a.Registration_Date, p.Last_Name, p.First_Name,
                   p.Passport_Number, a.Status_Code, a.Control_Date
            FROM Visa_Application a JOIN Applicant p ON p.Applicant_Id = a.Applicant_Id
            WHERE a.Application_Number = @text OR p.Passport_Number = @text OR p.Last_Name LIKE @prefix
            ORDER BY a.Registration_Date DESC
            """;
        using var connection = _connections.Open();
        using var command = Db.Command(connection, Sql)
            .With("@limit", limit).With("@text", text).With("@prefix", Db.LikePrefix(text));
        using var reader = command.ExecuteReader();
        var result = new List<ApplicationSummary>();
        while (reader.Read())
        {
            result.Add(new ApplicationSummary(
                reader.Text("Application_Number"),
                reader.Get<DateTime>("Registration_Date"),
                $"{reader.Text("Last_Name")} {reader.Text("First_Name")}",
                reader.Text("Passport_Number"),
                Codes.ToStatus(reader.Text("Status_Code")),
                reader.GetDate("Control_Date")));
        }

        return result;
    }

    public Applicant? FindApplicant(string lastName, string firstName, DateOnly birthDate, string citizenship)
    {
        const string Sql = """
            SELECT TOP (1) Applicant_Id, Last_Name, First_Name, Birth_Date, Sex, Citizenship, Passport_Number, Passport_Expiry
            FROM Applicant
            WHERE Last_Name = @last AND First_Name = @first AND Birth_Date = @birth AND Citizenship = @citizenship
            ORDER BY Applicant_Id DESC
            """;
        using var connection = _connections.Open();
        using var command = Db.Command(connection, Sql)
            .With("@last", lastName.Trim()).With("@first", firstName.Trim())
            .With("@birth", birthDate).With("@citizenship", citizenship);
        using var reader = command.ExecuteReader();
        return reader.Read() ? ReadApplicant(reader) : null;
    }

    public int CountApplications(int applicantId) =>
        Scalar("SELECT COUNT(*) FROM Visa_Application WHERE Applicant_Id = @id", ("@id", applicantId));

    public int LastSequenceNumber(int year) => Scalar(
        "SELECT ISNULL(MAX(CAST(RIGHT(Application_Number, 6) AS INT)), 0) FROM Visa_Application WHERE Application_Number LIKE @prefix",
        ("@prefix", $"{DomainConstants.ApplicationNumberPrefix}-{year}-%"));

    public bool VisaNumberExists(string visaNumber) =>
        Scalar("SELECT COUNT(*) FROM Issued_Visa WHERE Visa_Number = @number", ("@number", visaNumber)) > 0;

    public void Add(VisaApplication application)
    {
        ArgumentNullException.ThrowIfNull(application);
        using var connection = _connections.Open();
        using var transaction = connection.BeginTransaction();
        SaveApplicant(connection, transaction, application.Applicant);
        InsertApplication(connection, transaction, application);
        foreach (var record in application.History)
        {
            InsertHistory(connection, transaction, record);
        }

        transaction.Commit();
    }

    public void SaveStatusChange(StatusChange change)
    {
        ArgumentNullException.ThrowIfNull(change);
        using var connection = _connections.Open();
        using var transaction = connection.BeginTransaction();
        InsertHistory(connection, transaction, change.History);
        UpdateStatus(connection, transaction, change.History);
        if (change.NewVisa is not null)
        {
            InsertVisa(connection, transaction, change.NewVisa);
        }

        if (change.CancelledVisa is not null)
        {
            CancelVisa(connection, transaction, change.CancelledVisa);
        }

        transaction.Commit();
    }

    public IssuedVisa? FindVisaByApplication(string applicationNumber) =>
        ReadVisas($"SELECT {VisaColumns} FROM Issued_Visa WHERE Application_Number = @number", ("@number", applicationNumber))
            .SingleOrDefault();

    public IReadOnlyList<IssuedVisa> FindIssued(DateOnly from, DateOnly to) => ReadVisas(
        $"SELECT {VisaColumns} FROM Issued_Visa WHERE Issue_Date BETWEEN @from AND @to ORDER BY Issue_Date, Visa_Number",
        ("@from", from),
        ("@to", to));

    public RetentionResult PurgeExpired(DateTime applicationCutoff, DateOnly journalCutoff)
    {
        const string DeleteApplications = """
            DELETE a FROM Visa_Application a
            WHERE a.Status_Code NOT IN (@s1, @s2, @s3, @s4)
              AND (SELECT MAX(h.Changed_At) FROM Status_History h WHERE h.Application_Number = a.Application_Number) < @cutoff;
            DELETE p FROM Applicant p WHERE NOT EXISTS (SELECT 1 FROM Visa_Application a WHERE a.Applicant_Id = p.Applicant_Id);
            """;
        using var connection = _connections.Open();
        using var transaction = connection.BeginTransaction();
        using var applications = Db.Command(connection, DeleteApplications, transaction).With("@cutoff", applicationCutoff)
            .With("@s1", Codes.Of(ApplicationStatus.Registered)).With("@s2", Codes.Of(ApplicationStatus.InReview))
            .With("@s3", Codes.Of(ApplicationStatus.Interview)).With("@s4", Codes.Of(ApplicationStatus.OnApproval));
        var deletedApplications = applications.ExecuteNonQuery();
        using var journal = Db.Command(connection, "DELETE FROM Issued_Visa WHERE Issue_Date < @cutoff", transaction)
            .With("@cutoff", journalCutoff);
        var deletedVisas = journal.ExecuteNonQuery();
        transaction.Commit();
        return new RetentionResult(deletedApplications, deletedVisas);
    }

    private int Scalar(string sql, params (string Name, object Value)[] parameters)
    {
        using var connection = _connections.Open();
        using var command = Db.Command(connection, sql);
        foreach (var (name, value) in parameters)
        {
            command.With(name, value);
        }

        return Convert.ToInt32(command.ExecuteScalar(), System.Globalization.CultureInfo.InvariantCulture);
    }

    private static List<VisaApplication> ReadApplications(DbCommand command)
    {
        using (command)
        using (var reader = command.ExecuteReader())
        {
            var result = new List<VisaApplication>();
            while (reader.Read())
            {
                result.Add(MapApplication(reader));
            }

            return result;
        }
    }

    private static VisaApplication MapApplication(DbDataReader reader) => new()
    {
        Number = reader.Text("Application_Number"),
        RegistrationDate = reader.Get<DateTime>("Registration_Date"),
        Applicant = ReadApplicant(reader),
        Requested = new VisaParameters(
            Codes.ToVisaType(reader.Text("Visa_Type")),
            reader.Text("Category"),
            Codes.ToMultiplicity(reader.Text("Multiplicity")),
            reader.GetDate("Period_Start"),
            reader.GetDate("Period_End"),
            reader.Get<int>("Stay_Days")),
        IsUrgent = reader.Get<bool>("Is_Urgent"),
        ControlDate = reader.GetDate("Control_Date"),
    };

    private static Applicant ReadApplicant(DbDataReader reader) => new()
    {
        Id = reader.Get<int>("Applicant_Id"),
        LastName = reader.Text("Last_Name"),
        FirstName = reader.Text("First_Name"),
        BirthDate = reader.GetDate("Birth_Date"),
        Sex = Codes.ToSex(reader.Text("Sex")),
        Citizenship = reader.Text("Citizenship"),
        PassportNumber = reader.Text("Passport_Number"),
        PassportExpiry = reader.GetDate("Passport_Expiry"),
    };

    private static List<StatusHistory> ReadHistory(
        DbConnection connection, string condition, params (string Name, object Value)[] parameters)
    {
        var sql = "SELECT h.History_Id, h.Application_Number, h.Old_Status, h.New_Status, h.Changed_At, h.Employee_Login, h.Comment " +
                  $"FROM Status_History h {condition}";
        using var command = Db.Command(connection, sql);
        foreach (var (name, value) in parameters)
        {
            command.With(name, value);
        }

        using var reader = command.ExecuteReader();
        var result = new List<StatusHistory>();
        while (reader.Read())
        {
            result.Add(MapHistory(reader));
        }

        return result;
    }

    private static StatusHistory MapHistory(DbDataReader reader)
    {
        var old = reader.GetNullableString("Old_Status");
        return new StatusHistory
        {
            Id = reader.Get<int>("History_Id"),
            ApplicationNumber = reader.Text("Application_Number"),
            OldStatus = old is null ? null : Codes.ToStatus(old),
            NewStatus = Codes.ToStatus(reader.Text("New_Status")),
            ChangedAt = reader.Get<DateTime>("Changed_At"),
            EmployeeLogin = reader.Text("Employee_Login"),
            Comment = reader.GetNullableString("Comment"),
        };
    }

    private List<IssuedVisa> ReadVisas(string sql, params (string Name, object Value)[] parameters)
    {
        using var connection = _connections.Open();
        using var command = Db.Command(connection, sql);
        foreach (var (name, value) in parameters)
        {
            command.With(name, value);
        }

        using var reader = command.ExecuteReader();
        var result = new List<IssuedVisa>();
        while (reader.Read())
        {
            result.Add(MapVisa(reader));
        }

        return result;
    }

    private static IssuedVisa MapVisa(DbDataReader reader)
    {
        var visa = new IssuedVisa
        {
            VisaNumber = reader.Text("Visa_Number"),
            ApplicationNumber = reader.GetNullableString("Application_Number")?.Trim(),
            IssueDate = reader.GetDate("Issue_Date"),
            HolderName = reader.Text("Holder_Name"),
            HolderCitizenship = reader.Text("Holder_Citizenship"),
            Parameters = new VisaParameters(
                Codes.ToVisaType(reader.Text("Visa_Type")),
                reader.Text("Category"),
                Codes.ToMultiplicity(reader.Text("Multiplicity")),
                reader.GetDate("Valid_From"),
                reader.GetDate("Valid_To"),
                reader.Get<int>("Stay_Days")),
            Fee = reader.Get<decimal>("Fee"),
            Currency = reader.Text("Currency"),
            IssuedBy = reader.Text("Issued_By"),
        };
        if (Codes.ToState(reader.Text("State")) == VisaState.Cancelled)
        {
            visa.LoadCancellation(reader.GetNullableDate("Cancel_Date") ?? visa.IssueDate, reader.GetNullableString("Cancel_Reason") ?? string.Empty);
        }

        return visa;
    }

    private static void SaveApplicant(DbConnection connection, DbTransaction transaction, Applicant applicant)
    {
        if (applicant.Id != 0)
        {
            using var update = Db.Command(connection, "UPDATE Applicant SET Passport_Number = @number, Passport_Expiry = @expiry WHERE Applicant_Id = @id", transaction)
                .With("@number", applicant.PassportNumber).With("@expiry", applicant.PassportExpiry).With("@id", applicant.Id);
            update.ExecuteNonQuery();
            return;
        }

        const string Insert = """
            INSERT INTO Applicant (Last_Name, First_Name, Birth_Date, Sex, Citizenship, Passport_Number, Passport_Expiry)
            VALUES (@last, @first, @birth, @sex, @citizenship, @number, @expiry);
            SELECT CAST(SCOPE_IDENTITY() AS INT);
            """;
        using var command = Db.Command(connection, Insert, transaction)
            .With("@last", applicant.LastName).With("@first", applicant.FirstName).With("@birth", applicant.BirthDate)
            .With("@sex", Codes.Of(applicant.Sex)).With("@citizenship", applicant.Citizenship)
            .With("@number", applicant.PassportNumber).With("@expiry", applicant.PassportExpiry);
        applicant.Id = Convert.ToInt32(command.ExecuteScalar(), System.Globalization.CultureInfo.InvariantCulture);
    }

    private static void InsertApplication(DbConnection connection, DbTransaction transaction, VisaApplication application)
    {
        const string Sql = """
            INSERT INTO Visa_Application (Application_Number, Applicant_Id, Registration_Date, Visa_Type, Category, Multiplicity,
                Period_Start, Period_End, Stay_Days, Is_Urgent, Status_Code, Control_Date)
            VALUES (@number, @applicant, @registered, @type, @category, @multiplicity, @start, @end, @stay, @urgent, @status, @control)
            """;
        var visa = application.Requested;
        using var command = Db.Command(connection, Sql, transaction)
            .With("@number", application.Number).With("@applicant", application.Applicant.Id)
            .With("@registered", application.RegistrationDate).With("@type", Codes.Of(visa.Type))
            .With("@category", visa.Category).With("@multiplicity", Codes.Of(visa.Multiplicity))
            .With("@start", visa.ValidFrom).With("@end", visa.ValidTo).With("@stay", visa.StayDays)
            .With("@urgent", application.IsUrgent).With("@status", Codes.Of(application.Status))
            .With("@control", application.ControlDate);
        command.ExecuteNonQuery();
    }

    private static void InsertHistory(DbConnection connection, DbTransaction transaction, StatusHistory record)
    {
        const string Sql = """
            INSERT INTO Status_History (Application_Number, Old_Status, New_Status, Changed_At, Employee_Login, Comment)
            VALUES (@number, @old, @new, @at, @login, @comment)
            """;
        using var command = Db.Command(connection, Sql, transaction)
            .With("@number", record.ApplicationNumber)
            .With("@old", record.OldStatus is { } old ? Codes.Of(old) : null, System.Data.DbType.String)
            .With("@new", Codes.Of(record.NewStatus)).With("@at", record.ChangedAt)
            .With("@login", record.EmployeeLogin).With("@comment", record.Comment, System.Data.DbType.String);
        command.ExecuteNonQuery();
    }

    // Optimistic check: the status must still be the one read before the change
    private static void UpdateStatus(DbConnection connection, DbTransaction transaction, StatusHistory record)
    {
        using var command = Db.Command(
                connection,
                "UPDATE Visa_Application SET Status_Code = @new WHERE Application_Number = @number AND Status_Code = @old",
                transaction)
            .With("@new", Codes.Of(record.NewStatus)).With("@number", record.ApplicationNumber)
            .With("@old", record.OldStatus is { } old ? Codes.Of(old) : null, System.Data.DbType.String);
        if (command.ExecuteNonQuery() != 1)
        {
            throw new DomainException("Статус заявки был изменён другим пользователем. Обновите карточку.");
        }
    }

    private static void InsertVisa(DbConnection connection, DbTransaction transaction, IssuedVisa visa)
    {
        var sql = $"INSERT INTO Issued_Visa ({VisaColumns}) VALUES (@number, @application, @issued, @holder, @citizenship, @type, " +
                  "@category, @multiplicity, @from, @to, @stay, @fee, @currency, @state, NULL, NULL, @by)";
        var p = visa.Parameters;
        using var command = Db.Command(connection, sql, transaction)
            .With("@number", visa.VisaNumber).With("@application", visa.ApplicationNumber, System.Data.DbType.String)
            .With("@issued", visa.IssueDate).With("@holder", visa.HolderName).With("@citizenship", visa.HolderCitizenship)
            .With("@type", Codes.Of(p.Type)).With("@category", p.Category).With("@multiplicity", Codes.Of(p.Multiplicity))
            .With("@from", p.ValidFrom).With("@to", p.ValidTo).With("@stay", p.StayDays).With("@fee", visa.Fee)
            .With("@currency", visa.Currency).With("@state", Codes.Of(visa.State)).With("@by", visa.IssuedBy);
        command.ExecuteNonQuery();
    }

    private static void CancelVisa(DbConnection connection, DbTransaction transaction, IssuedVisa visa)
    {
        using var command = Db.Command(
                connection,
                "UPDATE Issued_Visa SET State = @state, Cancel_Date = @date, Cancel_Reason = @reason WHERE Visa_Number = @number",
                transaction)
            .With("@state", Codes.Of(visa.State)).With("@date", visa.CancelDate, System.Data.DbType.Date)
            .With("@reason", visa.CancelReason, System.Data.DbType.String).With("@number", visa.VisaNumber);
        command.ExecuteNonQuery();
    }
}
