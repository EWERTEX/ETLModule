using ClosedXML.Excel;
using ETLModule.Core.Files.Interfaces;

namespace ETLModule.Core.Files.Implementations;

/// <summary>
/// Обеспечивает функциональность для импорта и экспорта табличных данных в формате Excel (XLSX).
/// Использует библиотеку ClosedXML для безопасной генерации файлов Office Open XML.
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
        return await Task.Run(() =>
        {
            var result = new List<Dictionary<string, string>>();

            using var workbook = new XLWorkbook(filePath);
            var worksheet = workbook.Worksheets.FirstOrDefault();

            if (worksheet == null)
            {
                return result;
            }

            // Определение границ реально заполненных данных
            var lastRowUsed = worksheet.LastRowUsed();
            var lastColumnUsed = worksheet.LastColumnUsed();

            if (lastRowUsed == null || lastColumnUsed == null)
            {
                return result;
            }

            var rowCount = lastRowUsed.RowNumber();
            var colCount = lastColumnUsed.ColumnNumber();

            var headers = new List<string>();
            for (var col = 1; col <= colCount; col++)
            {
                // Метод GetString() гарантирует извлечение строкового представления ячейки
                headers.Add(worksheet.Cell(1, col).GetString());
            }

            for (var row = 2; row <= rowCount; row++)
            {
                var dataRow = new Dictionary<string, string>();
                var hasData = false;

                for (var col = 1; col <= colCount; col++)
                {
                    var header = headers[col - 1];
                    var value = worksheet.Cell(row, col).GetString();

                    if (!string.IsNullOrWhiteSpace(value))
                    {
                        hasData = true;
                    }

                    var safeHeader = string.IsNullOrWhiteSpace(header) ? $"Column_{col}" : header;
                    dataRow[safeHeader] = value;
                }

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
            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("Exported Data");

            if (dataList.Count == 0)
            {
                workbook.SaveAs(filePath);
                return;
            }

            var headers = dataList.First().Keys.ToList();

            // Запись и стилизация заголовков
            for (var col = 1; col <= headers.Count; col++)
            {
                var cell = worksheet.Cell(1, col);
                cell.Value = headers[col - 1];
                cell.Style.Font.Bold = true;
            }

            // Запись данных
            for (var row = 0; row < dataList.Count; row++)
            {
                var currentRow = dataList[row];
                for (var col = 1; col <= headers.Count; col++)
                {
                    var header = headers[col - 1];
                    var cell = worksheet.Cell(row + 2, col);

                    if (currentRow.TryGetValue(header, out var value) && value != null)
                    {

                        // ClosedXML строго типизирован, необходимо явное приведение типов для корректной записи
                        switch (value)
                        {
                            case DateTime dt:
                                cell.Value = dt;
                                cell.Style.DateFormat.Format = "yyyy-mm-dd hh:mm:ss";
                                break;
                            case double d:
                                cell.Value = d;
                                break;
                            case long l:
                                cell.Value = l;
                                break;
                            case bool b:
                                cell.Value = b;
                                break;
                            default:
                                cell.Value = value.ToString();
                                break;
                        }
                    }
                }
            }

            // Автоматическая настройка ширины колонок под размер содержимого.
            // Изолировано в try-catch, так как графический движок может упасть 
            // при сканировании системных шрифтов Windows (например, из-за закрытых папок вроде Mysql).
            try
            {
                worksheet.Columns().AdjustToContents();
            }
            catch (UnauthorizedAccessException)
            {
                // Игнорирование ошибки доступа. Файл сохранится со стандартной шириной колонок.
            }
            catch (Exception)
            {
                // Перехват любых других сбоев отрисовщика шрифтов SixLabors.
            }

            workbook.SaveAs(filePath);
        });
    }
}