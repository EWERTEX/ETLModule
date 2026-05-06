using DbExportModule.Core.Models;

namespace DbExportModule.Core.Repositories;

/// <summary>
/// Определяет контракт для динамического взаимодействия с хранилищем данных.
/// Позволяет выполнять операции определения структуры (DDL) и манипуляции данными (DML) 
/// без жесткой привязки к статическим моделям данных среды CLR.
/// </summary>
public interface IDynamicRepository
{
    /// <summary>
    /// Асинхронно извлекает список имен всех пользовательских таблиц, существующих в базе данных.
    /// </summary>
    /// <returns>Коллекция строковых идентификаторов таблиц.</returns>
    Task<IEnumerable<string>> GetTableNamesAsync();

    /// <summary>
    /// Асинхронно формирует и выполняет запрос на создание новой таблицы на основе предоставленной схемы.
    /// </summary>
    /// <param name="tableName">Идентификатор создаваемой таблицы.</param>
    /// <param name="schema">Словарь, сопоставляющий имена колонок с типами данных среды CLR.</param>
    Task CreateTableAsync(string tableName, Dictionary<string, Type> schema);

    /// <summary>
    /// Асинхронно выполняет пакетную вставку записей в указанную таблицу.
    /// </summary>
    /// <param name="tableName">Идентификатор целевой таблицы.</param>
    /// <param name="data">Коллекция записей, где каждая запись представлена словарем пар "имя колонки - значение".</param>
    /// <param name="policy">Политика обработки дубликатов по умолчанию.</param>
    Task InsertDataAsync(string tableName, IEnumerable<Dictionary<string, object?>> data, ImportPolicy policy = ImportPolicy.Fail);

    /// <summary>
    /// Асинхронно извлекает все записи из указанной таблицы для последующего экспорта.
    /// </summary>
    /// <param name="tableName">Идентификатор целевой таблицы.</param>
    /// <returns>Коллекция записей, представленных в виде словарей.</returns>
    Task<IEnumerable<Dictionary<string, object?>>> GetDataAsync(string tableName);
}