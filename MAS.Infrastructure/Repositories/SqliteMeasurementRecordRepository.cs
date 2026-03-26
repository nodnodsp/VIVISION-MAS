using MAS.Application.Abstractions;
using MAS.Core.Entities;
using MAS.Core.Enums;
using MAS.Infrastructure.Database;
using Microsoft.Data.Sqlite;

namespace MAS.Infrastructure.Repositories;

public sealed class SqliteMeasurementRecordRepository : IMeasurementRecordRepository
{
    public async Task AddAsync(MeasurementRecord record, CancellationToken cancellationToken = default)
    {
        await using var connection = SqliteConnectionFactory.Create();
        await connection.OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = @"
INSERT INTO measurement_records (id, task_id, record_no, record_type, total_delta_e, total_effect_diff, pass_status, result_summary, measurement_snapshot_json, measured_at, created_at)
VALUES ($id, $task_id, $record_no, $record_type, $total_delta_e, $total_effect_diff, $pass_status, $result_summary, $measurement_snapshot_json, $measured_at, $created_at);";
        command.Parameters.AddWithValue("$id", record.Id);
        command.Parameters.AddWithValue("$task_id", record.TaskId);
        command.Parameters.AddWithValue("$record_no", record.RecordNo);
        command.Parameters.AddWithValue("$record_type", record.RecordType);
        command.Parameters.AddWithValue("$total_delta_e", (object?)record.TotalDeltaE ?? DBNull.Value);
        command.Parameters.AddWithValue("$total_effect_diff", (object?)record.TotalEffectDiff ?? DBNull.Value);
        command.Parameters.AddWithValue("$pass_status", record.PassStatus.ToString());
        command.Parameters.AddWithValue("$result_summary", (object?)record.ResultSummary ?? DBNull.Value);
        command.Parameters.AddWithValue("$measurement_snapshot_json", DBNull.Value);
        command.Parameters.AddWithValue("$measured_at", record.MeasuredAt.ToString("O"));
        command.Parameters.AddWithValue("$created_at", record.CreatedAt.ToString("O"));
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task<MeasurementRecord?> GetByIdAsync(string recordId, CancellationToken cancellationToken = default)
    {
        await using var connection = SqliteConnectionFactory.Create();
        await connection.OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = @"SELECT id, task_id, record_no, record_type, total_delta_e, total_effect_diff, pass_status, result_summary, measured_at, created_at
FROM measurement_records WHERE id = $id LIMIT 1;";
        command.Parameters.AddWithValue("$id", recordId);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        return await reader.ReadAsync(cancellationToken) ? Map(reader) : null;
    }

    public async Task<IReadOnlyList<MeasurementRecord>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var items = new List<MeasurementRecord>();
        await using var connection = SqliteConnectionFactory.Create();
        await connection.OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = @"SELECT id, task_id, record_no, record_type, total_delta_e, total_effect_diff, pass_status, result_summary, measured_at, created_at
FROM measurement_records ORDER BY measured_at DESC, record_no DESC;";
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            items.Add(Map(reader));
        }

        return items;
    }

    public async Task<IReadOnlyList<MeasurementRecord>> GetByTaskIdAsync(string taskId, CancellationToken cancellationToken = default)
    {
        var items = new List<MeasurementRecord>();
        await using var connection = SqliteConnectionFactory.Create();
        await connection.OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = @"SELECT id, task_id, record_no, record_type, total_delta_e, total_effect_diff, pass_status, result_summary, measured_at, created_at
FROM measurement_records WHERE task_id = $task_id ORDER BY measured_at DESC, record_no DESC;";
        command.Parameters.AddWithValue("$task_id", taskId);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            items.Add(Map(reader));
        }

        return items;
    }

    private static MeasurementRecord Map(SqliteDataReader reader)
    {
        return new MeasurementRecord
        {
            Id = reader.GetString(0),
            TaskId = reader.GetString(1),
            RecordNo = reader.GetInt32(2),
            RecordType = reader.GetString(3),
            TotalDeltaE = reader.IsDBNull(4) ? null : reader.GetDouble(4),
            TotalEffectDiff = reader.IsDBNull(5) ? null : reader.GetDouble(5),
            PassStatus = Enum.Parse<PassStatus>(reader.GetString(6)),
            ResultSummary = reader.IsDBNull(7) ? null : reader.GetString(7),
            MeasuredAt = DateTime.Parse(reader.GetString(8)),
            CreatedAt = DateTime.Parse(reader.GetString(9)),
        };
    }
}
