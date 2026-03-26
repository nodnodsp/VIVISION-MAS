namespace MAS.Core.Common;

public abstract class EntityBase
{
    public string Id { get; init; } = Guid.NewGuid().ToString("N");
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
