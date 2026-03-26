using MAS.Core.Common;

namespace MAS.Core.Entities;

public sealed class Instrument : EntityBase
{
    public string InstrumentCode { get; set; } = string.Empty;
    public string InstrumentName { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public string? SerialNumber { get; set; }
    public string ConnectionType { get; set; } = "serial";
    public string? PortName { get; set; }
    public string Status { get; set; } = "idle";
}
