namespace ETLModule.Core.Files.Interfaces;

/// <summary>
/// Определяет контракт для импорта (чтения) данных из различных файловых форматов.
/// </summary>
public interface IFileImporter
{
    /// <summary>
    /// Асинхронно считывает данные из указанного файла и преобразует их в коллекцию словарей.
    /// </summary>
    /// <param name="filePath">Полный абсолютный или относительный путь к исходному файлу.</param>
    /// <returns>
    /// Коллекция словарей, где ключом является название колонки, 
    /// а значением — строковое представление содержимого ячейки. 
    /// Возвращает "сырые" нетипизированные данные для последующего анализа.
    /// </returns>
    /// <exception cref="System.IO.FileNotFoundException">Генерируется, если файл по указанному пути не найден.</exception>
    /// <exception cref="System.FormatException">Генерируется при нарушении структуры целевого формата файла.</exception>
    Task<IEnumerable<Dictionary<string, string>>> ImportAsync(string filePath);
}