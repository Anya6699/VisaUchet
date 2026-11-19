namespace VisaUchet.Domain;

// Visa application with its current status and status history (SRS entity «Визовая заявка»)
public sealed class VisaApplication
{
    private readonly List<StatusHistory> _history = [];

    public required string Number { get; init; }

    public DateTime RegistrationDate { get; init; }

    public required Applicant Applicant { get; init; }

    public required VisaParameters Requested { get; init; }

    public bool IsUrgent { get; init; }

    public ApplicationStatus Status { get; private set; } = ApplicationStatus.Registered;

    public DateOnly ControlDate { get; init; }

    public IReadOnlyList<StatusHistory> History => _history;

    // Restores a stored application (used by repositories)
    public void LoadHistory(IEnumerable<StatusHistory> records)
    {
        _history.Clear();
        _history.AddRange(records.OrderBy(h => h.ChangedAt).ThenBy(h => h.Id));
        if (_history.Count > 0)
        {
            Status = _history[^1].NewStatus;
        }
    }

    public bool CanMoveTo(ApplicationStatus to) => StatusRules.Default.CanMove(Status, to);

    // Records the first history entry at registration
    public StatusHistory Register(string employeeLogin)
    {
        if (_history.Count > 0)
        {
            throw new DomainException("Заявка уже зарегистрирована.");
        }

        return Append(null, ApplicationStatus.Registered, employeeLogin, null, RegistrationDate);
    }

    public StatusHistory ChangeStatus(ApplicationStatus to, string employeeLogin, string? comment, DateTime at)
    {
        if (!CanMoveTo(to))
        {
            throw new DomainException($"Переход из статуса «{StatusNames.Of(Status)}» в «{StatusNames.Of(to)}» не допускается.");
        }

        var errors = CommentRules.Check(to, comment);
        errors.ThrowIfInvalid();
        var record = Append(Status, to, employeeLogin, Normalize(comment), at);
        Status = to;
        return record;
    }

    // CRD-03: control date has passed while the decision is still pending
    public bool IsOverdue(DateOnly today) =>
        StatusRules.Default.IsPending(Status) && today > ControlDate;

    // Date of the decision (visa issued or refused), if any
    public DateTime? DecisionDate() =>
        _history.FirstOrDefault(h => StatusRules.Default.IsDecision(h.NewStatus))?.ChangedAt;

    private StatusHistory Append(
        ApplicationStatus? from, ApplicationStatus to, string login, string? comment, DateTime at)
    {
        var record = new StatusHistory
        {
            ApplicationNumber = Number,
            OldStatus = from,
            NewStatus = to,
            ChangedAt = at,
            EmployeeLogin = login,
            Comment = comment,
        };
        _history.Add(record);
        return record;
    }

    private static string? Normalize(string? comment) =>
        string.IsNullOrWhiteSpace(comment) ? null : comment.Trim();
}
