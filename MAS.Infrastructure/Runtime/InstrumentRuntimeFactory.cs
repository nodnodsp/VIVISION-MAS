using MAS.Application.Abstractions;
using MAS.Application.Services;
using MAS.Infrastructure.Configuration;

namespace MAS.Infrastructure.Runtime;

public sealed class InstrumentRuntimeFactory
{
    public InstrumentRuntimeServices Create(
        AppSettings settings,
        IInstrumentRepository instrumentRepository,
        ICalibrationRecordRepository calibrationRecordRepository)
    {
        var runtimeMode = settings.InstrumentRuntimeMode?.Trim();
        if (string.Equals(runtimeMode, "SerialStub", StringComparison.OrdinalIgnoreCase))
        {
            return new InstrumentRuntimeServices
            {
                ConnectionService = new SerialStubInstrumentConnectionService(
                    instrumentRepository,
                    settings.InstrumentPortName,
                    settings.InstrumentBaudRate,
                    settings.InstrumentReadTimeoutMs),
                MeasurementService = new SerialStubInstrumentMeasurementService(
                    settings.InstrumentPortName,
                    settings.InstrumentBaudRate,
                    settings.InstrumentReadTimeoutMs),
                RuntimeDescription = $"串口待接入模式 / 端口 {settings.InstrumentPortName} / 波特率 {settings.InstrumentBaudRate} / 超时 {settings.InstrumentReadTimeoutMs}ms",
            };
        }

        return new InstrumentRuntimeServices
        {
            ConnectionService = new SimulatedInstrumentConnectionService(instrumentRepository, calibrationRecordRepository),
            MeasurementService = new SimulatedInstrumentMeasurementService(),
            RuntimeDescription = "模拟仪器模式 / 无需真实串口协议",
        };
    }
}
