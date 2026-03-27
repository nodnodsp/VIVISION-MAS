using System.Text;
using MAS.Application.Abstractions;
using MAS.Core.Entities;

namespace MAS.Application.Services;

internal static class RawPacketLogHelper
{
    public static Task LogAsync(
        IRawPacketRepository repository,
        string direction,
        string packetType,
        string packetText,
        string? instrumentId = null,
        string? taskId = null,
        CancellationToken cancellationToken = default)
    {
        var payload = packetText ?? string.Empty;
        var packet = new RawPacket
        {
            TaskId = taskId,
            InstrumentId = instrumentId,
            Direction = direction,
            PacketType = packetType,
            PacketText = payload,
            PacketHex = Convert.ToHexString(Encoding.UTF8.GetBytes(payload)),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        };

        return repository.AddAsync(packet, cancellationToken);
    }
}
