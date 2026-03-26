using MAS.Application.Models;

namespace MAS.Application.Abstractions;

public interface IInstrumentMeasurementService
{
    Task<InstrumentMeasurementResult> MeasureAsync(InstrumentMeasurementRequest request, CancellationToken cancellationToken = default);
}
