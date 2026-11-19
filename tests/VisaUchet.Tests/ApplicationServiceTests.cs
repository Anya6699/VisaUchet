using VisaUchet.Application;

namespace VisaUchet.Tests;

public class ApplicationServiceTests
{
    private readonly InMemoryApplicationRepository _repository = new();
    private readonly FixedClock _clock = new();
    private readonly MemoryLog _log = new();
    private readonly ApplicationService _service;

    public ApplicationServiceTests() =>
        _service = new ApplicationService(_repository, StatusRules.Default, new ControlDateCalculator([]), _clock, _log);

    [Fact]
    public void Register_ValidInput_AssignsNumberAndControlDate()
    {
        var result = _service.Register(TestData.Input(urgent: true), "operator", null);

        Assert.Equal("VZ-2026-000001", result.Number);
        Assert.Equal(new DateOnly(2026, 11, 18), result.ControlDate);
        Assert.Equal(ApplicationStatus.Registered, _repository.Applications[0].Status);
    }

    [Fact]
    public void Register_InvalidInput_SavesNothing()
    {
        Assert.Throws<DomainException>(() => _service.Register(TestData.Input() with { FirstName = "" }, "operator", null));
        Assert.Empty(_repository.Applications);
    }

    [Fact]
    public void Register_ExistingApplicant_ReusesApplicantAndUpdatesPassport()
    {
        _service.Register(TestData.Input(), "operator", null);
        var input = TestData.Input() with { PassportNumber = "NEW12345" };
        var match = _service.FindExistingApplicant(input);

        Assert.NotNull(match);
        _service.Register(input, "operator", match!.Applicant.Id);

        Assert.Same(_repository.Applications[0].Applicant, _repository.Applications[1].Applicant);
        Assert.Equal("NEW12345", _repository.Applications[1].Applicant.PassportNumber);
        Assert.Equal(1, _service.GetCard("VZ-2026-000002").PreviousApplications);
    }

    [Fact]
    public void ChangeStatus_IssueVisa_CreatesJournalRecord()
    {
        var number = Registered(ApplicationStatus.InReview);
        var requested = _repository.Applications[0].Requested;

        _service.ChangeStatus(new StatusChangeRequest(number, ApplicationStatus.Issued, null, TestData.Visa(requested)), "consul");

        var visa = Assert.Single(_repository.Visas);
        Assert.Equal(number, visa.ApplicationNumber);
        Assert.Equal(ApplicationStatus.Issued, _repository.Applications[0].Status);
    }

    [Fact]
    public void ChangeStatus_IssueWithoutVisaData_Throws()
    {
        var number = Registered(ApplicationStatus.InReview);

        Assert.Throws<DomainException>(() => _service.ChangeStatus(new StatusChangeRequest(number, ApplicationStatus.Issued, null, null), "consul"));
        Assert.Empty(_repository.Visas);
    }

    [Fact]
    public void ChangeStatus_CancelVisa_MarksJournalRecordCancelled()
    {
        var number = Registered(ApplicationStatus.InReview);
        _service.ChangeStatus(new StatusChangeRequest(number, ApplicationStatus.Issued, null, TestData.Visa(_repository.Applications[0].Requested)), "consul");

        _service.ChangeStatus(new StatusChangeRequest(number, ApplicationStatus.Cancelled, "Подложные документы", null), "consul");

        Assert.Equal(VisaState.Cancelled, _repository.Visas[0].State);
        Assert.Equal("Подложные документы", _repository.Visas[0].CancelReason);
    }

    [Fact]
    public void GetCard_UnknownNumber_Throws() => Assert.Throws<DomainException>(() => _service.GetCard("VZ-2026-999999"));

    [Fact]
    public void Search_EmptyText_ReturnsAllApplications()
    {
        _service.Register(TestData.Input(), "operator", null);
        _service.Register(TestData.Input(), "operator", null);

        Assert.Equal(2, _service.Search("  ").Count);
        Assert.Equal(2, _service.Search("SMI").Count);
        Assert.Empty(_service.Search("ZZZ"));
    }

    private string Registered(ApplicationStatus status)
    {
        var number = _service.Register(TestData.Input(), "operator", null).Number;
        if (status != ApplicationStatus.Registered)
        {
            _service.ChangeStatus(new StatusChangeRequest(number, status, null, null), "consul");
        }

        return number;
    }
}
