namespace ETLModule.Core.Analysis;

/// <summary>
/// Предоставляет контракт для анализатора типов данных.
/// Предназначен для автоматического вывода типов колонок на основе выборки нетипизированных строковых данных.
/// </summary>
public interface IDataTypeAnalyzer
{
    /// <summary>
    /// Выполняет анализ переданной выборки данных и определяет оптимальный тип данных среды CLR для каждой колонки.
    /// </summary>
    /// <param name="sampleData">
    /// Выборка данных, где ключ внутреннего словаря представляет имя колонки, 
    /// а значение — исходное строковое представление данных из файла.
    /// </param>
    /// <returns>
    /// Словарь, сопоставляющий имя колонки и определенный для нее тип данных <see cref="Type"/>.
    /// </returns>
    Dictionary<string, Type> AnalyzeTypes(IEnumerable<Dictionary<string, string>> sampleData);
}