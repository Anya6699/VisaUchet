namespace VisaUchet.Domain;

// Display names and storage codes of enumerations
public static class StatusNames
{
    private static readonly IReadOnlyDictionary<ApplicationStatus, (string Code, string Name)> Statuses =
        new Dictionary<ApplicationStatus, (string, string)>
        {
            [ApplicationStatus.Registered] = ("REGISTERED", "Зарегистрирована"),
            [ApplicationStatus.InReview] = ("IN_REVIEW", "На рассмотрении"),
            [ApplicationStatus.Interview] = ("INTERVIEW", "Собеседование"),
            [ApplicationStatus.OnApproval] = ("ON_APPROVAL", "Согласование"),
            [ApplicationStatus.Issued] = ("ISSUED", "Виза выдана"),
            [ApplicationStatus.Returned] = ("RETURNED", "Возвращена"),
            [ApplicationStatus.Refused] = ("REFUSED", "Отказано"),
            [ApplicationStatus.Withdrawn] = ("WITHDRAWN", "Отозвана"),
            [ApplicationStatus.Cancelled] = ("CANCELLED", "Виза аннулирована"),
        };

    private static readonly IReadOnlyDictionary<Role, string> RoleNames = new Dictionary<Role, string>
    {
        [Role.Operator] = "Оператор приёма",
        [Role.ConsulOfficer] = "Консульское должностное лицо",
        [Role.Head] = "Руководитель",
        [Role.Admin] = "Администратор",
    };

    public static string Of(ApplicationStatus status) => Statuses[status].Name;

    public static string Code(ApplicationStatus status) => Statuses[status].Code;

    public static ApplicationStatus Parse(string code) =>
        Statuses.First(p => p.Value.Code == code).Key;

    public static string Of(Role role) => RoleNames[role];

    public static string Of(Multiplicity multiplicity) => multiplicity switch
    {
        Multiplicity.Single => "1",
        Multiplicity.Double => "2",
        _ => "ШМ/MULT",
    };

    public static string Of(VisaState state) =>
        state == VisaState.Valid ? "Действительна" : "Аннулирована";
}
