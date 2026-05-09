using CommunityToolkit.Mvvm.ComponentModel;

namespace ETLModule.Desktop.ViewModels;

public partial class InputDialogViewModel : ViewModelBase
{
    [ObservableProperty]
    public partial string Title { get; set; } = "Ввод данных";
    
    [ObservableProperty]
    public partial string Watermark { get; set; } = "Введите значение...";
    
    [ObservableProperty]
    public partial string InputText { get; set; } = string.Empty;
}