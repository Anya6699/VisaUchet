using System.Data;
using System.Data.Common;

namespace VisaUchet.Infrastructure;

// Creates open connections for the configured ADO.NET provider (SQL Server in production)
public sealed class DbConnectionFactory
{
    private readonly DbProviderFactory _provider;
    private readonly string _connectionString;

    public DbConnectionFactory(DbProviderFactory provider, string connectionString)
    {
        _provider = provider ?? throw new ArgumentNullException(nameof(provider));
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);
        _connectionString = connectionString;
    }

    public DbConnection Open()
    {
        var connection = _provider.CreateConnection()
            ?? throw new InvalidOperationException("Поставщик данных не создал подключение.");
        connection.ConnectionString = _connectionString;
        connection.Open();
        return connection;
    }
}

// Helpers for parameterized commands (SEC-03) and typed reading
internal static class Db
{
    public static DbCommand Command(DbConnection connection, string sql, DbTransaction? transaction = null)
    {
        var command = connection.CreateCommand();
        command.CommandText = sql;
        command.Transaction = transaction;
        return command;
    }

    public static DbCommand With(this DbCommand command, string name, object? value, DbType? type = null)
    {
        var parameter = command.CreateParameter();
        parameter.ParameterName = name;
        parameter.Value = Convert(value);
        if (type is { } dbType)
        {
            parameter.DbType = dbType;
        }
        else if (value is DateOnly)
        {
            parameter.DbType = DbType.Date;
        }

        command.Parameters.Add(parameter);
        return command;
    }

    public static DateOnly GetDate(this DbDataReader reader, string column) =>
        DateOnly.FromDateTime(reader.GetDateTime(reader.GetOrdinal(column)));

    public static DateOnly? GetNullableDate(this DbDataReader reader, string column) =>
        reader.IsDBNull(reader.GetOrdinal(column)) ? null : reader.GetDate(column);

    public static string? GetNullableString(this DbDataReader reader, string column) =>
        reader.IsDBNull(reader.GetOrdinal(column)) ? null : reader.GetString(reader.GetOrdinal(column));

    public static string Text(this DbDataReader reader, string column) =>
        reader.GetString(reader.GetOrdinal(column)).Trim();

    public static T Get<T>(this DbDataReader reader, string column) =>
        reader.GetFieldValue<T>(reader.GetOrdinal(column));

    // Escapes LIKE wildcards in user input for SQL Server
    public static string LikePrefix(string text) =>
        text.Replace("[", "[[]").Replace("%", "[%]").Replace("_", "[_]") + "%";

    private static object Convert(object? value) => value switch
    {
        null => DBNull.Value,
        DateOnly date => date.ToDateTime(TimeOnly.MinValue),
        _ => value,
    };
}
