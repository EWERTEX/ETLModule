using System.Data.Common;

namespace DbExportModule.Core.Database.Factories;

/// <summary>
/// Абстрактная фабрика для создания подключений к базе данных.
/// Является основой для независимости модуля от конкретной СУБД (принцип Dependency Inversion).
/// </summary>
public interface IDbConnectionFactory
{
    /// <summary>
    /// Создает и возвращает новое подключение к базе данных.
    /// Подключение возвращается в закрытом состоянии, метод Open() вызывается непосредственно перед выполнением запроса.
    /// </summary>
    /// <returns>Экземпляр <see cref="DbConnection"/>, специфичный для настроенной СУБД.</returns>
    DbConnection CreateConnection();
}