using MAS.Core.Common;

namespace MAS.Core.Entities;

public sealed class StandardSample : EntityBase
{
    public string LibraryId { get; set; } = string.Empty;
    public string StandardCode { get; set; } = string.Empty;
    public string StandardName { get; set; } = string.Empty;
    public int VersionNo { get; set; } = 1;
    public string? MaterialName { get; set; }
    public string? ColorName { get; set; }
    public string? ToleranceTemplateId { get; set; }
    public bool IsActive { get; set; } = true;
    public bool IsDefaultVersion { get; set; } = true;
}
