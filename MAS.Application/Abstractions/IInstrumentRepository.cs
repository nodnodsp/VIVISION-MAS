using MAS.Core.Entities;

namespace MAS.Application.Abstractions;

public interface IInstrumentRepository
{
    Task<IReadOnlyList<Instrument>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<Instrument?> GetByIdAsync(string instrumentId, CancellationToken cancellationToken = default);
    Task<Instrument?> GetDefaultAsync(CancellationToken cancellationToken = default);
    Task UpdateAsync(Instrument instrument, CancellationToken cancellationToken = default);
}
