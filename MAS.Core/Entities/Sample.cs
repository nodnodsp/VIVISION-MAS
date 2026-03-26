using MAS.Core.Common;

namespace MAS.Core.Entities;

public sealed class Sample : EntityBase
{
    public string SampleCode { get; set; } = string.Empty;
    public string SampleName { get; set; } = string.Empty;
    public string? BatchNo { get; set; }
    public string? MaterialName { get; set; }
    public string? ColorName { get; set; }
    public string Status { get; set; } = "active";
}
