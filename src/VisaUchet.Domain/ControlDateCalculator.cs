namespace VisaUchet.Domain;

// REG-05: control date in working days (Mon–Fri except holidays from the settings)
public sealed class ControlDateCalculator
{
    private readonly HashSet<DateOnly> _holidays;

    public ControlDateCalculator(IEnumerable<DateOnly> holidays)
    {
        ArgumentNullException.ThrowIfNull(holidays);
        _holidays = [.. holidays];
    }

    public DateOnly Calculate(DateOnly registrationDate, bool urgent) =>
        AddWorkingDays(
            registrationDate,
            urgent ? DomainConstants.UrgentReviewWorkingDays : DomainConstants.RegularReviewWorkingDays);

    public DateOnly AddWorkingDays(DateOnly date, int days)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(days);
        var current = date;
        var added = 0;
        while (added < days)
        {
            current = current.AddDays(1);
            if (IsWorkingDay(current))
            {
                added++;
            }
        }

        return current;
    }

    // Working days after 'from' up to and including 'to'
    public int WorkingDaysBetween(DateOnly from, DateOnly to)
    {
        var count = 0;
        for (var day = from.AddDays(1); day <= to; day = day.AddDays(1))
        {
            if (IsWorkingDay(day))
            {
                count++;
            }
        }

        return count;
    }

    public bool IsWorkingDay(DateOnly day) =>
        day.DayOfWeek is not (DayOfWeek.Saturday or DayOfWeek.Sunday) && !_holidays.Contains(day);
}
