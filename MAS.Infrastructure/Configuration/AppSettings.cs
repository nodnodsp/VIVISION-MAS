namespace MAS.Infrastructure.Configuration;

public sealed class AppSettings
{
    public string DefaultTaskType { get; set; } = "trial";
    public string DefaultMeasurementMode { get; set; } = "Single";
    public int DefaultAverageCount { get; set; } = 1;
    public int DefaultIntervalSeconds { get; set; } = 5;
    public string DefaultTemplateCode { get; set; } = "TPL-DEFAULT";
    public int ReportPreviewMaxLength { get; set; } = 6000;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
