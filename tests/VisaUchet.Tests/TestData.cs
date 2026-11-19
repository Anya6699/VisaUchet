using VisaUchet.Application;

namespace VisaUchet.Tests;

// Fixed time: Monday 16.11.2026 10:00
internal sealed class FixedClock : IClock
{
    public static readonly DateTime Monday = new(2026, 11, 16, 10, 0, 0);

    public DateTime Now { get; set; } = Monday;
}

// Collects log messages to check that no sensitive data is written
internal sealed class MemoryLog : IAppLog
{
    public List<string> Messages { get; } = [];

    public void Info(string message) => Messages.Add(message);

    public void Error(string message, Exception exception) => Messages.Add($"{message}: {exception.Message}");
}

internal static class TestData
{
    public static readonly DateOnly Today = DateOnly.FromDateTime(FixedClock.Monday);

    public static ApplicationInput Input(VisaType type = VisaType.C, int periodDays = 14, bool urgent = false) => new(
        "SMITH", "JOHN", new DateOnly(1990, 5, 1), Sex.M, "GBR", "123456789", Today.AddYears(5),
        type, DomainConstants.Categories[10], type == VisaType.D ? Multiplicity.Multiple : Multiplicity.Single,
        Today.AddDays(7), Today.AddDays(7 + periodDays - 1), Math.Min(periodDays, 14), urgent);

    public static VisaIssueInput Visa(VisaParameters requested, string number = "BLR1234567") => new(
        number, requested.ValidFrom, requested.ValidTo, requested.Multiplicity, requested.StayDays, 80m, "EUR");

    public static VisaApplication Application(ApplicationStatus status = ApplicationStatus.Registered)
    {
        var input = Input();
        var application = new VisaApplication
        {
            Number = "VZ-2026-000001",
            RegistrationDate = FixedClock.Monday,
            Applicant = new Applicant
            {
                LastName = input.LastName, FirstName = input.FirstName, Citizenship = input.Citizenship,
                PassportNumber = input.PassportNumber, BirthDate = input.BirthDate, PassportExpiry = input.PassportExpiry,
            },
            Requested = input.ToParameters(),
            ControlDate = Today.AddDays(7),
        };
        application.Register("operator");
        foreach (var step in PathTo(status))
        {
            application.ChangeStatus(step, "consul", "Комментарий по заявке", FixedClock.Monday.AddHours(1));
        }

        return application;
    }

    private static ApplicationStatus[] PathTo(ApplicationStatus status) => status switch
    {
        ApplicationStatus.Registered => [],
        ApplicationStatus.Issued => [ApplicationStatus.InReview, ApplicationStatus.Issued],
        ApplicationStatus.Refused => [ApplicationStatus.InReview, ApplicationStatus.Refused],
        _ => [ApplicationStatus.InReview],
    };
}
