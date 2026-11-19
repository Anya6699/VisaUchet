namespace VisaUchet.Domain;

// Application statuses and allowed transitions: SRS table 3.2
public enum ApplicationStatus
{
    Registered,
    InReview,
    Interview,
    OnApproval,
    Issued,
    Returned,
    Refused,
    Withdrawn,
    Cancelled,
}

// Entry visa types: B – transit, C – short-term, D – long-term
public enum VisaType
{
    B,
    C,
    D,
}

public enum Multiplicity
{
    Single,
    Double,
    Multiple,
}

// State of a journal record
public enum VisaState
{
    Valid,
    Cancelled,
}

public enum Sex
{
    M,
    F,
}

// User roles: SRS table 2.2
public enum Role
{
    Operator,
    ConsulOfficer,
    Head,
    Admin,
}

// Forms of the system: Ф1–Ф5 and user administration
public enum FormCode
{
    Registration,
    StatusChange,
    ApplicationCard,
    VisaJournal,
    Report,
    Users,
}
