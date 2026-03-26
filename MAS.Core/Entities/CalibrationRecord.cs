using MAS.Core.Common;

namespace MAS.Core.Entities;

public sealed class CalibrationRecord : EntityBase
{
    public string InstrumentId { get; set; } = string.Empty;
    public string CalibrationType { get; set; } = string.Empty;
    public string ResultCode { get; set; } = string.Empty;
    public string? ErrorCode { get; set; }
    public string? ErrorMessage { get; set; }
    public string? OperatorId { get; set; }
    public DateTime StartedAt { get; set; } = DateTime.UtcNow;
    public DateTime? FinishedAt { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public string? Remark { get; set; }
}
