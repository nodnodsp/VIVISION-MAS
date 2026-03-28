namespace MAS.WinUI.Models;

public sealed class SerialPortOption
{
    public string PortName { get; init; } = string.Empty;
    public string DisplayName { get; init; } = string.Empty;

    public override string ToString() => string.IsNullOrWhiteSpace(DisplayName) ? PortName : $"{PortName}  {DisplayName}";
}

public sealed class BluetoothDeviceOption
{
    public string FriendlyName { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public string DeviceClass { get; init; } = string.Empty;
    public string InstanceId { get; init; } = string.Empty;

    public override string ToString() => string.IsNullOrWhiteSpace(Status)
        ? FriendlyName
        : $"{FriendlyName}  [{Status}]";
}

public sealed class InstrumentConnectionSelection
{
    public string SelectedPortName { get; init; } = string.Empty;
    public string? SelectedDeviceName { get; init; }
}
