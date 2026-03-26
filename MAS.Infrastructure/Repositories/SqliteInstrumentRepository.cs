using MAS.Application.Abstractions;
using MAS.Core.Entities;
using MAS.Infrastructure.Database;
using Microsoft.Data.Sqlite;

namespace MAS.Infrastructure.Repositories;

public sealed class SqliteInstrumentRepository : IInstrumentRepository
{
    public async Task<IReadOnlyList<Instrument>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var items = new List<Instrument>();
        await using var connection = SqliteConnectionFactory.Create();
        await connection.OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = @"SELECT id, instrument_code, instrument_name, model, serial_number, connection_type, port_name, status, created_at, updated_at
FROM instruments ORDER BY instrument_code;";
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            items.Add(Map(reader));
        }

        return items;
    }

    public async Task<Instrument?> GetByIdAsync(string instrumentId, CancellationToken cancellationToken = default)
    {
        await using var connection = SqliteConnectionFactory.Create();
        await connection.OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = @"SELECT id, instrument_code, instrument_name, model, serial_number, connection_type, port_name, status, created_at, updated_at
FROM instruments WHERE id = $id LIMIT 1;";
        command.Parameters.AddWithValue("$id", instrumentId);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        return await reader.ReadAsync(cancellationToken) ? Map(reader) : null;
    }

    public async Task<Instrument?> GetDefaultAsync(CancellationToken cancellationToken = default)
    {
        await using var connection = SqliteConnectionFactory.Create();
        await connection.OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = @"SELECT id, instrument_code, instrument_name, model, serial_number, connection_type, port_name, status, created_at, updated_at
FROM instruments WHERE is_default = 1 ORDER BY instrument_code LIMIT 1;";
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        return await reader.ReadAsync(cancellationToken) ? Map(reader) : null;
    }

    public async Task UpdateAsync(Instrument instrument, CancellationToken cancellationToken = default)
    {
        await using var connection = SqliteConnectionFactory.Create();
        await connection.OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = @"
UPDATE instruments
SET instrument_code = $instrument_code,
    instrument_name = $instrument_name,
    model = $model,
    serial_number = $serial_number,
    connection_type = $connection_type,
    port_name = $port_name,
    status = $status,
    updated_at = $updated_at
WHERE id = $id;";
        command.Parameters.AddWithValue("$id", instrument.Id);
        command.Parameters.AddWithValue("$instrument_code", instrument.InstrumentCode);
        command.Parameters.AddWithValue("$instrument_name", instrument.InstrumentName);
        command.Parameters.AddWithValue("$model", instrument.Model);
        command.Parameters.AddWithValue("$serial_number", (object?)instrument.SerialNumber ?? DBNull.Value);
        command.Parameters.AddWithValue("$connection_type", instrument.ConnectionType);
        command.Parameters.AddWithValue("$port_name", (object?)instrument.PortName ?? DBNull.Value);
        command.Parameters.AddWithValue("$status", instrument.Status);
        command.Parameters.AddWithValue("$updated_at", instrument.UpdatedAt.ToString("O"));
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static Instrument Map(SqliteDataReader reader)
    {
        return new Instrument
        {
            Id = reader.GetString(0),
            InstrumentCode = reader.GetString(1),
            InstrumentName = reader.GetString(2),
            Model = reader.GetString(3),
            SerialNumber = reader.IsDBNull(4) ? null : reader.GetString(4),
            ConnectionType = reader.GetString(5),
            PortName = reader.IsDBNull(6) ? null : reader.GetString(6),
            Status = reader.GetString(7),
            CreatedAt = DateTime.Parse(reader.GetString(8)),
            UpdatedAt = DateTime.Parse(reader.GetString(9)),
        };
    }
}
