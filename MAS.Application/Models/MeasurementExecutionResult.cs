using MAS.Core.Entities;

namespace MAS.Application.Models;

public sealed class MeasurementExecutionResult
{
    public MeasurementTask Task { get; init; } = null!;
    public MeasurementRecord Record { get; init; } = null!;
    public IReadOnlyList<MeasurementAngleResult> AngleResults { get; init; } = Array.Empty<MeasurementAngleResult>();
    public IReadOnlyList<MeasurementEffectResult> EffectResults { get; init; } = Array.Empty<MeasurementEffectResult>();
}
