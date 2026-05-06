using System.Data.Common;
using Npgsql;

namespace DbExportModule.Core.Database.Factories;

/// <summary>
/// Реализация фабрики подключений для СУБД PostgreSQL.
/// </summary>
public class PostgresConnectionFactory : IDbConnectionFactory
{
    private readonly string _connectionString;

    /// <summary>
    /// Инициализирует новый экземпляр фабрики подключений PostgreSQL.
    /// </summary>
    /// <param name="connectionString">Строка подключения к серверу базы данных.</param>
    /// <exception cref="ArgumentException">Генерируется, если строка подключения не задана.</exception>
    public PostgresConnectionFactory(string connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new ArgumentException("Строка подключения не может быть пустой.", nameof(connectionString));
        }

        _connectionString = connectionString;
    }

    /// <summary>
    /// Создает и возвращает новый экземпляр <see cref="NpgsqlConnection"/>.
    /// </summary>
    /// <returns>Объект подключения к PostgreSQL в закрытом состоянии.</returns>
    public DbConnection CreateConnection()
    {
        return new NpgsqlConnection(_connectionString);
    }
}