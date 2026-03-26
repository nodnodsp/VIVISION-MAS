using MAS.Core.Entities;

namespace MAS.Application.Abstractions;

public interface ICalibrationRecordRepository
{
    Task AddAsync(CalibrationRecord record, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<CalibrationRecord>> GetByInstrumentIdAsync(string instrumentId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<CalibrationRecord>> GetAllAsync(CancellationToken cancellationToken = default);
}
