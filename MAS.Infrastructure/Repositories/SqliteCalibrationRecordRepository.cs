using MAS.Application.Abstractions;
using MAS.Core.Entities;
using MAS.Infrastructure.Database;
using Microsoft.Data.Sqlite;

namespace MAS.Infrastructure.Repositories;

public sealed class SqliteCalibrationRecordRepository : ICalibrationRecordRepository
{
    public async Task AddAsync(CalibrationRecord record, CancellationToken cancellationToken = default)
    {
        await using var connection = SqliteConnectionFactory.Create();
        await connection.OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = @"
INSERT INTO calibration_records (id, instrument_id, calibration_type, result_code, error_code, error_message, operator_id, started_at, finished_at, expires_at, remark)
VALUES ($id, $instrument_id, $calibration_type, $result_code, $error_code, $error_message, $operator_id, $started_at, $finished_at, $expires_at, $remark);";
        command.Parameters.AddWithValue("$id", record.Id);
        command.Parameters.AddWithValue("$instrument_id", record.InstrumentId);
        command.Parameters.AddWithValue("$calibration_type", record.CalibrationType);
        command.Parameters.AddWithValue("$result_code", record.ResultCode);
        command.Parameters.AddWithValue("$error_code", (object?)record.ErrorCode ?? DBNull.Value);
        command.Parameters.AddWithValue("$error_message", (object?)record.ErrorMessage ?? DBNull.Value);
        command.Parameters.AddWithValue("$operator_id", (object?)record.OperatorId ?? DBNull.Value);
        command.Parameters.AddWithValue("$started_at", record.StartedAt.ToString("O"));
        command.Parameters.AddWithValue("$finished_at", (object?)record.FinishedAt?.ToString("O") ?? DBNull.Value);
        command.Parameters.AddWithValue("$expires_at", (object?)record.ExpiresAt?.ToString("O") ?? DBNull.Value);
        command.Parameters.AddWithValue("$remark", (object?)record.Remark ?? DBNull.Value);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<CalibrationRecord>> GetByInstrumentIdAsync(string instrumentId, CancellationToken cancellationToken = default)
    {
        var items = new List<CalibrationRecord>();
        await using var connection = SqliteConnectionFactory.Create();
        await connection.OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = @"SELECT id, instrument_id, calibration_type, result_code, error_code, error_message, operator_id, started_at, finished_at, expires_at, remark
FROM calibration_records WHERE instrument_id = $instrument_id ORDER BY started_at DESC;";
        command.Parameters.AddWithValue("$instrument_id", instrumentId);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            items.Add(Map(reader));
        }

        return items;
    }

    public async Task<IReadOnlyList<CalibrationRecord>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var items = new List<CalibrationRecord>();
        await using var connection = SqliteConnectionFactory.Create();
        await connection.OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = @"SELECT id, instrument_id, calibration_type, result_code, error_code, error_message, operator_id, started_at, finished_at, expires_at, remark
FROM calibration_records ORDER BY started_at DESC;";
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            items.Add(Map(reader));
        }

        return items;
    }

    private static CalibrationRecord Map(SqliteDataReader reader)
    {
        return new CalibrationRecord
        {
            Id = reader.GetString(0),
            InstrumentId = reader.GetString(1),
            CalibrationType = reader.GetString(2),
            ResultCode = reader.GetString(3),
            ErrorCode = reader.IsDBNull(4) ? null : reader.GetString(4),
            ErrorMessage = reader.IsDBNull(5) ? null : reader.GetString(5),
            OperatorId = reader.IsDBNull(6) ? null : reader.GetString(6),
            StartedAt = DateTime.Parse(reader.GetString(7)),
            FinishedAt = reader.IsDBNull(8) ? null : DateTime.Parse(reader.GetString(8)),
            ExpiresAt = reader.IsDBNull(9) ? null : DateTime.Parse(reader.GetString(9)),
            Remark = reader.IsDBNull(10) ? null : reader.GetString(10),
        };
    }
}
