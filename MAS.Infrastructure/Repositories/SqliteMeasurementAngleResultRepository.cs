using MAS.Application.Abstractions;
using MAS.Core.Entities;
using MAS.Core.Enums;
using MAS.Infrastructure.Database;
using Microsoft.Data.Sqlite;

namespace MAS.Infrastructure.Repositories;

public sealed class SqliteMeasurementAngleResultRepository : IMeasurementAngleResultRepository
{
    public async Task AddRangeAsync(IEnumerable<MeasurementAngleResult> results, CancellationToken cancellationToken = default)
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
INSERT INTO measurement_angle_results (id, record_id, angle_code, cie_l, cie_a, cie_b, cie_c, cie_h, cie_x, cie_y, cie_z, delta_l, delta_a, delta_b, delta_c, delta_h, delta_e, pass_status, raw_value_json)
VALUES ($id, $record_id, $angle_code, $cie_l, $cie_a, $cie_b, $cie_c, $cie_h, $cie_x, $cie_y, $cie_z, $delta_l, $delta_a, $delta_b, $delta_c, $delta_h, $delta_e, $pass_status, $raw_value_json);";
            command.Parameters.AddWithValue("$id", result.Id);
            command.Parameters.AddWithValue("$record_id", result.RecordId);
            command.Parameters.AddWithValue("$angle_code", result.AngleCode);
            command.Parameters.AddWithValue("$cie_l", (object?)result.CieL ?? DBNull.Value);
            command.Parameters.AddWithValue("$cie_a", (object?)result.CieA ?? DBNull.Value);
            command.Parameters.AddWithValue("$cie_b", (object?)result.CieB ?? DBNull.Value);
            command.Parameters.AddWithValue("$cie_c", DBNull.Value);
            command.Parameters.AddWithValue("$cie_h", DBNull.Value);
            command.Parameters.AddWithValue("$cie_x", DBNull.Value);
            command.Parameters.AddWithValue("$cie_y", DBNull.Value);
            command.Parameters.AddWithValue("$cie_z", DBNull.Value);
            command.Parameters.AddWithValue("$delta_l", DBNull.Value);
            command.Parameters.AddWithValue("$delta_a", DBNull.Value);
            command.Parameters.AddWithValue("$delta_b", DBNull.Value);
            command.Parameters.AddWithValue("$delta_c", DBNull.Value);
            command.Parameters.AddWithValue("$delta_h", DBNull.Value);
            command.Parameters.AddWithValue("$delta_e", (object?)result.DeltaE ?? DBNull.Value);
            command.Parameters.AddWithValue("$pass_status", result.PassStatus.ToString());
            command.Parameters.AddWithValue("$raw_value_json", (object?)result.RawValueJson ?? DBNull.Value);
            await command.ExecuteNonQueryAsync(cancellationToken);
        }

        await transaction.CommitAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<MeasurementAngleResult>> GetByRecordIdAsync(string recordId, CancellationToken cancellationToken = default)
    {
        var items = new List<MeasurementAngleResult>();
        await using var connection = SqliteConnectionFactory.Create();
        await connection.OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = @"SELECT id, record_id, angle_code, cie_l, cie_a, cie_b, delta_e, pass_status, raw_value_json
FROM measurement_angle_results WHERE record_id = $record_id ORDER BY angle_code;";
        command.Parameters.AddWithValue("$record_id", recordId);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            items.Add(new MeasurementAngleResult
            {
                Id = reader.GetString(0),
                RecordId = reader.GetString(1),
                AngleCode = reader.GetString(2),
                CieL = reader.IsDBNull(3) ? null : reader.GetDouble(3),
                CieA = reader.IsDBNull(4) ? null : reader.GetDouble(4),
                CieB = reader.IsDBNull(5) ? null : reader.GetDouble(5),
                DeltaE = reader.IsDBNull(6) ? null : reader.GetDouble(6),
                PassStatus = Enum.Parse<PassStatus>(reader.GetString(7)),
                RawValueJson = reader.IsDBNull(8) ? null : reader.GetString(8),
            });
        }

        return items;
    }
}

