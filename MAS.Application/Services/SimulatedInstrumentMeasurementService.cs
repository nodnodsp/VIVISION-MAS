using MAS.Application.Abstractions;
using MAS.Application.Models;
using MAS.Core.Enums;

namespace MAS.Application.Services;

public sealed class SimulatedInstrumentMeasurementService : IInstrumentMeasurementService
{
    private static readonly string[] TrialAngles = ["15as-15", "45as45", "45as110"];
    private static readonly string[] StandardAngles = ["15as-15", "45as110"];

    public Task<InstrumentMeasurementResult> MeasureAsync(InstrumentMeasurementRequest request, CancellationToken cancellationToken = default)
    {
        var seed = HashCode.Combine(request.TaskCode, request.RecordType, request.SequenceNo, request.TaskType);
        var random = new Random(seed);
        var isTrial = string.Equals(request.RecordType, "trial", StringComparison.OrdinalIgnoreCase);
        var angles = isTrial ? TrialAngles : StandardAngles;
        var angleResults = new List<InstrumentMeasurementAngleResult>();

        foreach (var angle in angles)
        {
            var deltaE = Math.Round(0.18 + random.NextDouble() * (isTrial ? 0.95 : 0.45), 2);
            var passStatus = deltaE <= 0.8 ? PassStatus.Pass : deltaE <= 1.15 ? PassStatus.Warning : PassStatus.Fail;
            angleResults.Add(new InstrumentMeasurementAngleResult
            {
                AngleCode = angle,
                CieL = Math.Round(50 + random.NextDouble() * 15, 2),
                CieA = Math.Round(-2 + random.NextDouble() * 4, 2),
                CieB = Math.Round(-4 + random.NextDouble() * 6, 2),
                DeltaE = deltaE,
                PassStatus = passStatus,
            });
        }

        var sparkleValue = Math.Round(3 + random.NextDouble() * 2.5, 2);
        var sparkleDiff = Math.Round(0.05 + random.NextDouble() * 0.35, 2);
        var graininessValue = Math.Round(1.5 + random.NextDouble() * 1.2, 2);
        var graininessDiff = Math.Round(0.04 + random.NextDouble() * 0.28, 2);
        var totalDeltaE = Math.Round(angleResults.Average(x => x.DeltaE ?? 0), 2);
        var totalEffectDiff = Math.Round((sparkleDiff + graininessDiff) / 2, 2);
        var pass = EvaluatePassStatus(totalDeltaE, totalEffectDiff);

        var effectResults = new List<InstrumentMeasurementEffectResult>
        {
            new()
            {
                AngleCode = angles.Last(),
                SparkleValue = sparkleValue,
                SparkleDiff = sparkleDiff,
                GraininessValue = graininessValue,
                GraininessDiff = graininessDiff,
                PassStatus = pass,
            }
        };

        var summary = isTrial
            ? $"试样测量完成，综合色差 {totalDeltaE:F2}，效果差 {totalEffectDiff:F2}。"
            : $"标准样测量完成，标准数据已刷新，综合色差 {totalDeltaE:F2}。";

        return Task.FromResult(new InstrumentMeasurementResult
        {
            TotalDeltaE = totalDeltaE,
            TotalEffectDiff = totalEffectDiff,
            PassStatus = pass,
            ResultSummary = summary,
            AngleResults = angleResults,
            EffectResults = effectResults,
        });
    }

    private static PassStatus EvaluatePassStatus(double totalDeltaE, double totalEffectDiff)
    {
        if (totalDeltaE <= 0.8 && totalEffectDiff <= 0.2)
        {
            return PassStatus.Pass;
        }

        if (totalDeltaE <= 1.15 && totalEffectDiff <= 0.35)
        {
            return PassStatus.Warning;
        }

        return PassStatus.Fail;
    }
}
