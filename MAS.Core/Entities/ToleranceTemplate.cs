using MAS.Core.Common;

namespace MAS.Core.Entities;

public sealed class ToleranceTemplate : EntityBase
{
    public string TemplateCode { get; set; } = string.Empty;
    public string TemplateName { get; set; } = string.Empty;
    public string DeltaEFormula { get; set; } = "DE00";
    public double? OverallUpperLimit { get; set; }
    public double? EffectUpperLimit { get; set; }
    public bool IsDefault { get; set; }
    public string Status { get; set; } = "active";
}
