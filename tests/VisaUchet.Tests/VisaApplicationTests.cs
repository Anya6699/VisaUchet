namespace VisaUchet.Tests;

public class VisaApplicationTests
{
    [Fact]
    public void Register_CreatesFirstHistoryRecord()
    {
        var application = TestData.Application();

        var record = Assert.Single(application.History);
        Assert.Null(record.OldStatus);
        Assert.Equal(ApplicationStatus.Registered, application.Status);
    }

    [Fact]
    public void ChangeStatus_Allowed_AppendsHistory()
    {
        var application = TestData.Application();

        var record = application.ChangeStatus(ApplicationStatus.InReview, "consul", null, FixedClock.Monday);

        Assert.Equal(ApplicationStatus.Registered, record.OldStatus);
        Assert.Equal(ApplicationStatus.InReview, application.Status);
        Assert.Equal(2, application.History.Count);
    }

    [Fact]
    public void ChangeStatus_Forbidden_ThrowsAndKeepsStatus()
    {
        var application = TestData.Application();

        Assert.Throws<DomainException>(() => application.ChangeStatus(ApplicationStatus.Issued, "consul", null, FixedClock.Monday));
        Assert.Equal(ApplicationStatus.Registered, application.Status);
    }

    [Fact]
    public void ChangeStatus_ReturnWithoutComment_Throws()
    {
        var application = TestData.Application();

        var error = Assert.Throws<DomainException>(() => application.ChangeStatus(ApplicationStatus.Returned, "operator", " ", FixedClock.Monday));
        Assert.Equal(CommentRules.Field, error.Errors[0].Field);
    }

    [Fact]
    public void IsOverdue_PendingAfterControlDate_ReturnsTrue()
    {
        var application = TestData.Application(ApplicationStatus.InReview);

        Assert.False(application.IsOverdue(application.ControlDate));
        Assert.True(application.IsOverdue(application.ControlDate.AddDays(1)));
    }

    [Fact]
    public void IsOverdue_DecisionMade_ReturnsFalse() =>
        Assert.False(TestData.Application(ApplicationStatus.Refused).IsOverdue(TestData.Today.AddYears(1)));

    [Fact]
    public void IssuedVisa_CancelTwice_Throws()
    {
        var visa = new IssuedVisa
        {
            VisaNumber = "BLR0000001", HolderName = "SMITH JOHN", HolderCitizenship = "GBR",
            Parameters = TestData.Input().ToParameters(), Currency = "EUR", IssuedBy = "consul",
        };

        visa.Cancel("Ошибка в данных", TestData.Today);

        Assert.Equal(VisaState.Cancelled, visa.State);
        Assert.Throws<DomainException>(() => visa.Cancel("Повторно", TestData.Today));
    }
}
