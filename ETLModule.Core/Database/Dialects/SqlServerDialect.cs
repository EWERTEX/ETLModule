using System.Text;
using ETLModule.Core.Models;

namespace ETLModule.Core.Database.Dialects;

/// <summary>
/// Реализация SQL-диалекта для Microsoft SQL Server.
/// </summary>
public class SqlServerDialect : ISqlDialect
{
    /// <inheritdoc />
    public string EscapeIdentifier(string identifier)
    {
        // В SQL Server стандарт экранирования - квадратные скобки
        return $"[{identifier}]";
    }

    /// <inheritdoc />
    public string GetSqlDataType(Type type)
    {
        if (type == typeof(Guid)) return "UNIQUEIDENTIFIER";
        if (type == typeof(int)) return "INT";
        if (type == typeof(long)) return "BIGINT";
        if (type == typeof(bool)) return "BIT";
        if (type == typeof(float)) return "REAL";
        if (type == typeof(double)) return "FLOAT";
        if (type == typeof(decimal)) return "DECIMAL(18,4)";
        return type == typeof(DateTime) ? "DATETIME2" : "NVARCHAR(MAX)";
    }

    /// <inheritdoc />
    public string GetTableListQuery()
    {
        return "SELECT TABLE_NAME FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_TYPE = 'BASE TABLE';";
    }

   /// <inheritdoc />
    public string BuildCreateTableQuery(string tableName, Dictionary<string, Type> columns)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"CREATE TABLE {EscapeIdentifier(tableName)} (");

        var columnDefinitions = columns.Select(kvp =>
        {
            var colDef = $"    {EscapeIdentifier(kvp.Key)} {GetSqlDataType(kvp.Value)}";
            // Назначение первичного ключа при обнаружении колонки с идентификатором Id.
            if (kvp.Key.Equals("Id", StringComparison.OrdinalIgnoreCase))
                colDef += " PRIMARY KEY";
            return colDef;
        });

        sb.AppendLine(string.Join(",\n", columnDefinitions));
        sb.AppendLine(");");

        return sb.ToString();
    }

    /// <inheritdoc />
    public string BuildInsertQuery(string tableName, IEnumerable<string> columnNames, ImportPolicy policy)
    {
        var cols = columnNames.ToList();
        var escapedTableName = EscapeIdentifier(tableName);
        var columnsString = string.Join(", ", cols.Select(EscapeIdentifier));
        var parametersString = string.Join(", ", cols.Select(c => $"@{c}"));

        // Реализация базовой вставки для режима генерации исключений при конфликтах.
        if (policy == ImportPolicy.Fail)
        {
            return $"INSERT INTO {escapedTableName} ({columnsString}) VALUES ({parametersString});";
        }

        var idCol = cols.First(c => c.Equals("Id", StringComparison.OrdinalIgnoreCase));
        var otherCols = cols.Where(c => c != idCol).ToList();

        // Формирование запроса с использованием оператора MERGE для реализации стратегий Ignore и Update.
        var sb = new StringBuilder();
        sb.AppendLine($"MERGE INTO {escapedTableName} AS Target");
        // В качестве источника используется виртуальная таблица из одного ряда параметров.
        sb.AppendLine($"USING (SELECT {string.Join(", ", cols.Select(c => $"@{c} AS {EscapeIdentifier(c)}"))}) AS Source");
        sb.AppendLine($"ON Target.{EscapeIdentifier(idCol)} = Source.{EscapeIdentifier(idCol)}");

        if (policy == ImportPolicy.Update)
        {
            // При совпадении идентификаторов выполняется обновление всех значимых полей.
            sb.AppendLine("WHEN MATCHED THEN");
            var updateSet = string.Join(", ", otherCols.Select(c => $"{EscapeIdentifier(c)} = Source.{EscapeIdentifier(c)}"));
            sb.AppendLine($"    UPDATE SET {updateSet}");
        }

        // При отсутствии совпадения выполняется стандартная вставка новой записи.
        sb.AppendLine("WHEN NOT MATCHED THEN");
        sb.AppendLine($"    INSERT ({columnsString})");
        sb.AppendLine($"    VALUES ({string.Join(", ", cols.Select(c => $"Source.{EscapeIdentifier(c)}"))});");

        return sb.ToString();
    }
}