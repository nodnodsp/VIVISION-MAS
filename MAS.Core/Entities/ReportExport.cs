using MAS.Core.Common;

namespace MAS.Core.Entities;

public sealed class ReportExport : EntityBase
{
    public string RecordId { get; set; } = string.Empty;
    public string ReportCode { get; set; } = string.Empty;
    public string FileFormat { get; set; } = "pdf";
    public string? FilePath { get; set; }
    public string ExportStatus { get; set; } = "pending";
    public DateTime ExportedAt { get; set; } = DateTime.UtcNow;
}
