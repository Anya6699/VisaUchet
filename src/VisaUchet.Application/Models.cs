using VisaUchet.Domain;

namespace VisaUchet.Application;

// Row of the search result (CRD-01)
public sealed record ApplicationSummary(
    string Number,
    DateTime RegistrationDate,
    string ApplicantName,
    string PassportNumber,
    ApplicationStatus Status,
    DateOnly ControlDate);

// Existing applicant found by name, birth date and citizenship (REG-03)
public sealed record ApplicantMatch(Applicant Applicant, int PreviousApplications);

public sealed record RegistrationResult(string Number, DateOnly ControlDate);

// Data saved by one status change transaction
public sealed record StatusChange(
    VisaApplication Application,
    StatusHistory History,
    IssuedVisa? NewVisa,
    IssuedVisa? CancelledVisa);

// Data of form Ф2 «Смена статуса»
public sealed record StatusChangeRequest(
    string ApplicationNumber,
    ApplicationStatus NewStatus,
    string? Comment,
    VisaIssueInput? Visa);

// Content of form Ф3 «Карточка заявки» (SRS table 4.3)
public sealed record ApplicationCard(
    VisaApplication Application,
    int PreviousApplications,
    bool IsOverdue,
    IssuedVisa? Visa,
    IReadOnlyList<ApplicationStatus> AllowedStatuses);

// Selection of form Ф4 «Журнал выданных виз»
public sealed record JournalFilter(DateOnly From, DateOnly To, VisaType? Type, VisaState? State);

public sealed record RetentionResult(int ApplicationsDeleted, int JournalRecordsDeleted);
