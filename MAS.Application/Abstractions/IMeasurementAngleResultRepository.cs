using MAS.Core.Entities;

namespace MAS.Application.Abstractions;

public interface IMeasurementAngleResultRepository
{
    Task AddRangeAsync(IEnumerable<MeasurementAngleResult> results, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<MeasurementAngleResult>> GetByRecordIdAsync(string recordId, CancellationToken cancellationToken = default);
}
