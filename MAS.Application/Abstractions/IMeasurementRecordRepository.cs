using MAS.Core.Entities;

namespace MAS.Application.Abstractions;

public interface IMeasurementRecordRepository
{
    Task AddAsync(MeasurementRecord record, CancellationToken cancellationToken = default);
    Task<MeasurementRecord?> GetByIdAsync(string recordId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<MeasurementRecord>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<MeasurementRecord>> GetByTaskIdAsync(string taskId, CancellationToken cancellationToken = default);
}
