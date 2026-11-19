namespace VisaUchet.Domain;

// Employee account (SRS entity «Сотрудник», AUT-01, SEC-02)
public sealed class Employee
{
    public const int MaxFailedAttempts = 5;
    public static readonly TimeSpan LockoutDuration = TimeSpan.FromMinutes(15);

    private static readonly IReadOnlyDictionary<Role, FormCode[]> Access = new Dictionary<Role, FormCode[]>
    {
        [Role.Operator] = [FormCode.Registration, FormCode.ApplicationCard],
        [Role.ConsulOfficer] = [FormCode.StatusChange, FormCode.ApplicationCard, FormCode.VisaJournal],
        [Role.Head] = [FormCode.ApplicationCard, FormCode.VisaJournal, FormCode.Report],
        [Role.Admin] = [FormCode.Users],
    };

    public required string Login { get; init; }

    public required string FullName { get; set; }

    public Role Role { get; set; }

    public required string PasswordHash { get; set; }

    public bool IsBlocked { get; set; }

    public int FailedAttempts { get; set; }

    public DateTime? LockedUntil { get; set; }

    public bool CanOpen(FormCode form) => Access[Role].Contains(form);

    public bool VerifyPassword(string password) => PasswordHasher.Verify(password, PasswordHash);

    public bool IsLockedOut(DateTime now) => LockedUntil is { } until && until > now;

    public void RegisterFailedLogin(DateTime now)
    {
        FailedAttempts++;
        if (FailedAttempts >= MaxFailedAttempts)
        {
            LockedUntil = now + LockoutDuration;
            FailedAttempts = 0;
        }
    }

    public void RegisterSuccessfulLogin()
    {
        FailedAttempts = 0;
        LockedUntil = null;
    }
}
