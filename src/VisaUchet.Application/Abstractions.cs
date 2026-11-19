using VisaUchet.Domain;

namespace VisaUchet.Application;

// Current time source; replaced by a fixed clock in tests
public interface IClock
{
    DateTime Now { get; }

    DateOnly Today => DateOnly.FromDateTime(Now);
}

// Application log; must never receive passwords or applicants' personal data (SEC-04)
public interface IAppLog
{
    void Info(string message);

    void Error(string message, Exception exception);
}

// Storage of applications, history and the journal of issued visas (SWI-01)
public interface IApplicationRepository
{
    VisaApplication? FindByNumber(string number);

    IReadOnlyList<ApplicationSummary> Search(string text, int limit);

    Applicant? FindApplicant(string lastName, string firstName, DateOnly birthDate, string citizenship);

    int CountApplications(int applicantId);

    int LastSequenceNumber(int year);

    // Saves applicant, application and the first history record in one transaction
    void Add(VisaApplication application);

    // Saves history, current status and journal changes in one transaction (STS-05)
    void SaveStatusChange(StatusChange change);

    bool VisaNumberExists(string visaNumber);

    IssuedVisa? FindVisaByApplication(string applicationNumber);

    IReadOnlyList<IssuedVisa> FindIssued(DateOnly from, DateOnly to);

    IReadOnlyList<VisaApplication> FindRegistered(DateOnly from, DateOnly to);

    RetentionResult PurgeExpired(DateTime applicationCutoff, DateOnly journalCutoff);
}

public interface IEmployeeRepository
{
    Employee? FindByLogin(string login);

    IReadOnlyList<Employee> All();

    void Add(Employee employee);

    void Update(Employee employee);
}

// Holidays used to count working days (set by the administrator)
public interface IHolidayRepository
{
    IReadOnlyList<DateOnly> All();
}
