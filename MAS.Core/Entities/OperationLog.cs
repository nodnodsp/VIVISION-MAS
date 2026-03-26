using MAS.Core.Common;

namespace MAS.Core.Entities;

public sealed class OperationLog : EntityBase
{
    public string? TaskId { get; set; }
    public string? RecordId { get; set; }
    public string? OperatorId { get; set; }
    public string ModuleName { get; set; } = string.Empty;
    public string OperationType { get; set; } = string.Empty;
    public string? OperationDesc { get; set; }
    public string OperationResult { get; set; } = string.Empty;
}
