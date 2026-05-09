namespace ETLModule.Core.Transformation;

/// <summary>
/// Определяет контракт для трансформации сырых строковых данных в строго типизированные объекты.
/// </summary>
public interface IDataTransformer
{
    /// <summary>
    /// Выполняет преобразование сырых данных на основе заданной схемы типов.
    /// </summary>
    /// <param name="rawData">Сырые строковые данные, полученные от импортера.</param>
    /// <param name="schema">Схема типов данных, полученная от анализатора или базы данных.</param>
    /// <returns>Коллекция типизированных словарей, готовая для загрузки в базу данных.</returns>
    IEnumerable<Dictionary<string, object?>> Transform(
        IEnumerable<Dictionary<string, string>> rawData, 
        Dictionary<string, Type> schema);
}