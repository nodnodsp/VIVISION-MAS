using MAS.Core.Entities;

namespace MAS.Application.Abstractions;

public interface IToleranceTemplateRepository
{
    Task AddAsync(ToleranceTemplate template, CancellationToken cancellationToken = default);
    Task<ToleranceTemplate?> GetByIdAsync(string templateId, CancellationToken cancellationToken = default);
    Task<ToleranceTemplate?> GetByCodeAsync(string templateCode, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ToleranceTemplate>> GetAllAsync(CancellationToken cancellationToken = default);
}
