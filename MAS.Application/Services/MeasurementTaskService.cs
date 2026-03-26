using MAS.Application.Abstractions;
using MAS.Core.Entities;
using MAS.Core.Enums;
using MeasurementTaskStatus = MAS.Core.Enums.TaskStatus;

namespace MAS.Application.Services;

public sealed class MeasurementTaskService : IMeasurementTaskService
{
    public MeasurementTask CreateDraftTask(
        string instrumentId,
        string? sampleId,
        string? standardSampleId,
        string? templateId)
    {
        var now = DateTime.UtcNow;
        return new MeasurementTask
        {
            TaskCode = $"TASK-{now:yyyyMMdd-HHmmss}",
            InstrumentId = instrumentId,
            SampleId = sampleId,
            StandardSampleId = standardSampleId,
            TemplateId = templateId,
            Status = MeasurementTaskStatus.Draft,
            MeasurementMode = MeasurementMode.Single,
            CreatedAt = now,
            UpdatedAt = now,
        };
    }
}
