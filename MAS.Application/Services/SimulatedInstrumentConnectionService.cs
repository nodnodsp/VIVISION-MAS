using MAS.Application.Abstractions;
using MAS.Core.Entities;

namespace MAS.Application.Services;

public sealed class SimulatedInstrumentConnectionService : IInstrumentConnectionService
{
    private readonly IInstrumentRepository _instrumentRepository;
    private readonly ICalibrationRecordRepository _calibrationRecordRepository;

    public SimulatedInstrumentConnectionService(
        IInstrumentRepository instrumentRepository,
        ICalibrationRecordRepository calibrationRecordRepository)
    {
        _instrumentRepository = instrumentRepository;
        _calibrationRecordRepository = calibrationRecordRepository;
    }

    public async Task<Instrument> ConnectAsync(string instrumentId, CancellationToken cancellationToken = default)
    {
        var instrument = await _instrumentRepository.GetByIdAsync(instrumentId, cancellationToken)
                         ?? throw new InvalidOperationException($"仪器不存在: {instrumentId}");
        instrument.Status = "connected";
        instrument.PortName ??= "COM3";
        instrument.UpdatedAt = DateTime.UtcNow;
        await _instrumentRepository.UpdateAsync(instrument, cancellationToken);
        return instrument;
    }

    public async Task<Instrument> DisconnectAsync(string instrumentId, CancellationToken cancellationToken = default)
    {
        var instrument = await _instrumentRepository.GetByIdAsync(instrumentId, cancellationToken)
                         ?? throw new InvalidOperationException($"仪器不存在: {instrumentId}");
        instrument.Status = "idle";
        instrument.UpdatedAt = DateTime.UtcNow;
        await _instrumentRepository.UpdateAsync(instrument, cancellationToken);
        return instrument;
    }

    public async Task<CalibrationRecord> CalibrateAsync(string instrumentId, string calibrationType, CancellationToken cancellationToken = default)
    {
        var instrument = await ConnectAsync(instrumentId, cancellationToken);
        instrument.Status = "calibrated";
        instrument.UpdatedAt = DateTime.UtcNow;
        await _instrumentRepository.UpdateAsync(instrument, cancellationToken);

        var now = DateTime.UtcNow;
        var record = new CalibrationRecord
        {
            InstrumentId = instrumentId,
            CalibrationType = calibrationType,
            ResultCode = "success",
            StartedAt = now,
            FinishedAt = now.AddSeconds(5),
            ExpiresAt = now.AddDays(7),
            Remark = calibrationType == "white" ? "白板校准完成" : "黑腔校准完成",
            CreatedAt = now,
            UpdatedAt = now,
        };

        await _calibrationRecordRepository.AddAsync(record, cancellationToken);
        return record;
    }
}
