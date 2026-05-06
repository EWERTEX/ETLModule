using DbExportModule.Core.Models;
using DbExportModule.Core.Database.Dialects;
using DbExportModule.Core.Database.Factories;

namespace DbExportModule.Core.Repositories;

/// <summary>
/// Реализует механизм динамического доступа к данным посредством базовых абстракций ADO.NET.
/// Использует паттерн "Абстрактная фабрика" для управления подключениями и паттерн "Стратегия" для адаптации SQL-синтаксиса.
/// </summary>
public class DynamicRepository : IDynamicRepository
{
    private readonly IDbConnectionFactory _connectionFactory;
    private readonly ISqlDialect _dialect;

    /// <summary>
    /// Инициализирует новый экземпляр динамического репозитория с заданными зависимостями.
    /// </summary>
    /// <param name="connectionFactory">Фабрика для инициализации подключений к СУБД.</param>
    /// <param name="dialect">Провайдер SQL-диалекта для генерации специфичных запросов.</param>
    public DynamicRepository(IDbConnectionFactory connectionFactory, ISqlDialect dialect)
    {
        _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
        _dialect = dialect ?? throw new ArgumentNullException(nameof(dialect));
    }

    /// <inheritdoc />
    public async Task<IEnumerable<string>> GetTableNamesAsync()
    {
        var tables = new List<string>();
        var query = _dialect.GetTableListQuery();

        await using var connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync();

        await using var command = connection.CreateCommand();
        command.CommandText = query;

        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            tables.Add(reader.GetString(0));
        }

        return tables;
    }

    /// <inheritdoc />
    public async Task CreateTableAsync(string tableName, Dictionary<string, Type> schema)
    {
        // Поиск существующего идентификатора без учета регистра
        var idKey = schema.Keys.FirstOrDefault(k => k.Equals("Id", StringComparison.OrdinalIgnoreCase));
        
        // Если колонка первичного ключа отсутствует в исходных данных, 
        // схема принудительно дополняется системной колонкой типа Guid.
        if (idKey == null)
        {
            var newSchema = new Dictionary<string, Type> { { "Id", typeof(Guid) } };
            foreach (var kvp in schema)
            {
                newSchema.Add(kvp.Key, kvp.Value);
            }
            schema = newSchema;
        }

        var query = _dialect.BuildCreateTableQuery(tableName, schema);

        await using var connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = query;
        await command.ExecuteNonQueryAsync();
    }

    /// <inheritdoc />
    public async Task InsertDataAsync(string tableName, IEnumerable<Dictionary<string, object?>> data, ImportPolicy policy = ImportPolicy.Fail)
    {
        await using var connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync();
        await using var transaction = await connection.BeginTransactionAsync();

        try
        {
            foreach (var row in data)
            {
                // Проверка наличия значения первичного ключа в текущей записи.
                // При отсутствии генерируется уникальный идентификатор (UUID v4).
                var idKey = row.Keys.FirstOrDefault(k => k.Equals("Id", StringComparison.OrdinalIgnoreCase));
                if (idKey == null)
                {
                    row["Id"] = Guid.NewGuid();
                }

                await using var command = connection.CreateCommand();
                command.Transaction = transaction;
                
                // Передача политики в диалект для генерации специфичного запроса
                command.CommandText = _dialect.BuildInsertQuery(tableName, row.Keys, policy);

                foreach (var kvp in row)
                {
                    var parameter = command.CreateParameter();
                    parameter.ParameterName = $"@{kvp.Key}";
                    parameter.Value = kvp.Value ?? DBNull.Value;
                    command.Parameters.Add(parameter);
                }

                await command.ExecuteNonQueryAsync();
            }

            await transaction.CommitAsync();
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<IEnumerable<Dictionary<string, object?>>> GetDataAsync(string tableName)
    {
        var result = new List<Dictionary<string, object?>>();
        var escapedTableName = _dialect.EscapeIdentifier(tableName);
        var query = $"SELECT * FROM {escapedTableName};";

        await using var connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync();

        await using var command = connection.CreateCommand();
        command.CommandText = query;

        await using var reader = await command.ExecuteReaderAsync();
        
        while (await reader.ReadAsync())
        {
            var row = new Dictionary<string, object?>();
            for (var i = 0; i < reader.FieldCount; i++)
            {
                var columnName = reader.GetName(i);
                var value = reader.IsDBNull(i) ? null : reader.GetValue(i);
                row[columnName] = value;
            }
            result.Add(row);
        }

        return result;
    }
}