using MAS.Core.Enums;

namespace MAS.Application.Models;

public sealed class InstrumentMeasurementResult
{
    public double? TotalDeltaE { get; init; }
    public double? TotalEffectDiff { get; init; }
    public PassStatus PassStatus { get; init; } = PassStatus.Pass;
    public string ResultSummary { get; init; } = string.Empty;
    public IReadOnlyList<InstrumentMeasurementAngleResult> AngleResults { get; init; } = Array.Empty<InstrumentMeasurementAngleResult>();
    public IReadOnlyList<InstrumentMeasurementEffectResult> EffectResults { get; init; } = Array.Empty<InstrumentMeasurementEffectResult>();
}
