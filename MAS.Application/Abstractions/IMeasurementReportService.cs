using MAS.Application.Models;

namespace MAS.Application.Abstractions;

public interface IMeasurementReportService
{
    Task<MeasurementReportExportResult> ExportRecordReportAsync(string recordId, CancellationToken cancellationToken = default);
}
