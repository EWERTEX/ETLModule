using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.IO;
using Avalonia;
using Avalonia.Media;
using Avalonia.Styling;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Configuration;
using ETLModule.Core.Analysis;
using ETLModule.Core.Files.Interfaces;
using ETLModule.Core.Models;
using ETLModule.Core.Repositories.Interfaces;
using ETLModule.Core.Transformation;
using ETLModule.Desktop.Services;

namespace ETLModule.Desktop.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    // === 1. ГЕОМЕТРИЯ ИКОНОК ===
    private static readonly StreamGeometry SunIcon = StreamGeometry.Parse("M12 8a4 4 0 1 1 0 8 4 4 0 1 1 0-8 M12 2v2 M12 20v2 M4.93 4.93 l1.41 1.41 M17.66 17.66 l1.41 1.41 M2 12h2 M20 12h2 M6.34 17.66 l-1.41 1.41 M19.07 4.93 l-1.41 1.41");
    private static readonly StreamGeometry MoonIcon = StreamGeometry.Parse("M12 3a6 6 0 0 0 9 9 9 9 0 1 1-9-9Z");
    private static readonly StreamGeometry MenuIcon = StreamGeometry.Parse("M3 12h18 M3 6h18 M3 18h18");

    // === 2. СОСТОЯНИЯ ИНТЕРФЕЙСА ===
    [ObservableProperty]
    public partial string WindowTitle { get; set; } = "ETLModule | Конвейер данных";

    [ObservableProperty]
    public partial StreamGeometry ThemeIconData { get; set; } = SunIcon;

    [ObservableProperty]
    public partial StreamGeometry MenuIconData { get; set; } = MenuIcon;

    [ObservableProperty]
    public partial bool IsPaneOpen { get; set; } = true;

    [ObservableProperty]
    public partial string LogMessage { get; set; } = "Система готова к работе.";

    [ObservableProperty]
    public partial IBrush LogColor { get; set; } = Brushes.Gray;

    // === 3. ДАННЫЕ И ВЫБОР СУБД ===
    public ObservableCollection<string> DbmsList { get; } = ["SQLite", "PostgreSQL", "MS SQL Server"];
    
    [ObservableProperty]
    public partial string SelectedDbms { get; set; } = "SQLite";
    
    public ObservableCollection<string> Tables { get; } = [];

    [ObservableProperty]
    public partial string? SelectedTable { get; set; }

    [ObservableProperty]
    public partial DataTable? CurrentData { get; set; }

    // === 4. ПОЛИТИКИ ИМПОРТА ===
    public ObservableCollection<ImportPolicyViewModel> ImportPolicies { get; } =
    [
        new("Исключение", "При совпадении ключа будет вызвано исключение."),
        new("Обновлять", "При совпадении ключа старая запись будет заменена новой."),
        new("Игнорировать", "При совпадении ключа новая запись будет пропущена.")
    ];
    
    [ObservableProperty]
    public partial ImportPolicyViewModel? SelectedImportPolicy { get; set; }

    // === 5. СЕРВИСЫ И ЯДРО ===
    private IDynamicRepository? _repository;
    private readonly IRepositoryFactory _repositoryFactory;
    private readonly IConfigurationRoot _configuration;
    private readonly IDataTypeAnalyzer _typeAnalyzer;
    private readonly IDataTransformer _dataTransformer;
    private readonly IDialogService _dialogService;
    private readonly IFileHandlerFactory _fileHandlerFactory;

    // === КОНСТРУКТОРЫ ===
    
    /// <summary>
    /// Конструктор для режима работы (Runtime)
    /// </summary>
    public MainWindowViewModel(IDialogService dialogService, IDataTypeAnalyzer typeAnalyzer, 
        IDataTransformer dataTransformer, IRepositoryFactory repositoryFactory, IFileHandlerFactory fileHandlerFactory)
    {
        _dialogService = dialogService;
        _typeAnalyzer = typeAnalyzer;
        _dataTransformer = dataTransformer;
        _repositoryFactory = repositoryFactory;
        _fileHandlerFactory = fileHandlerFactory;

        var builder = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);
        _configuration = builder.Build();

        SelectedImportPolicy = ImportPolicies[0];
        
        _ = ConnectToDatabaseAsync(SelectedDbms); 
    }

    // === АВТОМАТИЧЕСКИЕ РЕАКЦИИ НА ВЫБОР ===

    partial void OnSelectedDbmsChanged(string value)
    {
        if (!string.IsNullOrEmpty(value)) _ = ConnectToDatabaseAsync(value);
    }

    partial void OnSelectedTableChanged(string? value)
    {
        if (!string.IsNullOrEmpty(value)) _ = LoadDataForTableAsync(value);
        else CurrentData = null;
    }

    // === ЛОГИКА РАБОТЫ С БАЗОЙ ДАННЫХ ===

    private async Task ConnectToDatabaseAsync(string dbmsType)
    {
        try
        {
            // Определяем ключ конфигурации
            var dbmsKey = dbmsType switch
            {
                "PostgreSQL" => "Postgres",
                "MS SQL Server" => "MsSql",
                _ => "Sqlite"
            };

            var connString = _configuration.GetConnectionString(dbmsKey);
            
            if (string.IsNullOrEmpty(connString))
            {
                if (dbmsType == "SQLite") connString = "Data Source=etl.db";
                else throw new InvalidOperationException($"Строка подключения для {dbmsType} не найдена.");
            }
            
            _repository = _repositoryFactory.Create(dbmsKey, connString);
            
            if (_repository == null)
            {
                WriteLog("Действие отменено: нет активного подключения к базе данных.", false);
                return;
            }
            
            WriteLog($"Успешное подключение к СУБД: {dbmsKey}", true);
            await RefreshTablesListAsync();
        }
        catch (Exception ex)
        {
            WriteLog($"Ошибка подключения к {dbmsType}: {ex.Message}", false);
            Tables.Clear();
            CurrentData = null;
        }
    }

    private async Task RefreshTablesListAsync()
    {
        if (_repository == null)
        {
            WriteLog("Действие отменено: нет активного подключения к базе данных.", false);
            return;
        }
        
        try
        {
            var names = await _repository.GetTableNamesAsync();
            Tables.Clear();
            foreach (var name in names) Tables.Add(name);

        }
        catch (Exception ex)
        {
            WriteLog($"Ошибка при получении списка таблиц: {ex.Message}", false);
        }
    }

    private async Task LoadDataForTableAsync(string tableName)
    {
        if (_repository == null)
        {
            WriteLog("Действие отменено: нет активного подключения к базе данных.", false);
            return;
        }
        
        try
        {
            var rawData = await _repository.GetDataAsync(tableName);
            var dataTable = ConvertToDataTable(rawData, tableName);
            
            CurrentData = dataTable;

            WriteLog($"Данные таблицы '{tableName}' успешно загружены.", true);
        }
        catch (Exception ex)
        {
            WriteLog($"Ошибка загрузки данных: {ex.Message}", false);
            CurrentData = null;
        }
    }

    private static DataTable ConvertToDataTable(IEnumerable<Dictionary<string, object?>> data, string tableName)
    {
        var dt = new DataTable(tableName);
        var columnsCreated = false;

        foreach (var row in data)
        {
            if (!columnsCreated)
            {
                foreach (var kvp in row)
                {
                    dt.Columns.Add(kvp.Key, kvp.Value?.GetType() ?? typeof(object));
                }
                columnsCreated = true;
            }

            var newRow = dt.NewRow();
            foreach (var kvp in row)
            {
                newRow[kvp.Key] = kvp.Value ?? DBNull.Value;
            }
            dt.Rows.Add(newRow);
        }
        return dt;
    }

    // === КОМАНДЫ (КНОПКИ ИНТЕРФЕЙСА) ===

    [RelayCommand]
    private void TogglePane() => IsPaneOpen = !IsPaneOpen;

    [RelayCommand]
    private void ToggleTheme()
    {
        if (Application.Current == null) return;
        
        var currentTheme = Application.Current.ActualThemeVariant;
        var newTheme = currentTheme == ThemeVariant.Dark ? ThemeVariant.Light : ThemeVariant.Dark;
        Application.Current.RequestedThemeVariant = newTheme;
        ThemeIconData = newTheme == ThemeVariant.Dark ? SunIcon : MoonIcon;
            
        WriteLog($"Тема интерфейса изменена на {(newTheme == ThemeVariant.Dark ? "Темную" : "Светлую")}", true);
    }

    // === ЛОГИКА РАБОТЫ С ФАЙЛАМИ ===

    [RelayCommand]
    private async Task AddTableAsync()
    {
        if (_repository == null)
        {
            WriteLog("Действие отменено: нет активного подключения к базе данных.", false);
            return;
        }
        
        var path = await _dialogService.ShowFilePickerAsync();
        if (string.IsNullOrEmpty(path)) return;
        
        var tableName = await _dialogService.ShowInputDialogAsync("Новая таблица", "Введите имя таблицы...");
        if (string.IsNullOrWhiteSpace(tableName)) return;

        try
        {
            WriteLog($"Чтение и анализ файла '{Path.GetFileName(path)}'...", true);

            // 3. ЧТЕНИЕ ФАЙЛА
            var (importer, _) = _fileHandlerFactory.GetHandlers(path);
            var rawDataEnumerable = await importer.ImportAsync(path);
            
            var rawData = rawDataEnumerable.ToList(); 
            if (rawData.Count == 0) throw new Exception("Файл пуст.");

            // 4. АНАЛИЗ ТИПОВ
            var schema = _typeAnalyzer.AnalyzeTypes(rawData);

            // 5. СОЗДАНИЕ ТАБЛИЦЫ
            await _repository.CreateTableAsync(tableName, schema);

            // 6. ВСТАВКА ДАННЫХ
            var dataToInsert = _dataTransformer.Transform(rawData, schema);

            await _repository.InsertDataAsync(tableName, dataToInsert, MapPolicy(SelectedImportPolicy?.Name));
            
            // 7. Обновление UI
            await RefreshTablesListAsync();
            SelectedTable = tableName;
            
            WriteLog($"Таблица '{tableName}' успешно создана и заполнена.", true);
        }
        catch (Exception ex) { WriteLog($"Ошибка добавления таблицы: {ex.Message}", false); }
    }

    [RelayCommand]
    private async Task ImportDataAsync()
    {
        if (_repository == null)
        {
            WriteLog("Действие отменено: нет активного подключения к базе данных.", false);
            return;
        }
        
        if (string.IsNullOrEmpty(SelectedTable)) return;

        var path = await _dialogService.ShowFilePickerAsync();
        if (string.IsNullOrEmpty(path)) return;

        try
        {
            WriteLog($"Импорт файла '{Path.GetFileName(path)}' в таблицу '{SelectedTable}'...", true);

            // 1. ЧТЕНИЕ ФАЙЛА
            var (importer, _) = _fileHandlerFactory.GetHandlers(path);
            var rawDataEnumerable = await importer.ImportAsync(path);

            // 2. АНАЛИЗ ТИПОВ
            var rawData = rawDataEnumerable.ToList();
            var schema = _typeAnalyzer.AnalyzeTypes(rawData);
            
            // 3. ПРЕОБРАЗОВАНИЕ И ВСТАВКА
            var dataToInsert = _dataTransformer.Transform(rawData, schema);

            await _repository.InsertDataAsync(SelectedTable, dataToInsert, MapPolicy(SelectedImportPolicy?.Name));

            // 4. Обновление таблицы в интерфейсе
            await LoadDataForTableAsync(SelectedTable);
            WriteLog($"Данные успешно добавлены в '{SelectedTable}'.", true);
        }
        catch (Exception ex) { WriteLog($"Ошибка импорта: {ex.Message}", false); }
    }

    [RelayCommand]
    private async Task ExportDataAsync(string format) // Avalonia передает "csv", "json" и т.д. из XAML
    {
        if (_repository == null)
        {
            WriteLog("Действие отменено: нет активного подключения к базе данных.", false);
            return;
        }
        
        if (string.IsNullOrEmpty(SelectedTable)) return;
        
        var defaultFileName = $"{SelectedTable}_Export";
        
        var savePath = await _dialogService.ShowFileSaverAsync(format, defaultFileName);
        if (string.IsNullOrEmpty(savePath)) return;

        try
        {
            WriteLog($"Подготовка данных для экспорта...", true);

            // 1. ПОЛУЧЕНИЕ ДАННЫХ ИЗ СУБД

            var data = await _repository.GetDataAsync(SelectedTable);

            // 2. ЭКСПОРТ
            var (_, exporter) = _fileHandlerFactory.GetHandlers(savePath);
            await exporter.ExportAsync(savePath, data);
            
            WriteLog($"Успешно экспортировано в '{Path.GetFileName(savePath)}'.", true);
        }
        catch (Exception ex) { WriteLog($"Ошибка экспорта: {ex.Message}", false); }
    }

    // === ВСПОМОГАТЕЛЬНЫЕ МЕТОДЫ ===
    
    // Вспомогательный метод для перевода текста из выпадающего списка в Enum ядра
    private static ImportPolicy MapPolicy(string? policyName) => policyName switch
    {
        "Обновлять" => ImportPolicy.Update,
        "Игнорировать" => ImportPolicy.Ignore,
        _ => ImportPolicy.Fail // Для политики "Исключение"
    };

    private void WriteLog(string message, bool isSuccess)
    {
        LogMessage = $"[{DateTime.Now:HH:mm:ss}] {message}";
        LogColor = isSuccess ? Brushes.SeaGreen : Brushes.IndianRed;
    }
}

/// <summary>
/// Класс для отображения политик импорта в ComboBox
/// </summary>
public class ImportPolicyViewModel(string name, string description)
{
    public string Name { get; } = name;
    public string Description { get; } = description;
}