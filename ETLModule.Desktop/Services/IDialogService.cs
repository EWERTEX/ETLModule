using System.Threading.Tasks;

namespace ETLModule.Desktop.Services;

/// <summary>
/// Интерфейс для вызова UI-диалогов из ViewModel без нарушения паттерна MVVM.
/// </summary>
public interface IDialogService
{
    /// <summary>
    /// Открывает системное окно выбора файла (CSV, JSON, XML, XLSX).
    /// </summary>
    /// <returns>Путь к выбранному файлу или null, если выбор отменен.</returns>
    Task<string?> ShowFilePickerAsync();

    /// <summary>
    /// Открывает системное окно сохранения файла.
    /// </summary>
    /// <returns>Путь к сохранённому файлу или null, если сохранение отменено.</returns>
    Task<string?> ShowFileSaverAsync(string format, string suggestedFileName);
    
    /// <summary>
    /// Открывает пользовательское диалоговое окно для ввода текста.
    /// </summary>
    /// <param name="title">Заголовок окна.</param>
    /// <param name="watermark">Подсказка в поле ввода.</param>
    /// <returns>Введенная строка или null.</returns>
    Task<string?> ShowInputDialogAsync(string title, string watermark);
    
}