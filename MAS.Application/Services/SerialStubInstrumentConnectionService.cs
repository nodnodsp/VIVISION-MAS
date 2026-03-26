using MAS.Application.Abstractions;
using MAS.Core.Entities;

namespace MAS.Application.Services;

public sealed class SerialStubInstrumentConnectionService : IInstrumentConnectionService
{
    private readonly IInstrumentRepository _instrumentRepository;
    private readonly string _portName;
    private readonly int _baudRate;
    private readonly int _readTimeoutMs;

    public SerialStubInstrumentConnectionService(IInstrumentRepository instrumentRepository, string portName, int baudRate, int readTimeoutMs)
    {
        _instrumentRepository = instrumentRepository;
        _portName = string.IsNullOrWhiteSpace(portName) ? "COM3" : portName.Trim();
        _baudRate = baudRate;
        _readTimeoutMs = readTimeoutMs;
    }

    public async Task<Instrument> ConnectAsync(string instrumentId, CancellationToken cancellationToken = default)
    {
        var instrument = await _instrumentRepository.GetByIdAsync(instrumentId, cancellationToken)
                         ?? throw new InvalidOperationException($"仪器不存在: {instrumentId}");
        instrument.PortName = _portName;
        instrument.UpdatedAt = DateTime.UtcNow;
        await _instrumentRepository.UpdateAsync(instrument, cancellationToken);
        throw new InvalidOperationException($"当前已切换到串口待接入模式，端口 {_portName}，波特率 {_baudRate}，读取超时 {_readTimeoutMs}ms。真实协议尚未接入，请先实现厂商串口适配器。");
    }

    public async Task<Instrument> DisconnectAsync(string instrumentId, CancellationToken cancellationToken = default)
    {
        var instrument = await _instrumentRepository.GetByIdAsync(instrumentId, cancellationToken)
                         ?? throw new InvalidOperationException($"仪器不存在: {instrumentId}");
        instrument.Status = "idle";
        instrument.PortName = _portName;
        instrument.UpdatedAt = DateTime.UtcNow;
        await _instrumentRepository.UpdateAsync(instrument, cancellationToken);
        return instrument;
    }

    public Task<CalibrationRecord> CalibrateAsync(string instrumentId, string calibrationType, CancellationToken cancellationToken = default)
    {
        throw new InvalidOperationException($"当前已切换到串口待接入模式，{calibrationType} 校准无法执行。请先实现真实串口协议和校准流程。");
    }
}
