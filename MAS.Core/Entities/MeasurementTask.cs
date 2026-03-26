using MAS.Core.Common;
using MAS.Core.Enums;
using MeasurementTaskStatus = MAS.Core.Enums.TaskStatus;

namespace MAS.Core.Entities;

public sealed class MeasurementTask : EntityBase
{
    public string TaskCode { get; set; } = string.Empty;
    public string InstrumentId { get; set; } = string.Empty;
    public string? SampleId { get; set; }
    public string? StandardSampleId { get; set; }
    public string? TemplateId { get; set; }
    public string TaskType { get; set; } = "trial";
    public MeasurementMode MeasurementMode { get; set; } = MeasurementMode.Single;
    public int AverageCount { get; set; } = 1;
    public int IntervalSeconds { get; set; } = 5;
    public MeasurementTaskStatus Status { get; set; } = MeasurementTaskStatus.Draft;
    public DateTime? StartedAt { get; set; }
    public DateTime? FinishedAt { get; set; }
}
