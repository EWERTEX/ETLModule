using System.Data.Common;
using Microsoft.Data.SqlClient;

namespace DbExportModule.Core.Database.Factories;

/// <summary>
/// Реализация фабрики подключений для Microsoft SQL Server.
/// </summary>
public class SqlServerConnectionFactory : IDbConnectionFactory
{
    private readonly string _connectionString;

    /// <summary>
    /// Инициализирует новый экземпляр фабрики подключений MS SQL Server.
    /// </summary>
    /// <param name="connectionString">Строка подключения к БД.</param>
    /// <exception cref="ArgumentException">Выбрасывается, если строка подключения пуста.</exception>
    public SqlServerConnectionFactory(string connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new ArgumentException("Строка подключения не может быть пустой или состоять из пробелов.", nameof(connectionString));
        }

        _connectionString = connectionString;
    }

    /// <summary>
    /// Создает новое подключение <see cref="SqlConnection"/> на основе переданной строки подключения.
    /// </summary>
    /// <returns>Готовое, но закрытое подключение к MS SQL Server.</returns>
    public DbConnection CreateConnection()
    {
        // Возвращаем специфичное для SQL Server подключение под видом базового DbConnection
        return new SqlConnection(_connectionString);
    }
}