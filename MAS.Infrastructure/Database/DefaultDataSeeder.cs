using Microsoft.Data.Sqlite;

namespace MAS.Infrastructure.Database;

public static class DefaultDataSeeder
{
    public static async Task EnsureSeedDataAsync(CancellationToken cancellationToken = default)
    {
        await using var connection = SqliteConnectionFactory.Create();
        await connection.OpenAsync(cancellationToken);

        var now = DateTime.UtcNow.ToString("O");

        await using (var instrumentCommand = connection.CreateCommand())
        {
            instrumentCommand.CommandText = @"
INSERT OR IGNORE INTO instruments (id, instrument_code, instrument_name, model, connection_type, is_default, status, created_at, updated_at)
VALUES ('instrument-default', 'INS-DEMO-001', '演示仪器', 'MAS-6A', 'serial', 1, 'idle', $created_at, $updated_at);";
            instrumentCommand.Parameters.AddWithValue("$created_at", now);
            instrumentCommand.Parameters.AddWithValue("$updated_at", now);
            await instrumentCommand.ExecuteNonQueryAsync(cancellationToken);
        }

        await using (var libraryCommand = connection.CreateCommand())
        {
            libraryCommand.CommandText = @"
INSERT OR IGNORE INTO color_libraries (id, library_code, library_name, is_default, created_at, updated_at)
VALUES ('library-default', 'LIB-DEMO-001', '默认颜色库', 1, $created_at, $updated_at);";
            libraryCommand.Parameters.AddWithValue("$created_at", now);
            libraryCommand.Parameters.AddWithValue("$updated_at", now);
            await libraryCommand.ExecuteNonQueryAsync(cancellationToken);
        }
    }
}
