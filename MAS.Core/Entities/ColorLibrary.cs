using MAS.Core.Common;

namespace MAS.Core.Entities;

public sealed class ColorLibrary : EntityBase
{
    public string LibraryCode { get; set; } = string.Empty;
    public string LibraryName { get; set; } = string.Empty;
    public string? CategoryName { get; set; }
    public bool IsDefault { get; set; }
}
