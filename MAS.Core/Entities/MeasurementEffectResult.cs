using MAS.Core.Common;
using MAS.Core.Enums;

namespace MAS.Core.Entities;

public sealed class MeasurementEffectResult : EntityBase
{
    public string RecordId { get; set; } = string.Empty;
    public string? AngleCode { get; set; }
    public double? SparkleValue { get; set; }
    public double? SparkleDiff { get; set; }
    public double? GraininessValue { get; set; }
    public double? GraininessDiff { get; set; }
    public PassStatus EffectPassStatus { get; set; } = PassStatus.Pass;
}
