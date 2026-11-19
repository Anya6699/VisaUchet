namespace VisaUchet.Domain;

// Visa parameters shared by the requested visa and the issued visa
public sealed record VisaParameters(
    VisaType Type,
    string Category,
    Multiplicity Multiplicity,
    DateOnly ValidFrom,
    DateOnly ValidTo,
    int StayDays)
{
    // Period length in days, both ends inclusive
    public int PeriodDays => ValidTo.DayNumber - ValidFrom.DayNumber + 1;

    public bool IsWithin(VisaParameters requested) =>
        ValidFrom >= requested.ValidFrom && ValidTo <= requested.ValidTo;
}
