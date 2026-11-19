namespace VisaUchet.Domain;

// Visa applicant: personal and passport data (SRS entity «Заявитель»)
public sealed class Applicant
{
    public int Id { get; set; }

    public required string LastName { get; init; }

    public required string FirstName { get; init; }

    public DateOnly BirthDate { get; init; }

    public Sex Sex { get; init; }

    public required string Citizenship { get; init; }

    public required string PassportNumber { get; set; }

    public DateOnly PassportExpiry { get; set; }

    public string FullName() => $"{LastName} {FirstName}";

    // REG-06: passport must stay valid 90 days after the visa ends
    public bool HasValidPassport(DateOnly visaEnd) =>
        PassportExpiry >= visaEnd.AddDays(DomainConstants.PassportReserveDays);

    // REG-03: identity match by name, birth date and citizenship
    public bool IsSamePerson(string lastName, string firstName, DateOnly birthDate, string citizenship) =>
        string.Equals(LastName, lastName, StringComparison.OrdinalIgnoreCase)
        && string.Equals(FirstName, firstName, StringComparison.OrdinalIgnoreCase)
        && BirthDate == birthDate
        && string.Equals(Citizenship, citizenship, StringComparison.OrdinalIgnoreCase);

    public void UpdatePassport(string number, DateOnly expiry)
    {
        PassportNumber = number;
        PassportExpiry = expiry;
    }
}
