namespace VisaUchet.Tests;

public class ControlDateCalculatorTests
{
    private static readonly DateOnly Friday = new(2026, 11, 20);

    [Fact]
    public void Calculate_RegularFromFriday_SkipsWeekend() =>
        Assert.Equal(new DateOnly(2026, 11, 27), new ControlDateCalculator([]).Calculate(Friday, urgent: false));

    [Fact]
    public void Calculate_UrgentFromFriday_ReturnsTuesday() =>
        Assert.Equal(new DateOnly(2026, 11, 24), new ControlDateCalculator([]).Calculate(Friday, urgent: true));

    [Fact]
    public void Calculate_HolidayIsNotCounted()
    {
        var calculator = new ControlDateCalculator([new DateOnly(2026, 11, 23)]);

        Assert.Equal(new DateOnly(2026, 11, 25), calculator.Calculate(Friday, urgent: true));
    }

    [Fact]
    public void WorkingDaysBetween_CountsOnlyWorkingDays() =>
        Assert.Equal(5, new ControlDateCalculator([]).WorkingDaysBetween(Friday, new DateOnly(2026, 11, 27)));
}
