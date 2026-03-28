using System.Windows.Media;

namespace MAS.WinUI.Models;

public sealed class MainColorDisplayRow
{
    public string RecordId { get; init; } = string.Empty;
    public string TaskId { get; init; } = string.Empty;
    public string RecordType { get; init; } = string.Empty;
    public string? SampleId { get; init; }
    public string? StandardSampleId { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Angle { get; init; } = string.Empty;
    public string LightSource { get; init; } = "D65";
    public string Observer { get; init; } = "2°";
    public Brush SimulatedColorBrush { get; init; } = Brushes.Transparent;
    public string TimeText { get; init; } = string.Empty;
    public double? LStar { get; init; }
    public double? AStar { get; init; }
    public double? BStar { get; init; }
    public double? CStar { get; init; }
    public double? HStar { get; init; }
}
