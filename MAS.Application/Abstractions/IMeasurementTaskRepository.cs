using MAS.Core.Entities;

namespace MAS.Application.Abstractions;

public interface IMeasurementTaskRepository
{
    Task AddAsync(MeasurementTask task, CancellationToken cancellationToken = default);
    Task<MeasurementTask?> GetByCodeAsync(string taskCode, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<MeasurementTask>> GetAllAsync(CancellationToken cancellationToken = default);
}
