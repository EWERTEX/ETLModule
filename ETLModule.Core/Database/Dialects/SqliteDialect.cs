using System.Text;
using ETLModule.Core.Models;

namespace ETLModule.Core.Database.Dialects;

/// <summary>
/// Реализация SQL-диалекта для SQLite.
/// </summary>
public class SqliteDialect : ISqlDialect
{
    public string EscapeIdentifier(string identifier)
    {
        // В SQLite безопаснее всего оборачивать идентификаторы в двойные кавычки
        return $"\"{identifier}\"";
    }

    public string GetSqlDataType(Type type)
    {
        // Базовый маппинг типов (можно расширять при необходимости)
        if (type == typeof(int) || type == typeof(long) || type == typeof(bool))
            return "INTEGER";
        if (type == typeof(float) || type == typeof(double) || type == typeof(decimal))
            return "REAL";
        
        return "TEXT";
    }

    public string GetTableListQuery()
    {
        // Системная таблица SQLite. Исключает системные таблицы самого движка (начинаются с sqlite_)
        return "SELECT name FROM sqlite_master WHERE type='table' AND name NOT LIKE 'sqlite_%';";
    }

    public string BuildCreateTableQuery(string tableName, Dictionary<string, Type> columns)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"CREATE TABLE {EscapeIdentifier(tableName)} (");

        var columnDefinitions = columns.Select(kvp =>
        {
            var colDef = $"    {EscapeIdentifier(kvp.Key)} {GetSqlDataType(kvp.Value)}";
            // Автоматическое назначение первичного ключа
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

        switch (policy)
        {
            case ImportPolicy.Fail:
                return $"INSERT INTO {EscapeIdentifier(tableName)} ({columnsString}) VALUES ({parametersString});";
            case ImportPolicy.Ignore:
                // Специфичная для SQLite конструкция пропуска конфликтов
                return $"INSERT OR IGNORE INTO {EscapeIdentifier(tableName)} ({columnsString}) VALUES ({parametersString});";
            // Update (Upsert)
            case ImportPolicy.Update:
            default:
            {
                var idCol = cols.First(c => c.Equals("Id", StringComparison.OrdinalIgnoreCase));
                var updateCols = cols.Where(c => c != idCol).ToList();
            
                var upsertQuery = $"INSERT INTO {EscapeIdentifier(tableName)} ({columnsString}) VALUES ({parametersString}) " +
                                  $"ON CONFLICT({EscapeIdentifier(idCol)}) DO UPDATE SET ";
                              
                var setAssignments = updateCols.Select(c => $"{EscapeIdentifier(c)} = excluded.{EscapeIdentifier(c)}");
                upsertQuery += string.Join(", ", setAssignments) + ";";
            
                return upsertQuery;
            }
        }
    }
}