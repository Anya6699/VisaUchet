using VisaUchet.Application;

namespace VisaUchet.Tests;

public class PasswordHasherTests
{
    // Hash of the demo administrator from database/02_seed.sql
    private const string SeedAdminHash = "PBKDF2$100000$1iYv+An0HogIiGTPazie6A==$RgnjHGTcKYYxfWxH20pzxBNRNeq2yB7VB5nVpMEEu9k=";

    [Fact]
    public void Hash_ThenVerify_Succeeds()
    {
        var hash = PasswordHasher.Hash("Secret#123");

        Assert.True(PasswordHasher.Verify("Secret#123", hash));
        Assert.False(PasswordHasher.Verify("secret#123", hash));
    }

    [Fact]
    public void Hash_SamePassword_UsesDifferentSalt() =>
        Assert.NotEqual(PasswordHasher.Hash("Secret#123"), PasswordHasher.Hash("Secret#123"));

    [Fact]
    public void Verify_SeedAdministrator_Succeeds() => Assert.True(PasswordHasher.Verify("Admin#2026", SeedAdminHash));

    [Theory]
    [InlineData("")]
    [InlineData("plain-text")]
    [InlineData("PBKDF2$x$y$z")]
    public void Verify_MalformedHash_ReturnsFalse(string stored) => Assert.False(PasswordHasher.Verify("Admin#2026", stored));
}

public class AuthServiceTests
{
    private readonly InMemoryEmployeeRepository _employees = new();
    private readonly FixedClock _clock = new();
    private readonly MemoryLog _log = new();
    private readonly AuthService _auth;

    public AuthServiceTests()
    {
        _employees.Add(new Employee { Login = "consul", FullName = "Петров П. П.", Role = Role.ConsulOfficer, PasswordHash = PasswordHasher.Hash("Consul#2026") });
        _auth = new AuthService(_employees, _clock, _log);
    }

    [Fact]
    public void Login_CorrectPassword_ReturnsEmployee() =>
        Assert.Equal(Role.ConsulOfficer, _auth.Login("consul", "Consul#2026").Role);

    [Fact]
    public void Login_FiveWrongAttempts_LocksAccount()
    {
        for (var i = 0; i < Employee.MaxFailedAttempts; i++)
        {
            Assert.Throws<DomainException>(() => _auth.Login("consul", "wrong"));
        }

        var error = Assert.Throws<DomainException>(() => _auth.Login("consul", "Consul#2026"));
        Assert.Contains("неудачных попыток", error.Message);
    }

    [Fact]
    public void Login_AfterLockoutExpires_Succeeds()
    {
        for (var i = 0; i < Employee.MaxFailedAttempts; i++)
        {
            Assert.Throws<DomainException>(() => _auth.Login("consul", "wrong"));
        }

        _clock.Now += Employee.LockoutDuration + TimeSpan.FromMinutes(1);

        Assert.NotNull(_auth.Login("consul", "Consul#2026"));
    }

    [Fact]
    public void Login_UnknownUser_SameMessageAsWrongPassword() =>
        Assert.Equal(AuthService.InvalidCredentials, Assert.Throws<DomainException>(() => _auth.Login("nobody", "x")).Message);

    [Fact]
    public void Login_NeverWritesPasswordToLog()
    {
        Assert.Throws<DomainException>(() => _auth.Login("consul", "wrong-secret"));
        _auth.Login("consul", "Consul#2026");

        Assert.DoesNotContain(_log.Messages, m => m.Contains("wrong-secret") || m.Contains("Consul#2026"));
    }

    [Theory]
    [InlineData(Role.Operator, FormCode.Registration, true)]
    [InlineData(Role.Operator, FormCode.StatusChange, false)]
    [InlineData(Role.ConsulOfficer, FormCode.VisaJournal, true)]
    [InlineData(Role.Head, FormCode.Report, true)]
    [InlineData(Role.Admin, FormCode.ApplicationCard, false)]
    public void CanOpen_FollowsRoleMatrix(Role role, FormCode form, bool expected)
    {
        var employee = new Employee { Login = "x", FullName = "x", Role = role, PasswordHash = string.Empty };

        Assert.Equal(expected, employee.CanOpen(form));
    }
}
