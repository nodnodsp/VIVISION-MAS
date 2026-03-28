namespace MAS.WinUI.Configuration;

public sealed class ToleranceRangeItem
{
    public string AngleCode { get; set; } = string.Empty;
    public double MinValue { get; set; } = 0.7;
    public double MaxValue { get; set; } = 1.0;
}

public sealed class ToleranceSettingsDocument
{
    public string ToleranceType { get; set; } = "ΔE*";
    public List<ToleranceRangeItem> ColorToleranceItems { get; set; } = CreateAngleDefaults();
    public List<ToleranceRangeItem> SparkleToleranceItems { get; set; } = CreateAngleDefaults();
    public List<ToleranceRangeItem> OverallToleranceItems { get; set; } =
    [
        new() { AngleCode = "综合色差", MinValue = 0.7, MaxValue = 1.0 }
    ];

    public static List<ToleranceRangeItem> CreateAngleDefaults()
    {
        return
        [
            new() { AngleCode = "45as-15", MinValue = 0.7, MaxValue = 1.0 },
            new() { AngleCode = "45as15", MinValue = 0.7, MaxValue = 1.0 },
            new() { AngleCode = "45as25", MinValue = 0.7, MaxValue = 1.0 },
            new() { AngleCode = "45as45", MinValue = 0.7, MaxValue = 1.0 },
            new() { AngleCode = "45as75", MinValue = 0.7, MaxValue = 1.0 },
            new() { AngleCode = "45as110", MinValue = 0.7, MaxValue = 1.0 },
            new() { AngleCode = "15as-45", MinValue = 0.7, MaxValue = 1.0 },
            new() { AngleCode = "15as-30", MinValue = 0.7, MaxValue = 1.0 },
            new() { AngleCode = "15as-15", MinValue = 0.7, MaxValue = 1.0 },
            new() { AngleCode = "15as15", MinValue = 0.7, MaxValue = 1.0 },
            new() { AngleCode = "15as45", MinValue = 0.7, MaxValue = 1.0 },
            new() { AngleCode = "15as80", MinValue = 0.7, MaxValue = 1.0 }
        ];
    }
}
