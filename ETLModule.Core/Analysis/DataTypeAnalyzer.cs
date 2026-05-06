using System.Globalization;

namespace ETLModule.Core.Analysis;

/// <summary>
/// Реализует механизм логического вывода типов (Type Inference) для динамических наборов данных.
/// Последовательно проверяет возможность приведения строковых значений к базовым типам CLR.
/// </summary>
public class DataTypeAnalyzer : IDataTypeAnalyzer
{
    /// <inheritdoc />
    public Dictionary<string, Type> AnalyzeTypes(IEnumerable<Dictionary<string, string>> sampleData)
    {
        var resultTypes = new Dictionary<string, Type>();
        var rowsList = sampleData.ToList();

        if (rowsList.Count == 0)
        {
            return resultTypes;
        }

        // Извлечение уникальных имен колонок из первой строки выборки.
        var columnNames = rowsList.First().Keys;

        foreach (var columnName in columnNames)
        {
            // Извлечение всех непустых строковых значений для анализируемой колонки.
            var columnValues = rowsList
                .Select(row => row.GetValueOrDefault(columnName))
                .Where(val => !string.IsNullOrWhiteSpace(val))
                .ToList();

            if (columnValues.Count == 0)
            {
                // В случае отсутствия данных в колонке по умолчанию назначается строковый тип.
                resultTypes[columnName] = typeof(string);
                continue;
            }

            var inferredType = DetermineColumnType(columnValues!);
            resultTypes[columnName] = inferredType;
        }

        return resultTypes;
    }

    /// <summary>
    /// Определяет наиболее подходящий тип данных для предоставленного набора строковых значений.
    /// Проверка осуществляется в порядке от наиболее строгих типов к наименее строгим.
    /// </summary>
    /// <param name="values">Коллекция строковых значений, принадлежащих одной колонке.</param>
    /// <returns>Тип данных <see cref="Type"/>, к которому могут быть приведены все переданные значения.</returns>
    private static Type DetermineColumnType(IEnumerable<string> values)
    {
        var valuesList = values.ToList();
        
        if (valuesList.All(string.IsNullOrWhiteSpace))
        {
            return typeof(string);
        }
        
        var isBool = true;
        var isLong = true;
        var isDouble = true;
        var isDateTime = true;
        var isGuid = true;

        foreach (var value in valuesList)
        {
            if (string.IsNullOrWhiteSpace(value)) continue;
            
            if (isGuid && !Guid.TryParse(value, out _))
            {
                isGuid = false;
            }
            
            if (isBool && !bool.TryParse(value, out _))
            {
                isBool = false;
            }

            if (isLong && !long.TryParse(value, out _))
            {
                isLong = false;
            }

            // Использование InvariantCulture предотвращает ошибки синтаксического анализа чисел 
            // при различных региональных стандартах ОС (точка или запятая в качестве разделителя).
            if (isDouble && !double.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out _))
            {
                isDouble = false;
            }

            // Анализ даты осуществляется без учета региональных смещений для обеспечения консистентности.
            if (isDateTime && !DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.None, out _))
            {
                isDateTime = false;
            }

            
            // Оптимизация: прерывание итерации, если все строгие типы были исключены.
            if (!isBool && !isLong && !isDouble && !isDateTime && !isGuid)
            {
                return typeof(string);
            }
        }

        if (isGuid) return typeof(Guid);
        if (isBool) return typeof(bool);
        if (isLong) return typeof(long);
        if (isDouble) return typeof(double);
        return isDateTime ? typeof(DateTime) : typeof(string);
    }
}