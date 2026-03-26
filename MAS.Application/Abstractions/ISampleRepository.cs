using MAS.Core.Entities;

namespace MAS.Application.Abstractions;

public interface ISampleRepository
{
    Task AddAsync(Sample sample, CancellationToken cancellationToken = default);
    Task<Sample?> GetByIdAsync(string sampleId, CancellationToken cancellationToken = default);
    Task<Sample?> GetByCodeAsync(string sampleCode, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Sample>> GetAllAsync(CancellationToken cancellationToken = default);
}
