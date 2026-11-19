using Microsoft.Data.SqlClient;
using VisaUchet.Infrastructure;

namespace VisaUchet.WinForms;

// Composition root: the only place that knows concrete implementations
public sealed class Services
{
    private Services(AppSettings settings, IAppLog log, IClock clock)
    {
        Settings = settings;
        Log = log;
        Clock = clock;
    }

    public AppSettings Settings { get; }

    public IAppLog Log { get; }

    public IClock Clock { get; }

    public ApplicationService Applications { get; private set; } = null!;

    public VisaJournalService Journal { get; private set; } = null!;

    public ReportService Reports { get; private set; } = null!;

    public AuthService Auth { get; private set; } = null!;

    public UserAdminService Users { get; private set; } = null!;

    public RetentionService Retention { get; private set; } = null!;

    public static Services Create(AppSettings settings, IAppLog log)
    {
        var services = new Services(settings, log, new SystemClock());
        var connections = new DbConnectionFactory(SqlClientFactory.Instance, settings.ConnectionString);
        var applications = new SqlApplicationRepository(connections);
        var employees = new SqlEmployeeRepository(connections);
        var calculator = new ControlDateCalculator(new SqlHolidayRepository(connections).All());
        services.Applications = new ApplicationService(applications, StatusRules.Default, calculator, services.Clock, log);
        services.Journal = new VisaJournalService(applications);
        services.Reports = new ReportService(applications, calculator);
        services.Auth = new AuthService(employees, services.Clock, log);
        services.Users = new UserAdminService(employees, log);
        services.Retention = new RetentionService(applications, services.Clock, log);
        return services;
    }
}
