using System.Text;
using DbExportModule.Core.Models;

namespace DbExportModule.Core.Database.Dialects;

/// <summary>
/// Реализация SQL-диалекта для PostgreSQL.
/// </summary>
public class PostgresDialect : ISqlDialect
{
    /// <inheritdoc />
    public string EscapeIdentifier(string identifier)
    {
        // В PostgreSQL идентификаторы заключаются в двойные кавычки для сохранения регистра.
        return $"\"{identifier}\"";
    }

    /// <inheritdoc />
    public string GetSqlDataType(Type type)
    {
        if (type == typeof(int)) return "INTEGER";
        if (type == typeof(long)) return "BIGINT";
        if (type == typeof(bool)) return "BOOLEAN";
        if (type == typeof(float)) return "REAL";
        if (type == typeof(double)) return "DOUBLE PRECISION";
        if (type == typeof(decimal)) return "NUMERIC(18,4)";
        if (type == typeof(Guid)) return "UUID";
        return type == typeof(DateTime) ? "TIMESTAMP" : "TEXT";
    }

    /// <inheritdoc />
    public string GetTableListQuery()
    {
        // Запрос к информационной схеме для получения таблиц в публичной схеме.
        return "SELECT table_name FROM information_schema.tables WHERE table_schema = 'public' AND table_type = 'BASE TABLE';";
    }

    public string BuildCreateTableQuery(string tableName, Dictionary<string, Type> columns)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"CREATE TABLE {EscapeIdentifier(tableName)} (");

        var columnDefinitions = columns.Select(kvp =>
        {
            var colDef = $"    {EscapeIdentifier(kvp.Key)} {GetSqlDataType(kvp.Value)}";
            if (kvp.Key.Equals("Id", StringComparison.OrdinalIgnoreCase))
                colDef += " PRIMARY KEY";
            return colDef;
        });

        sb.AppendLine(string.Join(",\n", columnDefinitions));
        sb.AppendLine(");");
        return sb.ToString();
    }

    public string BuildInsertQuery(string tableName, IEnumerable<string> columnNames, ImportPolicy policy)
    {
        var cols = columnNames.ToList();
        var columnsString = string.Join(", ", cols.Select(EscapeIdentifier));
        var parametersString = string.Join(", ", cols.Select(c => $"@{c}"));
        var baseInsert = $"INSERT INTO {EscapeIdentifier(tableName)} ({columnsString}) VALUES ({parametersString})";

        if (policy == ImportPolicy.Fail) return baseInsert + ";";

        var idCol = cols.First(c => c.Equals("Id", StringComparison.OrdinalIgnoreCase));

        if (policy == ImportPolicy.Ignore)
        {
            // Специфичная для PostgreSQL конструкция разрешения конфликтов
            return baseInsert + $" ON CONFLICT ({EscapeIdentifier(idCol)}) DO NOTHING;";
        }

        // Update
        var updateCols = cols.Where(c => c != idCol).ToList();
        var setAssignments = updateCols.Select(c => $"{EscapeIdentifier(c)} = EXCLUDED.{EscapeIdentifier(c)}");
        return baseInsert + $" ON CONFLICT ({EscapeIdentifier(idCol)}) DO UPDATE SET {string.Join(", ", setAssignments)};";
    }
}