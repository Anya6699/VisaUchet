using VisaUchet.Domain;

namespace VisaUchet.Application;

// AUT-01, SEC-02: login with lockout after 5 failed attempts
public sealed class AuthService
{
    public const string InvalidCredentials = "Неверный логин или пароль.";

    private readonly IEmployeeRepository _employees;
    private readonly IClock _clock;
    private readonly IAppLog _log;

    public AuthService(IEmployeeRepository employees, IClock clock, IAppLog log)
    {
        _employees = employees ?? throw new ArgumentNullException(nameof(employees));
        _clock = clock ?? throw new ArgumentNullException(nameof(clock));
        _log = log ?? throw new ArgumentNullException(nameof(log));
    }

    public Employee Login(string login, string password)
    {
        if (string.IsNullOrWhiteSpace(login) || string.IsNullOrEmpty(password))
        {
            throw new DomainException(InvalidCredentials);
        }

        var employee = _employees.FindByLogin(login.Trim()) ?? throw new DomainException(InvalidCredentials);
        var now = _clock.Now;
        EnsureCanLogin(employee, now);
        if (!employee.VerifyPassword(password))
        {
            employee.RegisterFailedLogin(now);
            _employees.Update(employee);
            _log.Info($"Неудачный вход: {employee.Login}");
            throw new DomainException(InvalidCredentials);
        }

        employee.RegisterSuccessfulLogin();
        _employees.Update(employee);
        _log.Info($"Вход: {employee.Login}");
        return employee;
    }

    private static void EnsureCanLogin(Employee employee, DateTime now)
    {
        if (employee.IsBlocked)
        {
            throw new DomainException("Учётная запись заблокирована. Обратитесь к администратору.");
        }

        if (employee.IsLockedOut(now))
        {
            throw new DomainException($"Слишком много неудачных попыток. Повторите после {employee.LockedUntil:HH:mm}.");
        }
    }
}

// AUT-02: administrator creates and blocks accounts and assigns roles
public sealed class UserAdminService
{
    public const int MinPasswordLength = 8;

    private readonly IEmployeeRepository _employees;
    private readonly IAppLog _log;

    public UserAdminService(IEmployeeRepository employees, IAppLog log)
    {
        _employees = employees ?? throw new ArgumentNullException(nameof(employees));
        _log = log ?? throw new ArgumentNullException(nameof(log));
    }

    public IReadOnlyList<Employee> List() => _employees.All();

    public void Create(string login, string fullName, Role role, string password)
    {
        var result = new ValidationResult();
        result.Require(!string.IsNullOrWhiteSpace(login), "Login", "Введите логин.");
        result.Require(!string.IsNullOrWhiteSpace(fullName), "FullName", "Введите ФИО.");
        result.Require(password?.Length >= MinPasswordLength, "Password", $"Пароль не короче {MinPasswordLength} символов.");
        result.ThrowIfInvalid();
        if (_employees.FindByLogin(login.Trim()) is not null)
        {
            throw new DomainException($"Логин {login} уже занят.");
        }

        _employees.Add(new Employee
        {
            Login = login.Trim(),
            FullName = fullName.Trim(),
            Role = role,
            PasswordHash = PasswordHasher.Hash(password!),
        });
        _log.Info($"Создана учётная запись {login.Trim()}");
    }

    public void SetBlocked(string login, bool blocked) => Modify(login, e => e.IsBlocked = blocked);

    public void SetRole(string login, Role role) => Modify(login, e => e.Role = role);

    private void Modify(string login, Action<Employee> change)
    {
        var employee = _employees.FindByLogin(login) ?? throw new DomainException($"Сотрудник {login} не найден.");
        change(employee);
        _employees.Update(employee);
        _log.Info($"Изменена учётная запись {login}");
    }
}
