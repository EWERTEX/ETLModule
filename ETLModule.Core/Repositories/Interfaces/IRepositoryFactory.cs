namespace ETLModule.Core.Repositories.Interfaces;

/// <summary>
/// Фабрика для динамического создания репозиториев на основе типа СУБД.
/// </summary>
public interface IRepositoryFactory
{
    IDynamicRepository Create(string dbmsType, string connectionString);
}