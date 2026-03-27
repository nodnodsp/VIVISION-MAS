using MAS.Core.Common;

namespace MAS.Core.Entities;

public sealed class RawPacket : EntityBase
{
    public string? TaskId { get; set; }
    public string? InstrumentId { get; set; }
    public string Direction { get; set; } = string.Empty;
    public string? PacketType { get; set; }
    public string? PacketHex { get; set; }
    public string? PacketText { get; set; }
}
