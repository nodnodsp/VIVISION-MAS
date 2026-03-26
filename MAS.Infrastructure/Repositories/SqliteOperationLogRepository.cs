using MAS.Application.Abstractions;
using MAS.Core.Entities;
using MAS.Infrastructure.Database;
using Microsoft.Data.Sqlite;

namespace MAS.Infrastructure.Repositories;

public sealed class SqliteOperationLogRepository : IOperationLogRepository
{
    public async Task AddAsync(OperationLog log, CancellationToken cancellationToken = default)
    {
        await using var connection = SqliteConnectionFactory.Create();
        await connection.OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = @"
INSERT INTO operation_logs (id, task_id, record_id, operator_id, module_name, operation_type, operation_desc, operation_result, created_at)
VALUES ($id, $task_id, $record_id, $operator_id, $module_name, $operation_type, $operation_desc, $operation_result, $created_at);";
        command.Parameters.AddWithValue("$id", log.Id);
        command.Parameters.AddWithValue("$task_id", (object?)log.TaskId ?? DBNull.Value);
        command.Parameters.AddWithValue("$record_id", (object?)log.RecordId ?? DBNull.Value);
        command.Parameters.AddWithValue("$operator_id", (object?)log.OperatorId ?? DBNull.Value);
        command.Parameters.AddWithValue("$module_name", log.ModuleName);
        command.Parameters.AddWithValue("$operation_type", log.OperationType);
        command.Parameters.AddWithValue("$operation_desc", (object?)log.OperationDesc ?? DBNull.Value);
        command.Parameters.AddWithValue("$operation_result", log.OperationResult);
        command.Parameters.AddWithValue("$created_at", log.CreatedAt.ToString("O"));
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<OperationLog>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await GetInternalAsync(null, cancellationToken);
    }

    public async Task<IReadOnlyList<OperationLog>> GetRecentAsync(int take, CancellationToken cancellationToken = default)
    {
        return await GetInternalAsync(take, cancellationToken);
    }

    private static async Task<IReadOnlyList<OperationLog>> GetInternalAsync(int? take, CancellationToken cancellationToken)
    {
        var items = new List<OperationLog>();
        await using var connection = SqliteConnectionFactory.Create();
        await connection.OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = $@"SELECT id, task_id, record_id, operator_id, module_name, operation_type, operation_desc, operation_result, created_at
FROM operation_logs ORDER BY created_at DESC{(take.HasValue ? $" LIMIT {take.Value}" : string.Empty)};";
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            items.Add(new OperationLog
            {
                Id = reader.GetString(0),
                TaskId = reader.IsDBNull(1) ? null : reader.GetString(1),
                RecordId = reader.IsDBNull(2) ? null : reader.GetString(2),
                OperatorId = reader.IsDBNull(3) ? null : reader.GetString(3),
                ModuleName = reader.GetString(4),
                OperationType = reader.GetString(5),
                OperationDesc = reader.IsDBNull(6) ? null : reader.GetString(6),
                OperationResult = reader.GetString(7),
                CreatedAt = DateTime.Parse(reader.GetString(8)),
            });
        }

        return items;
    }
}
