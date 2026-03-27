using MAS.Application.Abstractions;
using MAS.Core.Entities;
using MAS.Infrastructure.Database;

namespace MAS.Infrastructure.Repositories;

public sealed class SqliteRawPacketRepository : IRawPacketRepository
{
    public async Task AddAsync(RawPacket packet, CancellationToken cancellationToken = default)
    {
        await using var connection = SqliteConnectionFactory.Create();
        await connection.OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = @"
INSERT INTO raw_packets (id, task_id, instrument_id, direction, packet_type, packet_hex, packet_text, created_at)
VALUES ($id, $task_id, $instrument_id, $direction, $packet_type, $packet_hex, $packet_text, $created_at);";
        command.Parameters.AddWithValue("$id", packet.Id);
        command.Parameters.AddWithValue("$task_id", (object?)packet.TaskId ?? DBNull.Value);
        command.Parameters.AddWithValue("$instrument_id", (object?)packet.InstrumentId ?? DBNull.Value);
        command.Parameters.AddWithValue("$direction", packet.Direction);
        command.Parameters.AddWithValue("$packet_type", (object?)packet.PacketType ?? DBNull.Value);
        command.Parameters.AddWithValue("$packet_hex", (object?)packet.PacketHex ?? DBNull.Value);
        command.Parameters.AddWithValue("$packet_text", (object?)packet.PacketText ?? DBNull.Value);
        command.Parameters.AddWithValue("$created_at", packet.CreatedAt.ToString("O"));
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public Task<IReadOnlyList<RawPacket>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return GetInternalAsync(null, cancellationToken);
    }

    public Task<IReadOnlyList<RawPacket>> GetRecentAsync(int take, CancellationToken cancellationToken = default)
    {
        return GetInternalAsync(take, cancellationToken);
    }

    private static async Task<IReadOnlyList<RawPacket>> GetInternalAsync(int? take, CancellationToken cancellationToken)
    {
        var items = new List<RawPacket>();
        await using var connection = SqliteConnectionFactory.Create();
        await connection.OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = $@"SELECT id, task_id, instrument_id, direction, packet_type, packet_hex, packet_text, created_at
FROM raw_packets ORDER BY created_at DESC{(take.HasValue ? $" LIMIT {take.Value}" : string.Empty)};";
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            items.Add(new RawPacket
            {
                Id = reader.GetString(0),
                TaskId = reader.IsDBNull(1) ? null : reader.GetString(1),
                InstrumentId = reader.IsDBNull(2) ? null : reader.GetString(2),
                Direction = reader.GetString(3),
                PacketType = reader.IsDBNull(4) ? null : reader.GetString(4),
                PacketHex = reader.IsDBNull(5) ? null : reader.GetString(5),
                PacketText = reader.IsDBNull(6) ? null : reader.GetString(6),
                CreatedAt = DateTime.Parse(reader.GetString(7)),
            });
        }

        return items;
    }
}
