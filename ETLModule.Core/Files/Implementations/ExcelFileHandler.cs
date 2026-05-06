using OfficeOpenXml;
using DbExportModule.Core.Files.Interfaces;

namespace DbExportModule.Core.Files.Implementations;

/// <summary>
/// Обеспечивает функциональность для импорта и экспорта табличных данных в формате Excel (XLSX).
/// Использует библиотеку EPPlus (версии 4.5.3.3) для генерации нативных файлов Office Open XML.
/// </summary>
public class ExcelFileHandler : IFileImporter, IFileExporter
{
    /// <inheritdoc />
    public async Task<IEnumerable<Dictionary<string, string>>> ImportAsync(string filePath)
    {
        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException($"Файл для импорта не найден: {filePath}");
        }

        // Парсинг Excel-архива является процессорозависимой (CPU-bound) операцией.
        // Использование Task.Run предотвращает блокировку вызывающего потока.
        return await Task.Run(() =>
        {
            var result = new List<Dictionary<string, string>>();
            var fileInfo = new FileInfo(filePath);

            using var package = new ExcelPackage(fileInfo);
            
            // Выбирается первый лист в книге
            var worksheet = package.Workbook.Worksheets.FirstOrDefault();
            
            if (worksheet == null || worksheet.Dimension == null)
            {
                return result; // Возвращается пустая коллекция, если лист или данные отсутствуют
            }

            var rowCount = worksheet.Dimension.Rows;
            var colCount = worksheet.Dimension.Columns;

            // Считывание заголовков из первой строки (индексация в EPPlus начинается с 1)
            var headers = new List<string>();
            for (var col = 1; col <= colCount; col++)
            {
                // Использование свойства Text гарантирует получение отформатированного строкового представления,
                // избавляя от необходимости вручную приводить типы дат и чисел.
                headers.Add(worksheet.Cells[1, col].Text);
            }

            // Считывание строк с данными (начиная со второй строки)
            for (var row = 2; row <= rowCount; row++)
            {
                var dataRow = new Dictionary<string, string>();
                var hasData = false;

                for (var col = 1; col <= colCount; col++)
                {
                    var header = headers[col - 1];
                    var value = worksheet.Cells[row, col].Text;

                    if (!string.IsNullOrWhiteSpace(value))
                    {
                        hasData = true;
                    }

                    // Защита от пустых заголовков в Excel-файле
                    var safeHeader = string.IsNullOrWhiteSpace(header) ? $"Column_{col}" : header;
                    dataRow[safeHeader] = value;
                }

                // Пропуск полностью пустых строк, которые могли образоваться при удалении данных пользователем
                if (hasData)
                {
                    result.Add(dataRow);
                }
            }

            return result;
        });
    }

    /// <inheritdoc />
    public async Task ExportAsync(string filePath, IEnumerable<Dictionary<string, object?>> data)
    {
        var dataList = data.ToList();

        await Task.Run(() =>
        {
            var fileInfo = new FileInfo(filePath);
            
            // Удаление существующего файла для перезаписи
            if (fileInfo.Exists)
            {
                fileInfo.Delete();
            }

            using var package = new ExcelPackage(fileInfo);
            var worksheet = package.Workbook.Worksheets.Add("Exported Data");

            if (dataList.Count == 0)
            {
                package.Save();
                return;
            }

            var headers = dataList.First().Keys.ToList();

            // Запись заголовков и их стилизация
            for (var col = 1; col <= headers.Count; col++)
            {
                var cell = worksheet.Cells[1, col];
                cell.Value = headers[col - 1];
                cell.Style.Font.Bold = true; // Выделение заголовков жирным шрифтом
            }

            // Запись типизированных данных
            for (var row = 0; row < dataList.Count; row++)
            {
                var currentRow = dataList[row];
                for (var col = 1; col <= headers.Count; col++)
                {
                    var header = headers[col - 1];
                    var cell = worksheet.Cells[row + 2, col]; // +2: строка 1 занята заголовками, а индекс 'row' начинается с 0

                    if (!currentRow.TryGetValue(header, out object? value) || value == null) continue;
                    cell.Value = currentRow[header];
                        
                    // Применение нативного Excel-форматирования, если тип значения — дата
                    if (currentRow[header] is DateTime)
                    {
                        cell.Style.Numberformat.Format = "yyyy-mm-dd hh:mm:ss";
                    }
                }
            }

            // Автоматическая настройка ширины всех колонок под содержимое для аккуратного внешнего вида
            worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();

            package.Save();
        });
    }
}