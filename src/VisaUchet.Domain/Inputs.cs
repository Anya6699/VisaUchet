namespace VisaUchet.Domain;

// Data of form Ф1 «Регистрация заявки» (SRS table 4.1)
public sealed record ApplicationInput(
    string LastName,
    string FirstName,
    DateOnly BirthDate,
    Sex Sex,
    string Citizenship,
    string PassportNumber,
    DateOnly PassportExpiry,
    VisaType VisaType,
    string Category,
    Multiplicity Multiplicity,
    DateOnly ValidFrom,
    DateOnly ValidTo,
    int StayDays,
    bool IsUrgent)
{
    public VisaParameters ToParameters() =>
        new(VisaType, Category, Multiplicity, ValidFrom, ValidTo, StayDays);
}

// Visa data entered in form Ф2 when the status changes to «Виза выдана» (SRS table 4.2)
public sealed record VisaIssueInput(
    string VisaNumber,
    DateOnly ValidFrom,
    DateOnly ValidTo,
    Multiplicity Multiplicity,
    int StayDays,
    decimal Fee,
    string Currency);

// Field identifiers shared by validators and forms
public static class Fields
{
    public const string LastName = nameof(ApplicationInput.LastName);
    public const string FirstName = nameof(ApplicationInput.FirstName);
    public const string BirthDate = nameof(ApplicationInput.BirthDate);
    public const string Citizenship = nameof(ApplicationInput.Citizenship);
    public const string PassportNumber = nameof(ApplicationInput.PassportNumber);
    public const string PassportExpiry = nameof(ApplicationInput.PassportExpiry);
    public const string Category = nameof(ApplicationInput.Category);
    public const string Multiplicity = nameof(ApplicationInput.Multiplicity);
    public const string ValidFrom = nameof(ApplicationInput.ValidFrom);
    public const string ValidTo = nameof(ApplicationInput.ValidTo);
    public const string StayDays = nameof(ApplicationInput.StayDays);
    public const string VisaNumber = nameof(VisaIssueInput.VisaNumber);
    public const string Fee = nameof(VisaIssueInput.Fee);
    public const string Currency = nameof(VisaIssueInput.Currency);
}
