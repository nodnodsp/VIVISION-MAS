using MAS.Core.Entities;

namespace MAS.Application.Abstractions;

public interface IStandardSampleRepository
{
    Task AddAsync(StandardSample standardSample, CancellationToken cancellationToken = default);
    Task<StandardSample?> GetByIdAsync(string standardSampleId, CancellationToken cancellationToken = default);
    Task<StandardSample?> GetByCodeAsync(string standardCode, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<StandardSample>> GetAllAsync(CancellationToken cancellationToken = default);
}
