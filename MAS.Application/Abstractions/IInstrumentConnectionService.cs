using MAS.Core.Entities;

namespace MAS.Application.Abstractions;

public interface IInstrumentConnectionService
{
    Task<Instrument> ConnectAsync(string instrumentId, CancellationToken cancellationToken = default);
    Task<Instrument> DisconnectAsync(string instrumentId, CancellationToken cancellationToken = default);
    Task<CalibrationRecord> CalibrateAsync(string instrumentId, string calibrationType, CancellationToken cancellationToken = default);
}
