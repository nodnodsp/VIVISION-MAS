using System.Windows;
using System.Windows.Controls;
using MAS.WinUI.Models;
using MAS.WinUI.Services;

namespace MAS.WinUI;

public partial class InstrumentConnectWindow : Window
{
    public InstrumentConnectionSelection? Selection { get; private set; }

    public InstrumentConnectWindow(string? currentPortName = null)
    {
        InitializeComponent();
        Loaded += async (_, _) => await LoadPortsAsync(currentPortName);
    }

    private async Task LoadPortsAsync(string? currentPortName)
    {
        var ports = await InstrumentConnectionDiscoveryService.GetSerialPortsAsync();
        PortComboBox.ItemsSource = ports;

        if (ports.Count == 0)
        {
            StatusTextBlock.Text = "当前未发现可用的 COM 口。";
            ConfirmButton.IsEnabled = false;
            return;
        }

        var selected = ports.FirstOrDefault(x => string.Equals(x.PortName, currentPortName, StringComparison.OrdinalIgnoreCase))
            ?? ports.FirstOrDefault();
        PortComboBox.SelectedItem = selected;
        ConfirmButton.IsEnabled = selected is not null;

        if (selected is not null)
        {
            await LoadBluetoothDevicesAsync(selected.PortName);
        }
    }

    private async void PortComboBox_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        ConfirmButton.IsEnabled = PortComboBox.SelectedItem is SerialPortOption;
        if (PortComboBox.SelectedItem is SerialPortOption option)
        {
            await LoadBluetoothDevicesAsync(option.PortName);
        }
    }

    private async void ScanButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (PortComboBox.SelectedItem is SerialPortOption option)
        {
            await LoadBluetoothDevicesAsync(option.PortName);
        }
    }

    private async Task LoadBluetoothDevicesAsync(string portName)
    {
        StatusTextBlock.Text = $"正在读取 {portName} 对应的蓝牙设备...";
        BluetoothDeviceListBox.ItemsSource = null;

        var devices = await InstrumentConnectionDiscoveryService.GetBluetoothDevicesForPortAsync(portName);
        BluetoothDeviceListBox.ItemsSource = devices;
        BluetoothDeviceListBox.SelectedIndex = devices.Count > 0 ? 0 : -1;
        StatusTextBlock.Text = devices.Count == 0
            ? $"{portName} 未发现关联的蓝牙设备。"
            : $"{portName} 已加载 {devices.Count} 条蓝牙设备信息。";
    }

    private void ConfirmButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (PortComboBox.SelectedItem is not SerialPortOption option)
        {
            MessageBox.Show(this, "请先选择一个 COM 口。", "连接", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        Selection = new InstrumentConnectionSelection
        {
            SelectedPortName = option.PortName,
            SelectedDeviceName = (BluetoothDeviceListBox.SelectedItem as BluetoothDeviceOption)?.FriendlyName,
        };

        DialogResult = true;
        Close();
    }

    private void CloseButton_OnClick(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
