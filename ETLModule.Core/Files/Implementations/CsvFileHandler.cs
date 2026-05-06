using System.Text;
using ETLModule.Core.Files.Interfaces;

namespace ETLModule.Core.Files.Implementations;

/// <summary>
/// Обеспечивает функциональность для чтения и записи данных в формате CSV (Comma-Separated Values).
/// Реализует стандарты экранирования спецсимволов (запятых и кавычек) внутри строковых значений.
/// </summary>
public class CsvFileHandler : IFileImporter, IFileExporter
{
    private const string Separator = ",";
    private const string Quote = "\"";
    private const string EscapedQuote = "\"\"";

    /// <inheritdoc />
    public async Task<IEnumerable<Dictionary<string, string>>> ImportAsync(string filePath)
    {
        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException($"Файл для импорта не найден: {filePath}");
        }

        var result = new List<Dictionary<string, string>>();

        // Использование StreamReader для потокового чтения больших файлов без переполнения памяти
        using var reader = new StreamReader(filePath, Encoding.UTF8);
        
        var headerLine = await reader.ReadLineAsync();
        if (string.IsNullOrWhiteSpace(headerLine))
        {
            throw new FormatException("CSV файл пуст или не содержит строки заголовков.");
        }

        var headers = ParseCsvLine(headerLine);

        while (await reader.ReadLineAsync() is { } dataLine)
        {
            if (string.IsNullOrWhiteSpace(dataLine)) continue;

            var values = ParseCsvLine(dataLine);
            var row = new Dictionary<string, string>();

            for (var i = 0; i < headers.Count; i++)
            {
                // Защита от ситуации, когда в строке данных меньше колонок, чем в заголовке
                row[headers[i]] = i < values.Count ? values[i] : string.Empty;
            }

            result.Add(row);
        }

        return result;
    }
    
    /// <inheritdoc />
    public async Task ExportAsync(string filePath, IEnumerable<Dictionary<string, object?>> data)
    {
        var encoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: true);

        var dataList = data.ToList();

        if (dataList.Count == 0)
        {
            await File.WriteAllTextAsync(filePath, string.Empty, encoding);
            return;
        }
        
        // Извлекаются заголовки колонок из первого словаря
        var headers = dataList.First().Keys.ToList();
        
        await using var writer = new StreamWriter(filePath, false, encoding);

        // Запись строки заголовков
        await writer.WriteLineAsync(string.Join(Separator, headers.Select(EscapeCsvValue)));

        // Запись строк данных
        foreach (var row in dataList)
        {
            var values = headers.Select(header =>
            {
                if (!row.ContainsKey(header) || row[header] == null)
                    return string.Empty;

                var rawValue = row[header]!;
                var stringValue = rawValue switch
                {
                    DateTime dt => dt.ToString("yyyy-MM-dd HH:mm:ss", System.Globalization.CultureInfo.InvariantCulture),
                    IFormattable formattable => formattable.ToString(null, System.Globalization.CultureInfo.InvariantCulture),
                    _ => rawValue.ToString() ?? string.Empty
                };

                return EscapeCsvValue(stringValue);
            });

            await writer.WriteLineAsync(string.Join(Separator, values));
        }
    }

    /// <summary>
    /// Выполняет разбор одной строки CSV с учетом возможного экранирования кавычками.
    /// Алгоритм корректно обрабатывает запятые, находящиеся внутри текстовых значений.
    /// </summary>
    /// <param name="line">Сырая строка из CSV файла.</param>
    /// <returns>Список извлеченных текстовых значений.</returns>
    private static List<string> ParseCsvLine(string line)
    {
        var result = new List<string>();
        var inQuotes = false;
        var currentValue = new StringBuilder();

        for (var i = 0; i < line.Length; i++)
        {
            var c = line[i];

            switch (c)
            {
                case '\"':
                {
                    // Проверка на экранированную кавычку внутри закавыченного текста ("").
                    if (inQuotes && i + 1 < line.Length && line[i + 1] == '\"')
                    {
                        currentValue.Append('\"');
                        i++;
                    }
                    else
                    {
                        inQuotes = !inQuotes;
                    }

                    break;
                }
                case ',' when !inQuotes:
                    result.Add(currentValue.ToString());
                    currentValue.Clear();
                    break;
                
                default:
                    currentValue.Append(c);
                    break;
            }
        }

        // Добавление последнего значения после окончания цикла
        result.Add(currentValue.ToString());

        return result;
    }

    /// <summary>
    /// Экранирует строковое значение для записи в CSV. 
    /// Если строка содержит запятые или кавычки, она оборачивается в дополнительные кавычки.
    /// </summary>
    /// <param name="value">Исходное строковое значение.</param>
    /// <returns>Экранированная строка, безопасная для CSV формата.</returns>
    private static string EscapeCsvValue(string? value)
    {
        if (string.IsNullOrEmpty(value)) return string.Empty;

        if (value.Contains(Separator) || value.Contains(Quote))
        {
            // Замена одинарных кавычек на двойные и оборачивание всей строки
            return $"{Quote}{value.Replace(Quote, EscapedQuote)}{Quote}";
        }

        return value;
    }
}