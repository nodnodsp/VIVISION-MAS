using Microsoft.Data.Sqlite;

namespace MAS.Infrastructure.Database;

public static class SqliteConnectionFactory
{
    public static SqliteConnection Create()
    {
        var connectionString = new SqliteConnectionStringBuilder
        {
            DataSource = DatabasePaths.DatabaseFile,
            ForeignKeys = true,
        }.ToString();

        return new SqliteConnection(connectionString);
    }
}
