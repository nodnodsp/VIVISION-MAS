using MAS.Core.Common;
using MAS.Core.Enums;

namespace MAS.Core.Entities;

public sealed class MeasurementRecord : EntityBase
{
    public string TaskId { get; set; } = string.Empty;
    public int RecordNo { get; set; }
    public string RecordType { get; set; } = "trial";
    public double? TotalDeltaE { get; set; }
    public double? TotalEffectDiff { get; set; }
    public PassStatus PassStatus { get; set; } = PassStatus.Pass;
    public string? ResultSummary { get; set; }
    public DateTime MeasuredAt { get; set; } = DateTime.UtcNow;
}
