using ETLModule.Core.Files.Interfaces;

namespace ETLModule.Core.Files.Implementations;

public class FileHandlerFactory : IFileHandlerFactory
{
    public (IFileImporter Importer, IFileExporter Exporter) GetHandlers(string filePath)
    {
        var extension = Path.GetExtension(filePath).ToLower();
        return extension switch
        {
            ".csv" => (new CsvFileHandler(), new CsvFileHandler()),
            ".json" => (new JsonFileHandler(), new JsonFileHandler()),
            ".xml" => (new XmlFileHandler(), new XmlFileHandler()),
            ".xlsx" => (new ExcelFileHandler(), new ExcelFileHandler()),
            _ => throw new NotSupportedException($"Формат {extension} не поддерживается.")
        };
    }
}