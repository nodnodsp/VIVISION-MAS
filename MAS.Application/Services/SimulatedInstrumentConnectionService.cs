using MAS.Application.Abstractions;
using MAS.Core.Entities;

namespace MAS.Application.Services;

public sealed class SimulatedInstrumentConnectionService : IInstrumentConnectionService
{
    private readonly IInstrumentRepository _instrumentRepository;
    private readonly ICalibrationRecordRepository _calibrationRecordRepository;
    private readonly IRawPacketRepository _rawPacketRepository;

    public SimulatedInstrumentConnectionService(
        IInstrumentRepository instrumentRepository,
        ICalibrationRecordRepository calibrationRecordRepository,
        IRawPacketRepository rawPacketRepository)
    {
        _instrumentRepository = instrumentRepository;
        _calibrationRecordRepository = calibrationRecordRepository;
        _rawPacketRepository = rawPacketRepository;
    }

    public async Task<Instrument> ConnectAsync(string instrumentId, CancellationToken cancellationToken = default)
    {
        await RawPacketLogHelper.LogAsync(_rawPacketRepository, "outbound", "connect-request", $"CONNECT|{instrumentId}", instrumentId, cancellationToken: cancellationToken);

        var instrument = await _instrumentRepository.GetByIdAsync(instrumentId, cancellationToken)
                         ?? throw new InvalidOperationException($"仪器不存在: {instrumentId}");
        instrument.Status = "connected";
        instrument.PortName ??= "COM3";
        instrument.UpdatedAt = DateTime.UtcNow;
        await _instrumentRepository.UpdateAsync(instrument, cancellationToken);

        await RawPacketLogHelper.LogAsync(_rawPacketRepository, "inbound", "connect-response", $"CONNECTED|{instrument.InstrumentCode}|{instrument.PortName}", instrument.Id, cancellationToken: cancellationToken);
        return instrument;
    }

    public async Task<Instrument> DisconnectAsync(string instrumentId, CancellationToken cancellationToken = default)
    {
        await RawPacketLogHelper.LogAsync(_rawPacketRepository, "outbound", "disconnect-request", $"DISCONNECT|{instrumentId}", instrumentId, cancellationToken: cancellationToken);

        var instrument = await _instrumentRepository.GetByIdAsync(instrumentId, cancellationToken)
                         ?? throw new InvalidOperationException($"仪器不存在: {instrumentId}");
        instrument.Status = "idle";
        instrument.UpdatedAt = DateTime.UtcNow;
        await _instrumentRepository.UpdateAsync(instrument, cancellationToken);

        await RawPacketLogHelper.LogAsync(_rawPacketRepository, "inbound", "disconnect-response", $"IDLE|{instrument.InstrumentCode}", instrument.Id, cancellationToken: cancellationToken);
        return instrument;
    }

    public async Task<CalibrationRecord> CalibrateAsync(string instrumentId, string calibrationType, CancellationToken cancellationToken = default)
    {
        await RawPacketLogHelper.LogAsync(_rawPacketRepository, "outbound", "calibration-request", $"CALIBRATE|{calibrationType.ToUpperInvariant()}|{instrumentId}", instrumentId, cancellationToken: cancellationToken);

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
        await RawPacketLogHelper.LogAsync(_rawPacketRepository, "inbound", "calibration-response", $"CALIBRATED|{calibrationType.ToUpperInvariant()}|success", instrumentId, cancellationToken: cancellationToken);
        return record;
    }
}
