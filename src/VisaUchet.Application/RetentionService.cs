namespace VisaUchet.Application;

// DATA-03: applications are kept 1 year after the final status, journal records 2 years
public sealed class RetentionService
{
    public const int ApplicationRetentionYears = 1;
    public const int JournalRetentionYears = 2;

    private readonly IApplicationRepository _repository;
    private readonly IClock _clock;
    private readonly IAppLog _log;

    public RetentionService(IApplicationRepository repository, IClock clock, IAppLog log)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _clock = clock ?? throw new ArgumentNullException(nameof(clock));
        _log = log ?? throw new ArgumentNullException(nameof(log));
    }

    public RetentionResult Apply()
    {
        var now = _clock.Now;
        var result = _repository.PurgeExpired(
            now.AddYears(-ApplicationRetentionYears),
            DateOnly.FromDateTime(now).AddYears(-JournalRetentionYears));
        _log.Info($"Хранение: удалено заявок {result.ApplicationsDeleted}, записей журнала {result.JournalRecordsDeleted}");
        return result;
    }
}
