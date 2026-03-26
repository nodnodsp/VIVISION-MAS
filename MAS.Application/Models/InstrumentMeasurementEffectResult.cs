using MAS.Core.Enums;

namespace MAS.Application.Models;

public sealed class InstrumentMeasurementEffectResult
{
    public string? AngleCode { get; init; }
    public double? SparkleValue { get; init; }
    public double? SparkleDiff { get; init; }
    public double? GraininessValue { get; init; }
    public double? GraininessDiff { get; init; }
    public PassStatus PassStatus { get; init; } = PassStatus.Pass;
}
