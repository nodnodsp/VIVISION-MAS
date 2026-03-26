using MAS.Core.Entities;

namespace MAS.Application.Abstractions;

public interface IMeasurementEffectResultRepository
{
    Task AddRangeAsync(IEnumerable<MeasurementEffectResult> results, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<MeasurementEffectResult>> GetByRecordIdAsync(string recordId, CancellationToken cancellationToken = default);
}
