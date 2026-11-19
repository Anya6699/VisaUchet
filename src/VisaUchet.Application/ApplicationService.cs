using VisaUchet.Domain;

namespace VisaUchet.Application;

// Registration, status change and visa issue (blocks visa.app.register, visa.app.status, visa.app.card)
public sealed class ApplicationService
{
    public const int SearchLimit = 100;

    private readonly IApplicationRepository _repository;
    private readonly StatusRules _rules;
    private readonly ControlDateCalculator _calculator;
    private readonly IClock _clock;
    private readonly IAppLog _log;

    public ApplicationService(
        IApplicationRepository repository, StatusRules rules, ControlDateCalculator calculator, IClock clock, IAppLog log)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _rules = rules ?? throw new ArgumentNullException(nameof(rules));
        _calculator = calculator ?? throw new ArgumentNullException(nameof(calculator));
        _clock = clock ?? throw new ArgumentNullException(nameof(clock));
        _log = log ?? throw new ArgumentNullException(nameof(log));
    }

    // REG-03: an applicant with the same name, birth date and citizenship
    public ApplicantMatch? FindExistingApplicant(ApplicationInput input)
    {
        ArgumentNullException.ThrowIfNull(input);
        var applicant = _repository.FindApplicant(input.LastName, input.FirstName, input.BirthDate, input.Citizenship);
        return applicant is null ? null : new ApplicantMatch(applicant, _repository.CountApplications(applicant.Id));
    }

    public RegistrationResult Register(ApplicationInput input, string employeeLogin, int? existingApplicantId)
    {
        ArgumentNullException.ThrowIfNull(input);
        ArgumentException.ThrowIfNullOrWhiteSpace(employeeLogin);
        RegistrationValidator.Validate(input, _clock.Today).ThrowIfInvalid();
        var now = _clock.Now;
        var application = new VisaApplication
        {
            Number = NextNumber(now.Year),
            RegistrationDate = now,
            Applicant = ResolveApplicant(input, existingApplicantId),
            Requested = input.ToParameters(),
            IsUrgent = input.IsUrgent,
            ControlDate = _calculator.Calculate(DateOnly.FromDateTime(now), input.IsUrgent),
        };
        application.Register(employeeLogin);
        _repository.Add(application);
        _log.Info($"Зарегистрирована заявка {application.Number}");
        return new RegistrationResult(application.Number, application.ControlDate);
    }

    public void ChangeStatus(StatusChangeRequest request, string employeeLogin)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (request.NewStatus == ApplicationStatus.Issued)
        {
            var visa = request.Visa ?? throw new DomainException("Для статуса «Виза выдана» введите сведения о визе.");
            IssueVisa(request.ApplicationNumber, visa, employeeLogin, request.Comment);
            return;
        }

        var application = Load(request.ApplicationNumber);
        var history = application.ChangeStatus(request.NewStatus, employeeLogin, request.Comment, _clock.Now);
        var cancelled = request.NewStatus == ApplicationStatus.Cancelled ? CancelJournalRecord(application, history) : null;
        _repository.SaveStatusChange(new StatusChange(application, history, null, cancelled));
        _log.Info($"Заявка {application.Number}: статус {StatusNames.Code(request.NewStatus)}");
    }

    // STS-03, JRN-01: the journal record is created only together with the status «Виза выдана»
    public IssuedVisa IssueVisa(string number, VisaIssueInput visaInput, string employeeLogin, string? comment)
    {
        ArgumentNullException.ThrowIfNull(visaInput);
        var application = Load(number);
        if (!application.CanMoveTo(ApplicationStatus.Issued))
        {
            throw new DomainException($"Из статуса «{StatusNames.Of(application.Status)}» нельзя выдать визу.");
        }

        var exists = _repository.VisaNumberExists(visaInput.VisaNumber);
        VisaIssueValidator.Validate(visaInput, application.Requested, exists).ThrowIfInvalid();
        var history = application.ChangeStatus(ApplicationStatus.Issued, employeeLogin, comment, _clock.Now);
        var visa = CreateVisa(application, visaInput, employeeLogin);
        _repository.SaveStatusChange(new StatusChange(application, history, visa, null));
        _log.Info($"Заявка {application.Number}: выдана виза {visa.VisaNumber}");
        return visa;
    }

    public ApplicationCard GetCard(string number)
    {
        var application = Load(number);
        return new ApplicationCard(
            application,
            _repository.CountApplications(application.Applicant.Id) - 1,
            application.IsOverdue(_clock.Today),
            _repository.FindVisaByApplication(number),
            _rules.AllowedFrom(application.Status));
    }

    public IReadOnlyList<ApplicationSummary> Search(string text)
    {
        var query = text?.Trim() ?? string.Empty;
        return query.Length == 0 ? [] : _repository.Search(query, SearchLimit);
    }

    private VisaApplication Load(string number) =>
        _repository.FindByNumber(number?.Trim() ?? string.Empty)
        ?? throw new DomainException($"Заявка {number} не найдена.");

    private string NextNumber(int year)
    {
        var sequence = _repository.LastSequenceNumber(year) + 1;
        return $"{DomainConstants.ApplicationNumberPrefix}-{year}-{sequence.ToString().PadLeft(DomainConstants.ApplicationNumberDigits, '0')}";
    }

    private Applicant ResolveApplicant(ApplicationInput input, int? existingApplicantId)
    {
        if (existingApplicantId is null)
        {
            return NewApplicant(input);
        }

        var match = FindExistingApplicant(input);
        if (match is null || match.Applicant.Id != existingApplicantId)
        {
            throw new DomainException("Данные найденного заявителя изменились. Повторите регистрацию.");
        }

        match.Applicant.UpdatePassport(input.PassportNumber.ToUpperInvariant(), input.PassportExpiry);
        return match.Applicant;
    }

    private static Applicant NewApplicant(ApplicationInput input) => new()
    {
        LastName = input.LastName.Trim().ToUpperInvariant(),
        FirstName = input.FirstName.Trim().ToUpperInvariant(),
        BirthDate = input.BirthDate,
        Sex = input.Sex,
        Citizenship = input.Citizenship,
        PassportNumber = input.PassportNumber.ToUpperInvariant(),
        PassportExpiry = input.PassportExpiry,
    };

    private IssuedVisa CreateVisa(VisaApplication application, VisaIssueInput input, string login) => new()
    {
        VisaNumber = input.VisaNumber,
        ApplicationNumber = application.Number,
        IssueDate = _clock.Today,
        HolderName = application.Applicant.FullName(),
        HolderCitizenship = application.Applicant.Citizenship,
        Parameters = application.Requested with
        {
            Multiplicity = input.Multiplicity,
            ValidFrom = input.ValidFrom,
            ValidTo = input.ValidTo,
            StayDays = input.StayDays,
        },
        Fee = input.Fee,
        Currency = input.Currency,
        IssuedBy = login,
    };

    // JRN-03: the journal record is marked as cancelled, never deleted
    private IssuedVisa CancelJournalRecord(VisaApplication application, StatusHistory history)
    {
        var visa = _repository.FindVisaByApplication(application.Number)
            ?? throw new DomainException($"В журнале нет визы по заявке {application.Number}.");
        visa.Cancel(history.Comment ?? string.Empty, DateOnly.FromDateTime(history.ChangedAt));
        return visa;
    }
}
