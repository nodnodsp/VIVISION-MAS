using MAS.Core.Enums;

namespace MAS.Application.Models;

public sealed class InstrumentMeasurementAngleResult
{
    public string AngleCode { get; init; } = string.Empty;
    public double? CieL { get; init; }
    public double? CieA { get; init; }
    public double? CieB { get; init; }
    public double? DeltaE { get; init; }
    public PassStatus PassStatus { get; init; } = PassStatus.Pass;
}
