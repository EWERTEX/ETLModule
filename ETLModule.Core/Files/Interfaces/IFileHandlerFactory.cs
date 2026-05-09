namespace ETLModule.Core.Files.Interfaces;

/// <summary>
/// Фабрика для получения нужных обработчиков импорта и экспорта на основе расширения файла.
/// </summary>
public interface IFileHandlerFactory
{
    /// <summary>
    /// Возвращает пару обработчиков (импортер и экспортер) для указанного пути к файлу.
    /// </summary>
    /// <param name="filePath">Путь к файлу (расширение извлекается автоматически).</param>
    (IFileImporter Importer, IFileExporter Exporter) GetHandlers(string filePath);
}