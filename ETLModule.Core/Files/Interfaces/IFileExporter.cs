namespace DbExportModule.Core.Files.Interfaces;

/// <summary>
/// Определяет контракт для экспорта (записи) типизированных данных в различные файловые форматы.
/// </summary>
public interface IFileExporter
{
    /// <summary>
    /// Асинхронно сохраняет коллекцию типизированных данных в файл заданного формата.
    /// </summary>
    /// <param name="filePath">Путь, по которому будет создан или перезаписан целевой файл.</param>
    /// <param name="data">Коллекция типизированных данных, полученная из базы данных.</param>
    /// <returns>Задача, представляющая асинхронную операцию записи.</returns>
    /// <exception cref="System.IO.IOException">Генерируется при ошибках доступа к файловой системе.</exception>
    Task ExportAsync(string filePath, IEnumerable<Dictionary<string, object?>> data);
}