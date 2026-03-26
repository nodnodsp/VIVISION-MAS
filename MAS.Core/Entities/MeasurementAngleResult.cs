using MAS.Core.Common;
using MAS.Core.Enums;

namespace MAS.Core.Entities;

public sealed class MeasurementAngleResult : EntityBase
{
    public string RecordId { get; set; } = string.Empty;
    public string AngleCode { get; set; } = string.Empty;
    public double? CieL { get; set; }
    public double? CieA { get; set; }
    public double? CieB { get; set; }
    public double? DeltaE { get; set; }
    public PassStatus PassStatus { get; set; } = PassStatus.Pass;
    public string? RawValueJson { get; set; }
}
