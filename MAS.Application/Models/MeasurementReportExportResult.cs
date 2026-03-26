namespace MAS.Application.Models;

public sealed class MeasurementReportExportResult
{
    public string FilePath { get; init; } = string.Empty;
    public string FileFormat { get; init; } = string.Empty;
    public string ReportCode { get; init; } = string.Empty;
}
