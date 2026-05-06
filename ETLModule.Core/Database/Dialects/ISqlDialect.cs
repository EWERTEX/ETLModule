namespace ETLModule.Core.Database.Dialects;

/// <summary>
/// Интерфейс диалекта SQL. 
/// Изолирует специфику синтаксиса конкретной СУБД (экранирование, типы данных, системные запросы).
/// </summary>
public interface ISqlDialect
{
    /// <summary>
    /// Экранирует имя таблицы или колонки (например, "Name" в SQLite или [Name] в SQL Server),
    /// чтобы избежать ошибок с зарезервированными словами.
    /// </summary>
    string EscapeIdentifier(string identifier);

    /// <summary>
    /// Сопоставляет C# тип данных (Type) с физическим типом колонки в базе данных.
    /// </summary>
    string GetSqlDataType(Type type);

    /// <summary>
    /// Возвращает SQL-запрос для получения списка всех пользовательских таблиц в базе.
    /// </summary>
    string GetTableListQuery();

    /// <summary>
    /// Генерирует DDL-запрос (CREATE TABLE) на основе имени и набора колонок с их типами.
    /// </summary>
    string BuildCreateTableQuery(string tableName, Dictionary<string, Type> columns);

    /// <summary>
    /// Генерирует DML-запрос (INSERT) с параметрами для безопасной вставки данных.
    /// </summary>
    string BuildInsertQuery(string tableName, IEnumerable<string> columnNames, Models.ImportPolicy policy);
}