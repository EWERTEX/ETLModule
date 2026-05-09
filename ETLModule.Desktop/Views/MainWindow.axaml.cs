using System.ComponentModel;
using System.Data;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Platform.Storage;
using ETLModule.Core.Analysis;
using ETLModule.Core.Files.Implementations;
using ETLModule.Core.Repositories.Implementations;
using ETLModule.Core.Transformation;
using ETLModule.Desktop.Services;
using ETLModule.Desktop.ViewModels;

namespace ETLModule.Desktop.Views;

public partial class MainWindow : Window, IDialogService
{
    public MainWindow()
    {
        InitializeComponent();
        
        var vm = new MainWindowViewModel(this, new DataTypeAnalyzer(), new DataTransformer(), 
            new RepositoryFactory(), new FileHandlerFactory());
        
        DataContext = vm;
        
        vm.PropertyChanged += ViewModel_PropertyChanged;
    }

    private void ViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(MainWindowViewModel.CurrentData)) return;
        
        var vm = (MainWindowViewModel)DataContext!;
            
        var grid = this.FindControl<DataGrid>("MainDataGrid");
        if (grid == null) return;
            
        grid.Columns.Clear();
        grid.ItemsSource = null;
            
        if (vm.CurrentData is not { } table) return;
            
        foreach (DataColumn col in table.Columns)
        {
            grid.Columns.Add(new DataGridTextColumn
            {
                Header = col.ColumnName,
                Binding = new Binding($"Row.ItemArray[{col.Ordinal}]")
            });
        }
                
        // Привязка данных
        grid.ItemsSource = table.DefaultView;
    }

    // === РЕАЛИЗАЦИЯ ИНТЕРФЕЙСА IDialogService ===

    public async Task<string?> ShowFilePickerAsync()
    {
        var options = new FilePickerOpenOptions
        {
            Title = "Выберите файл для импорта",
            AllowMultiple = false,
            FileTypeFilter =
            [
                new FilePickerFileType("Поддерживаемые форматы (CSV, JSON, XML, XLSX)") 
                { 
                    Patterns = ["*.csv", "*.json", "*.xml", "*.xlsx"]
                }
            ]
        };

        var result = await StorageProvider.OpenFilePickerAsync(options);
        return result.Count > 0 ? result[0].Path.LocalPath : null;
    }

    public async Task<string?> ShowInputDialogAsync(string title, string watermark)
    {
        var dialog = new InputDialogWindow
        {
            DataContext = new InputDialogViewModel { Title = title, Watermark = watermark }
        };
        return await dialog.ShowDialog<string?>(this);
    }
    
    public async Task<string?> ShowFileSaverAsync(string format, string suggestedFileName)
    {
        var options = new FilePickerSaveOptions
        {
            Title = $"Сохранить экспорт в формате {format.ToUpper()}",
            DefaultExtension = format,
            SuggestedFileName = suggestedFileName,
            FileTypeChoices =
            [
                new FilePickerFileType($"{format.ToUpper()} File") 
                { 
                    Patterns = [$"*.{format}"]
                }
            ]
        };

        var result = await StorageProvider.SaveFilePickerAsync(options);
        return result?.Path.LocalPath;
    }
}