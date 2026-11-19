using VisaUchet.Domain;

namespace VisaUchet.Application;

// Content and totals of form Ф4 «Журнал выданных виз» (JRN-02)
public sealed class VisaJournal
{
    public VisaJournal(JournalFilter filter, IReadOnlyList<IssuedVisa> records)
    {
        Filter = filter ?? throw new ArgumentNullException(nameof(filter));
        ArgumentNullException.ThrowIfNull(records);
        Rows = records
            .Where(v => filter.Type is null || v.Parameters.Type == filter.Type)
            .Where(v => filter.State is null || v.State == filter.State)
            .OrderBy(v => v.IssueDate)
            .ThenBy(v => v.VisaNumber, StringComparer.Ordinal)
            .ToList();
    }

    public JournalFilter Filter { get; }

    public IReadOnlyList<IssuedVisa> Rows { get; }

    public int CountVisas() => Rows.Count;

    public int CountCancelled() => Rows.Count(v => v.State == VisaState.Cancelled);

    public decimal TotalFee(string currency) =>
        Rows.Where(v => v.Currency == currency).Sum(v => v.Fee);

    // Currencies present in the selection, in the order of DomainConstants.Currencies
    public IReadOnlyList<string> Currencies() =>
        DomainConstants.Currencies.Where(c => Rows.Any(v => v.Currency == c)).ToList();
}

public sealed class VisaJournalService
{
    private readonly IApplicationRepository _repository;

    public VisaJournalService(IApplicationRepository repository) =>
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));

    public VisaJournal Query(JournalFilter filter)
    {
        ArgumentNullException.ThrowIfNull(filter);
        if (filter.To < filter.From)
        {
            throw new DomainException("Дата окончания периода раньше даты начала.");
        }

        return new VisaJournal(filter, _repository.FindIssued(filter.From, filter.To));
    }

    // JRN-02: by default the current month
    public static JournalFilter DefaultFilter(DateOnly today)
    {
        var first = new DateOnly(today.Year, today.Month, 1);
        return new JournalFilter(first, first.AddMonths(1).AddDays(-1), null, null);
    }
}
