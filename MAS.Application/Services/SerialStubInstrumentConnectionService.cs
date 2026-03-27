using MAS.Application.Abstractions;
using MAS.Core.Entities;

namespace MAS.Application.Services;

public sealed class SerialStubInstrumentConnectionService : IInstrumentConnectionService
{
    private readonly IInstrumentRepository _instrumentRepository;
    private readonly IRawPacketRepository _rawPacketRepository;
    private readonly string _portName;
    private readonly int _baudRate;
    private readonly int _readTimeoutMs;

    public SerialStubInstrumentConnectionService(IInstrumentRepository instrumentRepository, IRawPacketRepository rawPacketRepository, string portName, int baudRate, int readTimeoutMs)
    {
        _instrumentRepository = instrumentRepository;
        _rawPacketRepository = rawPacketRepository;
        _portName = string.IsNullOrWhiteSpace(portName) ? "COM3" : portName.Trim();
        _baudRate = baudRate;
        _readTimeoutMs = readTimeoutMs;
    }

    public async Task<Instrument> ConnectAsync(string instrumentId, CancellationToken cancellationToken = default)
    {
        await RawPacketLogHelper.LogAsync(_rawPacketRepository, "outbound", "serial-connect-request", $"OPEN|{_portName}|{_baudRate}|{instrumentId}", instrumentId, cancellationToken: cancellationToken);

        var instrument = await _instrumentRepository.GetByIdAsync(instrumentId, cancellationToken)
                         ?? throw new InvalidOperationException($"仪器不存在: {instrumentId}");
        instrument.PortName = _portName;
        instrument.UpdatedAt = DateTime.UtcNow;
        await _instrumentRepository.UpdateAsync(instrument, cancellationToken);

        var message = $"当前已切换到串口待接入模式，端口 {_portName}，波特率 {_baudRate}，读取超时 {_readTimeoutMs}ms。真实协议尚未接入，请先实现厂商串口适配器。";
        await RawPacketLogHelper.LogAsync(_rawPacketRepository, "inbound", "serial-connect-response", $"ERROR|{message}", instrument.Id, cancellationToken: cancellationToken);
        throw new InvalidOperationException(message);
    }

    public async Task<Instrument> DisconnectAsync(string instrumentId, CancellationToken cancellationToken = default)
    {
        await RawPacketLogHelper.LogAsync(_rawPacketRepository, "outbound", "serial-disconnect-request", $"CLOSE|{_portName}|{instrumentId}", instrumentId, cancellationToken: cancellationToken);

        var instrument = await _instrumentRepository.GetByIdAsync(instrumentId, cancellationToken)
                         ?? throw new InvalidOperationException($"仪器不存在: {instrumentId}");
        instrument.Status = "idle";
        instrument.PortName = _portName;
        instrument.UpdatedAt = DateTime.UtcNow;
        await _instrumentRepository.UpdateAsync(instrument, cancellationToken);

        await RawPacketLogHelper.LogAsync(_rawPacketRepository, "inbound", "serial-disconnect-response", $"CLOSED|{_portName}|{instrument.InstrumentCode}", instrument.Id, cancellationToken: cancellationToken);
        return instrument;
    }

    public async Task<CalibrationRecord> CalibrateAsync(string instrumentId, string calibrationType, CancellationToken cancellationToken = default)
    {
        var message = $"当前已切换到串口待接入模式，{calibrationType} 校准无法执行。请先实现真实串口协议和校准流程。";
        await RawPacketLogHelper.LogAsync(_rawPacketRepository, "outbound", "serial-calibration-request", $"CALIBRATE|{calibrationType.ToUpperInvariant()}|{instrumentId}", instrumentId, cancellationToken: cancellationToken);
        await RawPacketLogHelper.LogAsync(_rawPacketRepository, "inbound", "serial-calibration-response", $"ERROR|{message}", instrumentId, cancellationToken: cancellationToken);
        throw new InvalidOperationException(message);
    }
}
