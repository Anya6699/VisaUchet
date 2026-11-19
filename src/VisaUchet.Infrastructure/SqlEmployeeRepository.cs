using System.Data;
using System.Data.Common;
using VisaUchet.Application;
using VisaUchet.Domain;

namespace VisaUchet.Infrastructure;

public sealed class SqlEmployeeRepository : IEmployeeRepository
{
    private const string Columns = "Login, Full_Name, Role_Code, Password_Hash, Is_Blocked, Failed_Attempts, Locked_Until";

    private readonly DbConnectionFactory _connections;

    public SqlEmployeeRepository(DbConnectionFactory connections) =>
        _connections = connections ?? throw new ArgumentNullException(nameof(connections));

    public Employee? FindByLogin(string login) =>
        Read($"SELECT {Columns} FROM Employee WHERE Login = @login", login).SingleOrDefault();

    public IReadOnlyList<Employee> All() => Read($"SELECT {Columns} FROM Employee ORDER BY Login", null);

    public void Add(Employee employee)
    {
        ArgumentNullException.ThrowIfNull(employee);
        Execute(
            $"INSERT INTO Employee ({Columns}) VALUES (@login, @name, @role, @hash, @blocked, @failed, @locked)",
            employee);
    }

    public void Update(Employee employee)
    {
        ArgumentNullException.ThrowIfNull(employee);
        Execute(
            "UPDATE Employee SET Full_Name = @name, Role_Code = @role, Password_Hash = @hash, Is_Blocked = @blocked, " +
            "Failed_Attempts = @failed, Locked_Until = @locked WHERE Login = @login",
            employee);
    }

    private void Execute(string sql, Employee employee)
    {
        using var connection = _connections.Open();
        using var command = Db.Command(connection, sql)
            .With("@login", employee.Login).With("@name", employee.FullName).With("@role", Codes.Of(employee.Role))
            .With("@hash", employee.PasswordHash).With("@blocked", employee.IsBlocked)
            .With("@failed", employee.FailedAttempts).With("@locked", employee.LockedUntil, DbType.DateTime2);
        command.ExecuteNonQuery();
    }

    private List<Employee> Read(string sql, string? login)
    {
        using var connection = _connections.Open();
        using var command = Db.Command(connection, sql);
        if (login is not null)
        {
            command.With("@login", login);
        }

        using var reader = command.ExecuteReader();
        var result = new List<Employee>();
        while (reader.Read())
        {
            result.Add(Map(reader));
        }

        return result;
    }

    private static Employee Map(DbDataReader reader) => new()
    {
        Login = reader.Text("Login"),
        FullName = reader.Text("Full_Name"),
        Role = Codes.ToRole(reader.Text("Role_Code")),
        PasswordHash = reader.Text("Password_Hash"),
        IsBlocked = reader.Get<bool>("Is_Blocked"),
        FailedAttempts = reader.Get<int>("Failed_Attempts"),
        LockedUntil = reader.IsDBNull(reader.GetOrdinal("Locked_Until")) ? null : reader.Get<DateTime>("Locked_Until"),
    };
}

public sealed class SqlHolidayRepository : IHolidayRepository
{
    private readonly DbConnectionFactory _connections;

    public SqlHolidayRepository(DbConnectionFactory connections) =>
        _connections = connections ?? throw new ArgumentNullException(nameof(connections));

    public IReadOnlyList<DateOnly> All()
    {
        using var connection = _connections.Open();
        using var command = Db.Command(connection, "SELECT Holiday_Date FROM Holiday");
        using var reader = command.ExecuteReader();
        var result = new List<DateOnly>();
        while (reader.Read())
        {
            result.Add(reader.GetDate("Holiday_Date"));
        }

        return result;
    }
}
