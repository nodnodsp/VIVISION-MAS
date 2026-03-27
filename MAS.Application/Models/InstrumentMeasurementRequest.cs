using MAS.Core.Enums;

namespace MAS.Application.Models;

public sealed class InstrumentMeasurementRequest
{
    public string TaskId { get; init; } = string.Empty;
    public string InstrumentId { get; init; } = string.Empty;
    public string TaskCode { get; init; } = string.Empty;
    public string TaskType { get; init; } = string.Empty;
    public string RecordType { get; init; } = string.Empty;
    public MeasurementMode MeasurementMode { get; init; }
    public int SequenceNo { get; init; }
}
