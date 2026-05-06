using System.Text.Encodings.Web;
using System.Text.Json;
using ETLModule.Core.Files.Interfaces;

namespace ETLModule.Core.Files.Implementations;

/// <summary>
/// Обеспечивает функциональность для импорта и экспорта табличных данных в формате JSON.
/// Использует нативную библиотеку <see cref="System.Text.Json"/> для высокой производительности и низкого потребления памяти.
/// </summary>
public class JsonFileHandler : IFileImporter, IFileExporter
{
    private readonly JsonSerializerOptions _serializerOptions;

    /// <summary>
    /// Инициализирует новый экземпляр класса <see cref="JsonFileHandler"/> 
    /// с предварительно настроенными параметрами сериализации.
    /// </summary>
    public JsonFileHandler()
    {
        _serializerOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };
    }

    /// <inheritdoc />
    public async Task<IEnumerable<Dictionary<string, string>>> ImportAsync(string filePath)
    {
        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException($"Файл для импорта не найден: {filePath}");
        }

        // Используется асинхронный файловый поток для оптимизации работы с диском
        await using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize: 4096, useAsync: true);

        try
        {
            // Десериализация JSON в массив словарей.
            // Использование JsonElement позволяет безопасно прочитать значение любого типа 
            // (число, строка, boolean) до того, как он будет приведён к строке для анализатора.
            var rawData = await JsonSerializer.DeserializeAsync<List<Dictionary<string, JsonElement>>>(stream);

            if (rawData == null)
            {
                return [];
            }

            var result = new List<Dictionary<string, string>>();

            foreach (var rawRow in rawData)
            {
                var row = new Dictionary<string, string>();
                foreach (var kvp in rawRow)
                {
                    row[kvp.Key] = kvp.Value.ValueKind == JsonValueKind.Null 
                        ? string.Empty 
                        : kvp.Value.ToString();
                }
                result.Add(row);
            }

            return result;
        }
        catch (JsonException ex)
        {
            throw new FormatException($"Нарушена структура JSON файла: {ex.Message}", ex);
        }
    }

    /// <inheritdoc />
    public async Task ExportAsync(string filePath, IEnumerable<Dictionary<string, object?>> data)
    {
        var dataList = data.ToList();

        // Открывается поток на создание/перезапись файла
        await using var stream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None, bufferSize: 4096, useAsync: true);
        
        // Сериализуется типизированные данные (числа останутся числами, строки - строками)
        await JsonSerializer.SerializeAsync(stream, dataList, _serializerOptions);
    }
}