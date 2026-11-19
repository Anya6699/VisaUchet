using VisaUchet.Application;

namespace VisaUchet.Tests;

// Repository fakes for service tests without SQL Server
internal sealed class InMemoryApplicationRepository : IApplicationRepository
{
    public List<VisaApplication> Applications { get; } = [];

    public List<IssuedVisa> Visas { get; } = [];

    public List<StatusChange> Changes { get; } = [];

    public VisaApplication? FindByNumber(string number) => Applications.FirstOrDefault(a => a.Number == number);

    public IReadOnlyList<ApplicationSummary> Search(string text, int limit) => Applications
        .Where(a => text.Length == 0 || a.Number.Contains(text) || a.Applicant.LastName.StartsWith(text))
        .Take(limit)
        .Select(a => new ApplicationSummary(a.Number, a.RegistrationDate, a.Applicant.FullName(), a.Applicant.PassportNumber, a.Status, a.ControlDate))
        .ToList();

    public Applicant? FindApplicant(string lastName, string firstName, DateOnly birthDate, string citizenship) =>
        Applications.Select(a => a.Applicant).FirstOrDefault(p => p.IsSamePerson(lastName, firstName, birthDate, citizenship));

    public int CountApplications(int applicantId) => Applications.Count(a => a.Applicant.Id == applicantId);

    public int LastSequenceNumber(int year) => Applications.Count(a => a.RegistrationDate.Year == year);

    public void Add(VisaApplication application)
    {
        if (application.Applicant.Id == 0)
        {
            application.Applicant.Id = Applications.Count + 1;
        }

        Applications.Add(application);
    }

    public void SaveStatusChange(StatusChange change)
    {
        Changes.Add(change);
        if (change.NewVisa is not null)
        {
            Visas.Add(change.NewVisa);
        }
    }

    public bool VisaNumberExists(string visaNumber) => Visas.Any(v => v.VisaNumber == visaNumber);

    public IssuedVisa? FindVisaByApplication(string applicationNumber) => Visas.FirstOrDefault(v => v.ApplicationNumber == applicationNumber);

    public IReadOnlyList<IssuedVisa> FindIssued(DateOnly from, DateOnly to) =>
        Visas.Where(v => v.IssueDate >= from && v.IssueDate <= to).ToList();

    public IReadOnlyList<VisaApplication> FindRegistered(DateOnly from, DateOnly to) => Applications
        .Where(a => DateOnly.FromDateTime(a.RegistrationDate) >= from && DateOnly.FromDateTime(a.RegistrationDate) <= to)
        .ToList();

    public RetentionResult PurgeExpired(DateTime applicationCutoff, DateOnly journalCutoff) => new(0, 0);
}

internal sealed class InMemoryEmployeeRepository : IEmployeeRepository
{
    public List<Employee> Employees { get; } = [];

    public Employee? FindByLogin(string login) => Employees.FirstOrDefault(e => e.Login == login);

    public IReadOnlyList<Employee> All() => Employees;

    public void Add(Employee employee) => Employees.Add(employee);

    // Objects are kept by reference, so changes are already stored
    public void Update(Employee employee) => ArgumentNullException.ThrowIfNull(employee);
}
