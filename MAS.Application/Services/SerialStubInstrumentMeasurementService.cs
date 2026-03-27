using MAS.Application.Abstractions;
using MAS.Application.Models;

namespace MAS.Application.Services;

public sealed class SerialStubInstrumentMeasurementService : IInstrumentMeasurementService
{
    private readonly IRawPacketRepository _rawPacketRepository;
    private readonly string _portName;
    private readonly int _baudRate;
    private readonly int _readTimeoutMs;

    public SerialStubInstrumentMeasurementService(IRawPacketRepository rawPacketRepository, string portName, int baudRate, int readTimeoutMs)
    {
        _rawPacketRepository = rawPacketRepository;
        _portName = string.IsNullOrWhiteSpace(portName) ? "COM3" : portName.Trim();
        _baudRate = baudRate;
        _readTimeoutMs = readTimeoutMs;
    }

    public async Task<InstrumentMeasurementResult> MeasureAsync(InstrumentMeasurementRequest request, CancellationToken cancellationToken = default)
    {
        var message = $"当前测量运行模式为串口待接入模式，端口 {_portName}，波特率 {_baudRate}，读取超时 {_readTimeoutMs}ms。任务 {request.TaskCode} 的真实测量协议尚未接入。";
        await RawPacketLogHelper.LogAsync(_rawPacketRepository, "outbound", "serial-measure-request", $"MEASURE|{request.RecordType.ToUpperInvariant()}|{request.TaskCode}|{_portName}|{_baudRate}", request.InstrumentId, request.TaskId, cancellationToken);
        await RawPacketLogHelper.LogAsync(_rawPacketRepository, "inbound", "serial-measure-response", $"ERROR|{message}", request.InstrumentId, request.TaskId, cancellationToken);
        throw new InvalidOperationException(message);
    }
}
