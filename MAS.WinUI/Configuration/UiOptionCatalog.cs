namespace MAS.WinUI.Configuration;

public sealed class UiOptionCatalog
{
    public List<string> LightSources { get; set; } =
    [
        "A",
        "C",
        "D50",
        "D65",
        "F2",
        "F7",
        "F11"
    ];

    public List<string> Observers { get; set; } =
    [
        "2°",
        "10°"
    ];

    public List<string> AvailableDisplayItems { get; set; } =
    [
        "名称",
        "角度",
        "光源",
        "观察者",
        "仿真色",
        "时间",
        "L*",
        "a*",
        "b*",
        "C*",
        "h*"
    ];

    public List<string> DefaultDisplayItems { get; set; } =
    [
        "名称",
        "角度",
        "光源",
        "观察者",
        "仿真色",
        "时间",
        "L*",
        "a*",
        "b*",
        "C*",
        "h*"
    ];
}
