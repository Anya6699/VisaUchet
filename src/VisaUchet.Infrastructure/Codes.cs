using VisaUchet.Domain;

namespace VisaUchet.Infrastructure;

// Mapping of enumerations to storage codes used in the database CHECK constraints
internal static class Codes
{
    private static readonly IReadOnlyDictionary<Role, string> RoleCodes = new Dictionary<Role, string>
    {
        [Role.Operator] = "OPERATOR",
        [Role.ConsulOfficer] = "CONSUL_OFFICER",
        [Role.Head] = "HEAD",
        [Role.Admin] = "ADMIN",
    };

    public static string Of(ApplicationStatus status) => StatusNames.Code(status);

    public static ApplicationStatus ToStatus(string code) => StatusNames.Parse(code.Trim());

    public static string Of(Role role) => RoleCodes[role];

    public static Role ToRole(string code) => RoleCodes.First(p => p.Value == code.Trim()).Key;

    public static string Of(Multiplicity value) => value.ToString().ToUpperInvariant();

    public static Multiplicity ToMultiplicity(string code) => Enum.Parse<Multiplicity>(code.Trim(), ignoreCase: true);

    public static string Of(VisaType value) => value.ToString();

    public static VisaType ToVisaType(string code) => Enum.Parse<VisaType>(code.Trim());

    public static string Of(Sex value) => value.ToString();

    public static Sex ToSex(string code) => Enum.Parse<Sex>(code.Trim());

    public static string Of(VisaState value) => value == VisaState.Valid ? "VALID" : "CANCELLED";

    public static VisaState ToState(string code) => code.Trim() == "VALID" ? VisaState.Valid : VisaState.Cancelled;
}
