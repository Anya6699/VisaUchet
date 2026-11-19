using VisaUchet.Domain;

namespace VisaUchet.Application;

// Indicators of form Ф5 «Отчёт о заявках» (RPT-01, SRS table 4.5)
public sealed class ApplicationReport
{
    private const int Precision = 1;
    private readonly IReadOnlyList<VisaApplication> _applications;
    private readonly ControlDateCalculator _calculator;

    public ApplicationReport(
        DateOnly from, DateOnly to, IReadOnlyList<VisaApplication> applications, ControlDateCalculator calculator)
    {
        From = from;
        To = to;
        _applications = applications ?? throw new ArgumentNullException(nameof(applications));
        _calculator = calculator ?? throw new ArgumentNullException(nameof(calculator));
    }

    public DateOnly From { get; }

    public DateOnly To { get; }

    public int RegisteredCount() => _applications.Count;

    public IReadOnlyDictionary<ApplicationStatus, int> CountByStatus() =>
        _applications.GroupBy(a => a.Status).OrderBy(g => g.Key).ToDictionary(g => g.Key, g => g.Count());

    public IReadOnlyDictionary<string, int> CountByCategory() =>
        _applications.GroupBy(a => a.Requested.Category).OrderBy(g => g.Key, StringComparer.Ordinal)
            .ToDictionary(g => g.Key, g => g.Count());

    // Refused / (issued + refused) × 100; null when there are no decisions
    public decimal? RefusalRate()
    {
        var decisions = Decisions().ToList();
        if (decisions.Count == 0)
        {
            return null;
        }

        var refused = decisions.Count(d => d.Status == ApplicationStatus.Refused);
        return Math.Round(100m * refused / decisions.Count, Precision, MidpointRounding.AwayFromZero);
    }

    // Average number of working days from registration to the decision
    public decimal? AverageReviewDays()
    {
        var days = Decisions()
            .Select(d => _calculator.WorkingDaysBetween(DateOnly.FromDateTime(d.Application.RegistrationDate), d.Date))
            .ToList();
        return days.Count == 0
            ? null
            : Math.Round((decimal)days.Average(), Precision, MidpointRounding.AwayFromZero);
    }

    public int OverdueDecisions() => Decisions().Count(d => d.Date > d.Application.ControlDate);

    private IEnumerable<Decision> Decisions()
    {
        foreach (var application in _applications)
        {
            var record = application.History.FirstOrDefault(h => StatusRules.Default.IsDecision(h.NewStatus));
            if (record is not null)
            {
                yield return new Decision(application, record.NewStatus, DateOnly.FromDateTime(record.ChangedAt));
            }
        }
    }

    private sealed record Decision(VisaApplication Application, ApplicationStatus Status, DateOnly Date);
}

public sealed class ReportService
{
    private readonly IApplicationRepository _repository;
    private readonly ControlDateCalculator _calculator;

    public ReportService(IApplicationRepository repository, ControlDateCalculator calculator)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _calculator = calculator ?? throw new ArgumentNullException(nameof(calculator));
    }

    // RPT-02: no applications in the period – the report is not built
    public ApplicationReport Build(DateOnly from, DateOnly to)
    {
        if (to < from)
        {
            throw new DomainException("Дата окончания периода раньше даты начала.");
        }

        var applications = _repository.FindRegistered(from, to);
        if (applications.Count == 0)
        {
            throw new DomainException("Нет данных за выбранный период.");
        }

        return new ApplicationReport(from, to, applications, _calculator);
    }
}
