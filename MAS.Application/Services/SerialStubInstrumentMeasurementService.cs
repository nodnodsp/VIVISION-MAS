using MAS.Application.Abstractions;
using MAS.Application.Models;

namespace MAS.Application.Services;

public sealed class SerialStubInstrumentMeasurementService : IInstrumentMeasurementService
{
    private readonly string _portName;
    private readonly int _baudRate;
    private readonly int _readTimeoutMs;

    public SerialStubInstrumentMeasurementService(string portName, int baudRate, int readTimeoutMs)
    {
        _portName = string.IsNullOrWhiteSpace(portName) ? "COM3" : portName.Trim();
        _baudRate = baudRate;
        _readTimeoutMs = readTimeoutMs;
    }

    public Task<InstrumentMeasurementResult> MeasureAsync(InstrumentMeasurementRequest request, CancellationToken cancellationToken = default)
    {
        throw new InvalidOperationException($"当前测量运行模式为串口待接入模式，端口 {_portName}，波特率 {_baudRate}，读取超时 {_readTimeoutMs}ms。任务 {request.TaskCode} 的真实测量协议尚未接入。");
    }
}
