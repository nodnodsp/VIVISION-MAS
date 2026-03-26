using MAS.Core.Entities;

namespace MAS.Application.Abstractions;

public interface IMeasurementTaskService
{
    MeasurementTask CreateDraftTask(
        string instrumentId,
        string? sampleId,
        string? standardSampleId,
        string? templateId);
}
