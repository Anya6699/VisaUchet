namespace VisaUchet.Tests;

public class RegistrationValidatorTests
{
    [Fact]
    public void Validate_CorrectInput_IsValid() =>
        Assert.True(RegistrationValidator.Validate(TestData.Input(), TestData.Today).IsValid);

    [Theory]
    [InlineData("SMITH1")]
    [InlineData("ИВАНОВ")]
    [InlineData("")]
    public void Validate_NameNotLatin_ReturnsLastNameError(string lastName)
    {
        var result = RegistrationValidator.Validate(TestData.Input() with { LastName = lastName }, TestData.Today);

        Assert.Contains(result.Errors, e => e.Field == Fields.LastName);
    }

    [Fact]
    public void Validate_UnknownCountry_ReturnsCitizenshipError()
    {
        var result = RegistrationValidator.Validate(TestData.Input() with { Citizenship = "ZZZ" }, TestData.Today);

        Assert.Contains(result.Errors, e => e.Field == Fields.Citizenship);
    }

    [Fact]
    public void Validate_PassportExpiresTooSoon_ReturnsExpiryError()
    {
        var input = TestData.Input();
        var result = RegistrationValidator.Validate(input with { PassportExpiry = input.ValidTo.AddDays(30) }, TestData.Today);

        Assert.Contains(result.Errors, e => e.Field == Fields.PassportExpiry);
    }

    [Fact]
    public void ValidateData_PassportExpiresTooSoon_IsCheckedSeparately()
    {
        var input = TestData.Input();
        var shortPassport = input with { PassportExpiry = input.ValidTo.AddDays(30) };

        Assert.True(RegistrationValidator.ValidateData(shortPassport, TestData.Today).IsValid);
        Assert.False(RegistrationValidator.IsPassportTermSufficient(shortPassport));
        Assert.True(RegistrationValidator.IsPassportTermSufficient(input));
    }

    [Fact]
    public void Validate_ShortTermVisaLongerThan90Days_ReturnsPeriodError()
    {
        var result = RegistrationValidator.Validate(TestData.Input(periodDays: 91), TestData.Today);

        Assert.Contains(result.Errors, e => e.Field == Fields.ValidTo);
    }

    [Fact]
    public void Validate_LongTermVisa_IsValid() =>
        Assert.True(RegistrationValidator.Validate(TestData.Input(VisaType.D, periodDays: 180), TestData.Today).IsValid);

    [Fact]
    public void Validate_StayLongerThanPeriod_ReturnsStayError()
    {
        var result = RegistrationValidator.Validate(TestData.Input() with { StayDays = 15 }, TestData.Today);

        Assert.Contains(result.Errors, e => e.Field == Fields.StayDays);
    }
}

public class VisaIssueValidatorTests
{
    private static readonly VisaParameters Requested = TestData.Input().ToParameters();

    [Fact]
    public void Validate_SameAsRequested_IsValid() =>
        Assert.True(VisaIssueValidator.Validate(TestData.Visa(Requested), Requested, numberExists: false).IsValid);

    [Theory]
    [InlineData("BLR123")]
    [InlineData("RUS1234567")]
    public void Validate_WrongNumberFormat_ReturnsNumberError(string number)
    {
        var result = VisaIssueValidator.Validate(TestData.Visa(Requested, number), Requested, numberExists: false);

        Assert.Contains(result.Errors, e => e.Field == Fields.VisaNumber);
    }

    [Fact]
    public void Validate_DuplicateNumber_ReturnsNumberError() =>
        Assert.Contains(VisaIssueValidator.Validate(TestData.Visa(Requested), Requested, numberExists: true).Errors,
            e => e.Field == Fields.VisaNumber);

    [Fact]
    public void Validate_PeriodWiderThanRequested_ReturnsError()
    {
        var visa = TestData.Visa(Requested) with { ValidTo = Requested.ValidTo.AddDays(1) };

        Assert.False(VisaIssueValidator.Validate(visa, Requested, numberExists: false).IsValid);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(1000)]
    [InlineData(10.555)]
    public void Validate_WrongFee_ReturnsFeeError(double fee)
    {
        var visa = TestData.Visa(Requested) with { Fee = (decimal)fee };

        Assert.Contains(VisaIssueValidator.Validate(visa, Requested, numberExists: false).Errors, e => e.Field == Fields.Fee);
    }
}
