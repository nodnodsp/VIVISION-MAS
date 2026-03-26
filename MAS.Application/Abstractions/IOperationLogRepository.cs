using MAS.Core.Entities;

namespace MAS.Application.Abstractions;

public interface IOperationLogRepository
{
    Task AddAsync(OperationLog log, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<OperationLog>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<OperationLog>> GetRecentAsync(int take, CancellationToken cancellationToken = default);
}
