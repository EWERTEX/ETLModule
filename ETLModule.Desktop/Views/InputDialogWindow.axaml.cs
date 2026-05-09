using Avalonia.Controls;
using Avalonia.Interactivity;

namespace ETLModule.Desktop.Views;

public partial class InputDialogWindow : Window
{
    public InputDialogWindow()
    {
        InitializeComponent();
    }

    private void OkButton_Click(object? sender, RoutedEventArgs e)
    {
        var vm = (ViewModels.InputDialogViewModel)DataContext!;
        Close(vm.InputText); 
    }

    private void CancelButton_Click(object? sender, RoutedEventArgs e)
    {
        Close(null); 
    }
}