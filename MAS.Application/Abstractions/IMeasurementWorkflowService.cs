using MAS.Application.Models;

namespace MAS.Application.Abstractions;

public interface IMeasurementWorkflowService
{
    Task<MeasurementExecutionResult> ExecuteMeasurementAsync(string taskCode, string recordType, CancellationToken cancellationToken = default);
}
