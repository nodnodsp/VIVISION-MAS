using MAS.Application.Abstractions;
using MAS.Core.Entities;
using MAS.Infrastructure.Database;
using Microsoft.Data.Sqlite;

namespace MAS.Infrastructure.Repositories;

public sealed class SqliteMeasurementTaskRepository : IMeasurementTaskRepository
{
    public async Task AddAsync(MeasurementTask task, CancellationToken cancellationToken = default)
    {
        await using var connection = SqliteConnectionFactory.Create();
        await connection.OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = @"
INSERT INTO measurement_tasks (id, task_code, instrument_id, sample_id, standard_sample_id, template_id, task_type, measurement_mode, average_count, interval_seconds, status, created_by, started_at, finished_at, remark, created_at, updated_at)
VALUES ($id, $task_code, $instrument_id, $sample_id, $standard_sample_id, $template_id, $task_type, $measurement_mode, $average_count, $interval_seconds, $status, $created_by, $started_at, $finished_at, $remark, $created_at, $updated_at);";
        Bind(command, task);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task UpdateAsync(MeasurementTask task, CancellationToken cancellationToken = default)
    {
        await using var connection = SqliteConnectionFactory.Create();
        await connection.OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = @"
UPDATE measurement_tasks
SET instrument_id = $instrument_id,
    sample_id = $sample_id,
    standard_sample_id = $standard_sample_id,
    template_id = $template_id,
    task_type = $task_type,
    measurement_mode = $measurement_mode,
    average_count = $average_count,
    interval_seconds = $interval_seconds,
    status = $status,
    started_at = $started_at,
    finished_at = $finished_at,
    updated_at = $updated_at
WHERE id = $id;";
        Bind(command, task, includeTaskCode: false, includeCreatedAt: false);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task<MeasurementTask?> GetByIdAsync(string taskId, CancellationToken cancellationToken = default)
    {
        await using var connection = SqliteConnectionFactory.Create();
        await connection.OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = @"SELECT id, task_code, instrument_id, sample_id, standard_sample_id, template_id, task_type, measurement_mode, average_count, interval_seconds, status, started_at, finished_at, created_at, updated_at
FROM measurement_tasks WHERE id = $id LIMIT 1;";
        command.Parameters.AddWithValue("$id", taskId);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        return await reader.ReadAsync(cancellationToken) ? Map(reader) : null;
    }

    public async Task<MeasurementTask?> GetByCodeAsync(string taskCode, CancellationToken cancellationToken = default)
    {
        await using var connection = SqliteConnectionFactory.Create();
        await connection.OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = @"SELECT id, task_code, instrument_id, sample_id, standard_sample_id, template_id, task_type, measurement_mode, average_count, interval_seconds, status, started_at, finished_at, created_at, updated_at
FROM measurement_tasks WHERE task_code = $task_code LIMIT 1;";
        command.Parameters.AddWithValue("$task_code", taskCode);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        return await reader.ReadAsync(cancellationToken) ? Map(reader) : null;
    }

    public async Task<IReadOnlyList<MeasurementTask>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var items = new List<MeasurementTask>();
        await using var connection = SqliteConnectionFactory.Create();
        await connection.OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = @"SELECT id, task_code, instrument_id, sample_id, standard_sample_id, template_id, task_type, measurement_mode, average_count, interval_seconds, status, started_at, finished_at, created_at, updated_at
FROM measurement_tasks ORDER BY created_at;";
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            items.Add(Map(reader));
        }

        return items;
    }

    private static void Bind(SqliteCommand command, MeasurementTask task, bool includeTaskCode = true, bool includeCreatedAt = true)
    {
        command.Parameters.AddWithValue("$id", task.Id);
        if (includeTaskCode)
        {
            command.Parameters.AddWithValue("$task_code", task.TaskCode);
            command.Parameters.AddWithValue("$created_by", DBNull.Value);
            command.Parameters.AddWithValue("$remark", DBNull.Value);
        }

        command.Parameters.AddWithValue("$instrument_id", task.InstrumentId);
        command.Parameters.AddWithValue("$sample_id", (object?)task.SampleId ?? DBNull.Value);
        command.Parameters.AddWithValue("$standard_sample_id", (object?)task.StandardSampleId ?? DBNull.Value);
        command.Parameters.AddWithValue("$template_id", (object?)task.TemplateId ?? DBNull.Value);
        command.Parameters.AddWithValue("$task_type", task.TaskType);
        command.Parameters.AddWithValue("$measurement_mode", task.MeasurementMode.ToString());
        command.Parameters.AddWithValue("$average_count", task.AverageCount);
        command.Parameters.AddWithValue("$interval_seconds", task.IntervalSeconds);
        command.Parameters.AddWithValue("$status", task.Status.ToString());
        command.Parameters.AddWithValue("$started_at", (object?)task.StartedAt?.ToString("O") ?? DBNull.Value);
        command.Parameters.AddWithValue("$finished_at", (object?)task.FinishedAt?.ToString("O") ?? DBNull.Value);
        if (includeCreatedAt)
        {
            command.Parameters.AddWithValue("$created_at", task.CreatedAt.ToString("O"));
        }
        command.Parameters.AddWithValue("$updated_at", task.UpdatedAt.ToString("O"));
    }

    private static MeasurementTask Map(SqliteDataReader reader)
    {
        return new MeasurementTask
        {
            Id = reader.GetString(0),
            TaskCode = reader.GetString(1),
            InstrumentId = reader.GetString(2),
            SampleId = reader.IsDBNull(3) ? null : reader.GetString(3),
            StandardSampleId = reader.IsDBNull(4) ? null : reader.GetString(4),
            TemplateId = reader.IsDBNull(5) ? null : reader.GetString(5),
            TaskType = reader.GetString(6),
            MeasurementMode = Enum.Parse<MAS.Core.Enums.MeasurementMode>(reader.GetString(7)),
            AverageCount = reader.IsDBNull(8) ? 1 : reader.GetInt32(8),
            IntervalSeconds = reader.IsDBNull(9) ? 5 : reader.GetInt32(9),
            Status = Enum.Parse<MAS.Core.Enums.TaskStatus>(reader.GetString(10)),
            StartedAt = reader.IsDBNull(11) ? null : DateTime.Parse(reader.GetString(11)),
            FinishedAt = reader.IsDBNull(12) ? null : DateTime.Parse(reader.GetString(12)),
            CreatedAt = DateTime.Parse(reader.GetString(13)),
            UpdatedAt = DateTime.Parse(reader.GetString(14)),
        };
    }
}
