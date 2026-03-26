using System.Reflection;
using MAS.Application.Abstractions;
using Microsoft.Data.Sqlite;

namespace MAS.Infrastructure.Database;

public sealed class SqliteScriptBootstrapper : IDatabaseBootstrapper
{
    public string DatabasePath => DatabasePaths.DatabaseFile;

    public string SchemaScriptPath => DatabasePaths.SchemaResourceName;

    public async Task EnsureCreatedAsync(CancellationToken cancellationToken = default)
    {
        Directory.CreateDirectory(DatabasePaths.Root);

        var schemaSql = await ReadSchemaAsync(cancellationToken);
        var connectionString = new SqliteConnectionStringBuilder
        {
            DataSource = DatabasePath,
            ForeignKeys = true,
        }.ToString();

        await using var connection = new SqliteConnection(connectionString);
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = schemaSql;
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static async Task<string> ReadSchemaAsync(CancellationToken cancellationToken)
    {
        var assembly = typeof(SqliteScriptBootstrapper).Assembly;
        await using var stream = assembly.GetManifestResourceStream(DatabasePaths.SchemaResourceName)
            ?? throw new FileNotFoundException($"Embedded schema not found: {DatabasePaths.SchemaResourceName}");
        using var reader = new StreamReader(stream);
        var content = await reader.ReadToEndAsync(cancellationToken);
        if (string.IsNullOrWhiteSpace(content))
        {
            throw new InvalidOperationException("Schema script is empty.");
        }

        return content;
    }
}
