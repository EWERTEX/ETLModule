using ETLModule.Core.Database.Dialects;
using ETLModule.Core.Database.Factories;
using ETLModule.Core.Repositories.Interfaces;

namespace ETLModule.Core.Repositories.Implementations;

public class RepositoryFactory : IRepositoryFactory
{
    public IDynamicRepository Create(string dbmsType, string connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new ArgumentException("Строка подключения не может быть пустой.");

        IDbConnectionFactory factory;
        ISqlDialect dialect;

        switch (dbmsType)
        {
            case "Sqlite":
                factory = new SqliteConnectionFactory(connectionString);
                dialect = new SqliteDialect();
                break;
            
            case "Postgres":
                factory = new PostgresConnectionFactory(connectionString);
                dialect = new PostgresDialect();
                break;
            
            case "MsSql":
                factory = new SqlServerConnectionFactory(connectionString);
                dialect = new SqlServerDialect();
                break;
            
            default:
                throw new NotSupportedException($"СУБД '{dbmsType}' не поддерживается.");
        }

        return new DynamicRepository(factory, dialect);
    }
}