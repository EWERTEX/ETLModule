using System.Data.Common;
using Microsoft.Data.Sqlite;

namespace DbExportModule.Core.Database.Factories;

/// <summary>
/// Реализация фабрики подключений для СУБД SQLite.
/// </summary>
public class SqliteConnectionFactory : IDbConnectionFactory
{
    private readonly string _connectionString;

    /// <summary>
    /// Инициализирует новый экземпляр фабрики подключений SQLite.
    /// </summary>
    /// <param name="connectionString">Строка подключения к БД (например, "Data Source=ExportDatabase.db").</param>
    /// <exception cref="ArgumentException">Выбрасывается, если строка подключения пуста.</exception>
    public SqliteConnectionFactory(string connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new ArgumentException("Строка подключения не может быть пустой или состоять из пробелов.", nameof(connectionString));
        }

        _connectionString = connectionString;
    }

    /// <summary>
    /// Создает новое подключение <see cref="SqliteConnection"/> на основе переданной строки подключения.
    /// </summary>
    /// <returns>Готовое, но закрытое подключение к SQLite.</returns>
    public DbConnection CreateConnection()
    {
        // Создаем конкретный объект SqliteConnection, 
        // но возвращаем его под "маской" базового класса DbConnection
        return new SqliteConnection(_connectionString);
    }
}