using MAS.Application.Abstractions;
using MAS.Core.Entities;
using MAS.Core.Enums;
using MAS.Infrastructure.Database;
using Microsoft.Data.Sqlite;

namespace MAS.Infrastructure.Repositories;

public sealed class SqliteMeasurementEffectResultRepository : IMeasurementEffectResultRepository
{
    public async Task AddRangeAsync(IEnumerable<MeasurementEffectResult> results, CancellationToken cancellationToken = default)
    {
        var items = results.ToList();
        if (items.Count == 0)
        {
            return;
        }

        await using var connection = SqliteConnectionFactory.Create();
        await connection.OpenAsync(cancellationToken);
        await using var transaction = (SqliteTransaction)await connection.BeginTransactionAsync(cancellationToken);

        foreach (var result in items)
        {
            await using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = @"
INSERT INTO measurement_effect_results (id, record_id, angle_code, sparkle_value, sparkle_diff, graininess_value, graininess_diff, effect_pass_status, raw_effect_json)
VALUES ($id, $record_id, $angle_code, $sparkle_value, $sparkle_diff, $graininess_value, $graininess_diff, $effect_pass_status, $raw_effect_json);";
            command.Parameters.AddWithValue("$id", result.Id);
            command.Parameters.AddWithValue("$record_id", result.RecordId);
            command.Parameters.AddWithValue("$angle_code", (object?)result.AngleCode ?? DBNull.Value);
            command.Parameters.AddWithValue("$sparkle_value", (object?)result.SparkleValue ?? DBNull.Value);
            command.Parameters.AddWithValue("$sparkle_diff", (object?)result.SparkleDiff ?? DBNull.Value);
            command.Parameters.AddWithValue("$graininess_value", (object?)result.GraininessValue ?? DBNull.Value);
            command.Parameters.AddWithValue("$graininess_diff", (object?)result.GraininessDiff ?? DBNull.Value);
            command.Parameters.AddWithValue("$effect_pass_status", result.EffectPassStatus.ToString());
            command.Parameters.AddWithValue("$raw_effect_json", DBNull.Value);
            await command.ExecuteNonQueryAsync(cancellationToken);
        }

        await transaction.CommitAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<MeasurementEffectResult>> GetByRecordIdAsync(string recordId, CancellationToken cancellationToken = default)
    {
        var items = new List<MeasurementEffectResult>();
        await using var connection = SqliteConnectionFactory.Create();
        await connection.OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = @"SELECT id, record_id, angle_code, sparkle_value, sparkle_diff, graininess_value, graininess_diff, effect_pass_status
FROM measurement_effect_results WHERE record_id = $record_id ORDER BY angle_code;";
        command.Parameters.AddWithValue("$record_id", recordId);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            items.Add(new MeasurementEffectResult
            {
                Id = reader.GetString(0),
                RecordId = reader.GetString(1),
                AngleCode = reader.IsDBNull(2) ? null : reader.GetString(2),
                SparkleValue = reader.IsDBNull(3) ? null : reader.GetDouble(3),
                SparkleDiff = reader.IsDBNull(4) ? null : reader.GetDouble(4),
                GraininessValue = reader.IsDBNull(5) ? null : reader.GetDouble(5),
                GraininessDiff = reader.IsDBNull(6) ? null : reader.GetDouble(6),
                EffectPassStatus = Enum.Parse<PassStatus>(reader.GetString(7)),
            });
        }

        return items;
    }
}

