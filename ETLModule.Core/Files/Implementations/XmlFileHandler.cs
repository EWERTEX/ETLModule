using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;
using ETLModule.Core.Files.Interfaces;

namespace ETLModule.Core.Files.Implementations;

/// <summary>
/// Обеспечивает функциональность для импорта и экспорта табличных данных в формате XML.
/// Реализует автоматическое преобразование плоских структур данных в иерархическое дерево узлов и обратно.
/// </summary>
public partial class XmlFileHandler : IFileImporter, IFileExporter
{
    private const string RootNodeName = "ExportedData";
    private const string RowNodeName = "Item";

    /// <inheritdoc />
    public async Task<IEnumerable<Dictionary<string, string>>> ImportAsync(string filePath)
    {
        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException($"Файл для импорта не найден: {filePath}");
        }

        var result = new List<Dictionary<string, string>>();

        await using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, true);
        
        // Осуществляется асинхронная загрузка дерева документа
        var document = await XDocument.LoadAsync(stream, LoadOptions.None, CancellationToken.None);
        
        if (document.Root == null)
        {
            throw new FormatException("XML файл не содержит корневого элемента.");
        }

        // Обход всех дочерних элементов корневого узла (каждый элемент трактуется как строка данных)
        foreach (var rowElement in document.Root.Elements())
        {
            var row = new Dictionary<string, string>();
            
            // Извлечение значений из вложенных тегов, представляющих колонки
            foreach (var columnElement in rowElement.Elements())
            {
                row[columnElement.Name.LocalName] = columnElement.Value;
            }
            
            result.Add(row);
        }

        return result;
    }

    /// <inheritdoc />
    public async Task ExportAsync(string filePath, IEnumerable<Dictionary<string, object?>> data)
    {
        // Создание базовой структуры XML-документа с объявлением кодировки UTF-8
        var document = new XDocument(new XDeclaration("1.0", "utf-8", "yes"));
        var rootElement = new XElement(RootNodeName);
        document.Add(rootElement);

        foreach (var row in data)
        {
            var rowElement = new XElement(RowNodeName);

            foreach (var kvp in row)
            {
                var safeNodeName = SanitizeXmlNodeName(kvp.Key);
                
                var nodeValue = kvp.Value switch
                {
                    null => string.Empty,
                    DateTime dt => dt.ToString("yyyy-MM-dd HH:mm:ss", System.Globalization.CultureInfo.InvariantCulture),
                    IFormattable f => f.ToString(null, System.Globalization.CultureInfo.InvariantCulture),
                    _ => kvp.Value.ToString() ?? string.Empty
                };
                
                rowElement.Add(new XElement(safeNodeName, nodeValue));
            }

            rootElement.Add(rowElement);
        }

        await using var stream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None, 4096, true);
        
        // Настройка параметров записи для генерации человекочитаемого формата (с отступами)
        var settings = new XmlWriterSettings 
        { 
            Async = true, 
            Indent = true 
        };
        
        await using var writer = XmlWriter.Create(stream, settings);
        
        await document.WriteToAsync(writer, CancellationToken.None);
        await writer.FlushAsync();
    }

    /// <summary>
    /// Приводит исходную строку к допустимому формату имени XML-узла.
    /// Удаляет запрещенные символы, пробелы, а также корректирует первый символ, если это цифра.
    /// </summary>
    /// <param name="name">Исходное имя колонки из базы данных.</param>
    /// <returns>Строка, валидная для использования в качестве элемента XML.</returns>
    private static string SanitizeXmlNodeName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return "UnknownColumn";
        }

        // Выполняется замена всех символов, кроме букв английского алфавита, цифр и нижнего подчеркивания
        var sanitized = MyRegex().Replace(name, "");

        if (string.IsNullOrEmpty(sanitized))
        {
            return "Column";
        }

        // Согласно стандарту XML, имя тега не может начинаться с цифры
        if (char.IsDigit(sanitized[0]))
        {
            sanitized = "_" + sanitized;
        }

        return sanitized;
    }

    [GeneratedRegex(@"[^a-zA-Z0-9_]")]
    private static partial Regex MyRegex();
}