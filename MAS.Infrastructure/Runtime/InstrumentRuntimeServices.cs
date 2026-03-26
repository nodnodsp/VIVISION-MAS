using MAS.Application.Abstractions;

namespace MAS.Infrastructure.Runtime;

public sealed class InstrumentRuntimeServices
{
    public required IInstrumentConnectionService ConnectionService { get; init; }
    public required IInstrumentMeasurementService MeasurementService { get; init; }
    public required string RuntimeDescription { get; init; }
}
