using Microsoft.Extensions.Configuration;

// Подключаем только интерфейсы и фабрики из Ядра!
using ETLModule.Core.Analysis;
using ETLModule.Core.Files.Implementations;
using ETLModule.Core.Files.Interfaces;
using ETLModule.Core.Models;
using ETLModule.Core.Repositories.Implementations;
using ETLModule.Core.Repositories.Interfaces;
using ETLModule.Core.Transformation;

namespace ETLModule.ConsoleTest;

/// <summary>
/// Обеспечивает комплексное интеграционное тестирование всех слоев модуля (ETL-конвейера).
/// Включает проверку файловой подсистемы, анализатора типов и уровня доступа к данным (DAL).
/// </summary>
internal static class ConsoleTest
{
    private const string TestTableName = "IntegrationTestUsers";

    /// <summary>
    /// Асинхронная точка входа. Запускает последовательное выполнение этапов тестирования.
    /// </summary>
    private static async Task Main()
    {
        Console.WriteLine("=== Запуск комплексного интеграционного тестирования модуля ===\n");

        try
        {
            // 1. Инициализация инфраструктуры (используем абстракции)
            var configuration = BuildConfiguration();
            var repository = InitializeRepository(configuration);
            var analyzer = new DataTypeAnalyzer();
            var fileHandlers = InitializeFileHandlers();

            var baseDir = AppDomain.CurrentDomain.BaseDirectory;
            var testData = GenerateTestData();

            // 2. Тестирование подсистемы экспорта (File Handlers)
            Console.WriteLine("[ЭТАП 1] Экспорт эталонных данных в поддерживаемые форматы...");
            foreach (var handler in fileHandlers)
            {
                var filePath = Path.Combine(baseDir, $"ExportedData{handler.Extension}");
                await handler.Exporter.ExportAsync(filePath, testData);
                Console.WriteLine($"  -> Файл успешно сгенерирован: {Path.GetFileName(filePath)}");
            }

            // 3. Тестирование подсистемы импорта (на примере CSV)
            Console.WriteLine("\n[ЭТАП 2] Чтение данных из сгенерированного CSV-файла...");
            var targetImportFile = Path.Combine(baseDir, "ExportedData.csv");
            var csvImporter = fileHandlers.First(h => h.Extension == ".csv").Importer;
            
            var rawImportedData = await csvImporter.ImportAsync(targetImportFile);
            var rawDataList = rawImportedData.ToList();
            Console.WriteLine($"  -> Успешно прочитано строк: {rawDataList.Count}");

            // 4. Тестирование анализатора типов данных
            Console.WriteLine("\n[ЭТАП 3] Автоматический анализ типов данных (Вывод схемы)...");
            var schema = analyzer.AnalyzeTypes(rawDataList);
            
            foreach (var column in schema)
            {
                Console.WriteLine($"  -> Колонка '{column.Key}' определена как: {column.Value.Name}");
            }

            // 5. Трансформация данных в соответствии с выведенной схемой
            Console.WriteLine("\n[ЭТАП 4] Трансформация строковых данных в строго типизированные объекты...");
            var transformer = new DataTransformer();
            var typedData = transformer.Transform(rawDataList, schema);
            Console.WriteLine("  -> Преобразование типов завершено без ошибок.");

            // 6. Тестирование уровня базы данных (Data Access Layer)
            Console.WriteLine($"\n[ЭТАП 5] Работа с базой данных (Таблица: {TestTableName})...");
            
            try
            {
                await repository.CreateTableAsync(TestTableName, schema);
                Console.WriteLine("  -> Таблица успешно создана (или проверена).");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  -> Пропуск создания: Таблица уже существует. ({ex.Message})");
            }

            Console.WriteLine("  -> Запись данных с использованием политики UPSERT (ImportPolicy.Update)...");
            await repository.InsertDataAsync(TestTableName, typedData, ImportPolicy.Update);
            
            Console.WriteLine("  -> Чтение записанных данных для верификации...");
            var dbData = await repository.GetDataAsync(TestTableName);
            
            Console.WriteLine("\n--- Результат выборки из базы данных ---");
            foreach (var row in dbData)
            {
                var id = row["Id"];
                var name = row["FullName"];
                var age = row["Age"];
                var date = row["RegistrationDate"] ?? "NULL";
                Console.WriteLine($"ID: {id} | Имя: {name} | Возраст: {age} | Дата: {date}");
            }

            Console.WriteLine("\n=== Интеграционное тестирование успешно завершено ===");
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"\nКРИТИЧЕСКАЯ ОШИБКА: {ex.Message}");
            Console.WriteLine(ex.StackTrace);
            Console.ResetColor();
        }

        Console.WriteLine("\nНажмите любую клавишу для выхода...");
        Console.ReadKey();
    }

    /// <summary>
    /// Выполняет сборку конфигурации из файла appsettings.json.
    /// </summary>
    /// <returns>Объект конфигурации.</returns>
    private static IConfiguration BuildConfiguration()
    {
        return new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .Build();
    }

    /// <summary>
    /// Инициализирует репозиторий баз данных на основе текущих настроек конфигурации.
    /// </summary>
    /// <param name="configuration">Текущая конфигурация приложения.</param>
    /// <returns>Экземпляр репозитория для работы с БД.</returns>
    /// <exception cref="InvalidOperationException">Генерируется при отсутствии строки подключения.</exception>
    private static IDynamicRepository InitializeRepository(IConfiguration configuration)
    {
        var configDbType = configuration["CurrentDatabase"] ?? "SQLite";
        var connectionString = configuration.GetConnectionString(configDbType) 
                               ?? throw new InvalidOperationException("Строка подключения не найдена.");

        // Маппинг ключей конфигурации консоли к формату фабрики Ядра
        var factoryDbType = configDbType switch
        {
            "Postgres" => "Postgres",
            "MsSql" => "MsSql",
            _ => "Sqlite"
        };

        var factory = new RepositoryFactory();
        return factory.Create(factoryDbType, connectionString);
    }

    /// <summary>
    /// Инициализирует коллекцию обработчиков для различных файловых форматов.
    /// </summary>
    /// <returns>Список кортежей, содержащих экспортер, импортер и расширение файла.</returns>
    private static List<(IFileExporter Exporter, IFileImporter Importer, string Extension)> InitializeFileHandlers()
    {
        var factory = new FileHandlerFactory();

        // Проверяем работу фабрики на лету
        var csv = factory.GetHandlers("dummy.csv");
        var json = factory.GetHandlers("dummy.json");
        var xml = factory.GetHandlers("dummy.xml");
        var xlsx = factory.GetHandlers("dummy.xlsx");

        return
        [
            (csv.Exporter, csv.Importer, ".csv"),
            (json.Exporter, json.Importer, ".json"),
            (xml.Exporter, xml.Importer, ".xml"),
            (xlsx.Exporter, xlsx.Importer, ".xlsx")
        ];
    }

    /// <summary>
    /// Генерирует эталонный набор данных для проведения тестирования.
    /// Включает краевые случаи (NULL значения, спецсимволы, различные типы данных).
    /// </summary>
    /// <returns>Коллекция типизированных данных.</returns>
    private static List<Dictionary<string, object?>> GenerateTestData()
    {
        return
        [
            new Dictionary<string, object?>
            {
                { "Id", Guid.NewGuid() },
                { "FullName", "Иван Иванов" },
                { "Age", 28L }, // Типизация Long для совместимости с анализатором
                { "AccountBalance", 150000.50 },
                { "IsActive", true },
                { "RegistrationDate", new DateTime(2025, 10, 15, 14, 30, 0) }
            },

            new Dictionary<string, object?>
            {
                { "Id", Guid.NewGuid() },
                { "FullName", "Анна Смирнова, директор" }, // Проверка экранирования запятых
                { "Age", 34L },
                { "AccountBalance", 999.99 },
                { "IsActive", false },
                { "RegistrationDate", null } // Проверка обработки пустых полей
            }
        ];
    }
}