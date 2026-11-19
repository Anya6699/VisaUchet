using VisaUchet.Application;

namespace VisaUchet.Tests;

public class JournalAndReportTests
{
    private readonly InMemoryApplicationRepository _repository = new();

    [Fact]
    public void Journal_FiltersAndTotalsByCurrency()
    {
        AddVisa("BLR0000001", 60m, "EUR", VisaType.C);
        AddVisa("BLR0000002", 70m, "EUR", VisaType.C).Cancel("Ошибка", TestData.Today);
        AddVisa("BLR0000003", 100m, "USD", VisaType.D);
        var service = new VisaJournalService(_repository);

        var all = service.Query(VisaJournalService.DefaultFilter(TestData.Today));
        var onlyC = service.Query(VisaJournalService.DefaultFilter(TestData.Today) with { Type = VisaType.C });

        Assert.Equal(3, all.CountVisas());
        Assert.Equal(1, all.CountCancelled());
        Assert.Equal(130m, all.TotalFee("EUR"));
        Assert.Equal(2, onlyC.CountVisas());
    }

    [Fact]
    public void Journal_EndBeforeStart_Throws() =>
        Assert.Throws<DomainException>(() => new VisaJournalService(_repository)
            .Query(new JournalFilter(TestData.Today, TestData.Today.AddDays(-1), null, null)));

    [Fact]
    public void Report_CountsRefusalRateAndOverdueDecisions()
    {
        _repository.Add(TestData.Application(ApplicationStatus.Issued));
        _repository.Add(TestData.Application(ApplicationStatus.Refused));
        _repository.Add(TestData.Application(ApplicationStatus.InReview));
        var report = new ReportService(_repository, new ControlDateCalculator([])).Build(TestData.Today, TestData.Today);

        Assert.Equal(3, report.RegisteredCount());
        Assert.Equal(50.0m, report.RefusalRate());
        Assert.Equal(0, report.OverdueDecisions());
        Assert.Equal(1, report.CountByStatus()[ApplicationStatus.InReview]);
    }

    [Fact]
    public void Report_NoApplications_ThrowsNoData()
    {
        var error = Assert.Throws<DomainException>(() =>
            new ReportService(_repository, new ControlDateCalculator([])).Build(TestData.Today, TestData.Today));

        Assert.Equal("Нет данных за выбранный период.", error.Message);
    }

    private IssuedVisa AddVisa(string number, decimal fee, string currency, VisaType type)
    {
        var visa = new IssuedVisa
        {
            VisaNumber = number, IssueDate = TestData.Today, HolderName = "SMITH JOHN", HolderCitizenship = "GBR",
            Parameters = TestData.Input(type, type == VisaType.D ? 180 : 14).ToParameters(), Fee = fee,
            Currency = currency, IssuedBy = "consul",
        };
        _repository.Visas.Add(visa);
        return visa;
    }
}
