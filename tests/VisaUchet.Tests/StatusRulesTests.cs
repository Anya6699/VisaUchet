namespace VisaUchet.Tests;

public class StatusRulesTests
{
    private readonly StatusRules _rules = StatusRules.Default;

    [Theory]
    [InlineData(ApplicationStatus.Registered, ApplicationStatus.InReview)]
    [InlineData(ApplicationStatus.InReview, ApplicationStatus.Issued)]
    [InlineData(ApplicationStatus.Interview, ApplicationStatus.InReview)]
    [InlineData(ApplicationStatus.Issued, ApplicationStatus.Cancelled)]
    public void CanMove_AllowedTransition_ReturnsTrue(ApplicationStatus from, ApplicationStatus to) =>
        Assert.True(_rules.CanMove(from, to));

    [Theory]
    [InlineData(ApplicationStatus.Registered, ApplicationStatus.Issued)]
    [InlineData(ApplicationStatus.Refused, ApplicationStatus.InReview)]
    [InlineData(ApplicationStatus.Interview, ApplicationStatus.Issued)]
    public void CanMove_ForbiddenTransition_ReturnsFalse(ApplicationStatus from, ApplicationStatus to) =>
        Assert.False(_rules.CanMove(from, to));

    [Theory]
    [InlineData(ApplicationStatus.Refused)]
    [InlineData(ApplicationStatus.Returned)]
    [InlineData(ApplicationStatus.Withdrawn)]
    [InlineData(ApplicationStatus.Cancelled)]
    public void IsFinal_FinalStatus_ReturnsTrue(ApplicationStatus status) => Assert.True(_rules.IsFinal(status));

    [Fact]
    public void RequiresComment_ReturnedAndCancelled()
    {
        Assert.True(_rules.RequiresComment(ApplicationStatus.Returned));
        Assert.True(_rules.RequiresComment(ApplicationStatus.Cancelled));
        Assert.False(_rules.RequiresComment(ApplicationStatus.InReview));
    }
}
