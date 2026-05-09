using System.Globalization;

namespace ETLModule.Core.Transformation;

/// <summary>
/// Реализация по умолчанию для трансформации типов данных.
/// </summary>
public class DataTransformer : IDataTransformer
{
    /// <inheritdoc />
    public IEnumerable<Dictionary<string, object?>> Transform(
        IEnumerable<Dictionary<string, string>> rawData, 
        Dictionary<string, Type> schema)
    {
        var typedData = new List<Dictionary<string, object?>>();

        foreach (var rawRow in rawData)
        {
            var typedRow = new Dictionary<string, object?>();
            
            foreach (var (rowName, rowValue) in rawRow)
            {
                // Защита: если колонки нет в схеме, оставляем как есть
                if (!schema.TryGetValue(rowName, out var targetType))
                {
                    typedRow[rowName] = string.IsNullOrWhiteSpace(rowValue) ? null : rowValue;
                    continue;
                }

                // Логика конвертации
                if (string.IsNullOrWhiteSpace(rowValue))
                {
                    typedRow[rowName] = null;
                }
                else
                {
                    if (targetType == typeof(Guid))
                    {
                        typedRow[rowName] = Guid.Parse(rowValue);
                    }
                    else
                    {
                        // InvariantCulture важен для правильного парсинга дат и чисел с точкой
                        typedRow[rowName] = Convert.ChangeType(rowValue, targetType, CultureInfo.InvariantCulture);
                    }
                }
            }
            
            typedData.Add(typedRow);
        }

        return typedData;
    }
}