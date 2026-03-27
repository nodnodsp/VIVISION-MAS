using MAS.Core.Entities;

namespace MAS.Application.Abstractions;

public interface IRawPacketRepository
{
    Task AddAsync(RawPacket packet, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<RawPacket>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<RawPacket>> GetRecentAsync(int take, CancellationToken cancellationToken = default);
}
