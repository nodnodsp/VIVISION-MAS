using System.Diagnostics;
using System.IO.Ports;
using System.Text;
using System.Text.Json;
using MAS.WinUI.Models;

namespace MAS.WinUI.Services;

public static class InstrumentConnectionDiscoveryService
{
    public static async Task<IReadOnlyList<SerialPortOption>> GetSerialPortsAsync(CancellationToken cancellationToken = default)
    {
        const string script = @"
[System.IO.Ports.SerialPort]::GetPortNames() |
    Sort-Object { if ($_ -match '^COM(\\d+)$') { [int]$Matches[1] } else { [int]::MaxValue } } |
    ForEach-Object { [pscustomobject]@{ PortName = $_; DisplayName = $_ } } |
    ConvertTo-Json -Compress
";

        var json = await ExecutePowerShellAsync(script, cancellationToken);
        return ParseJsonList(json, element => new SerialPortOption
        {
            PortName = element.TryGetProperty("PortName", out var port) ? port.GetString() ?? string.Empty : string.Empty,
            DisplayName = element.TryGetProperty("DisplayName", out var name) ? name.GetString() ?? string.Empty : string.Empty,
        })
        .Where(x => !string.IsNullOrWhiteSpace(x.PortName))
        .ToList();
    }

    public static async Task<IReadOnlyList<BluetoothDeviceOption>> GetBluetoothDevicesForPortAsync(string portName, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(portName))
        {
            return Array.Empty<BluetoothDeviceOption>();
        }

        var escapedPortName = EscapeForSingleQuotedPowerShell(portName);
        var script = $@"
$port = '{escapedPortName}'
try {{
    $result = @()
    $portMatch = Get-CimInstance Win32_PnPEntity -ErrorAction Stop | Where-Object {{
        ($_.Name -like ('*' + $port + '*')) -or ($_.Caption -like ('*' + $port + '*'))
    }}
    foreach ($item in $portMatch) {{
        $result += [pscustomobject]@{{
            FriendlyName = $item.Name
            Status = $item.Status
            DeviceClass = if ([string]::IsNullOrWhiteSpace($item.PNPClass)) {{ 'Ports' }} else {{ $item.PNPClass }}
            InstanceId = $item.PNPDeviceID
        }}
    }}
    $bluetooth = Get-CimInstance Win32_PnPEntity -ErrorAction Stop | Where-Object {{
        $_.PNPClass -eq 'Bluetooth' -or $_.PNPDeviceID -like 'BTH*' -or $_.Name -like '*Bluetooth*'
    }}
    foreach ($item in $bluetooth) {{
        if (-not ($result | Where-Object {{ $_.InstanceId -eq $item.PNPDeviceID }})) {{
            $result += [pscustomobject]@{{
                FriendlyName = $item.Name
                Status = $item.Status
                DeviceClass = if ([string]::IsNullOrWhiteSpace($item.PNPClass)) {{ 'Bluetooth' }} else {{ $item.PNPClass }}
                InstanceId = $item.PNPDeviceID
            }}
        }}
    }}
    if ($result.Count -eq 0) {{
        $result = @([pscustomobject]@{{ FriendlyName = '未发现与该 COM 口关联的蓝牙设备'; Status = 'unknown'; DeviceClass = 'Bluetooth'; InstanceId = '' }})
    }}
    $result | ConvertTo-Json -Compress
}}
catch {{
    @([pscustomobject]@{{ FriendlyName = '当前环境无法读取蓝牙设备信息'; Status = 'blocked'; DeviceClass = 'Bluetooth'; InstanceId = '' }}) | ConvertTo-Json -Compress
}}
";

        var json = await ExecutePowerShellAsync(script, cancellationToken);
        var devices = ParseJsonList(json, element => new BluetoothDeviceOption
        {
            FriendlyName = element.TryGetProperty("FriendlyName", out var name) ? name.GetString() ?? string.Empty : string.Empty,
            Status = element.TryGetProperty("Status", out var status) ? status.GetString() ?? string.Empty : string.Empty,
            DeviceClass = element.TryGetProperty("DeviceClass", out var deviceClass) ? deviceClass.GetString() ?? string.Empty : string.Empty,
            InstanceId = element.TryGetProperty("InstanceId", out var instanceId) ? instanceId.GetString() ?? string.Empty : string.Empty,
        })
        .Where(x => !string.IsNullOrWhiteSpace(x.FriendlyName))
        .ToList();

        if (devices.Count == 0)
        {
            devices.Add(new BluetoothDeviceOption
            {
                FriendlyName = $"{portName} 未返回蓝牙设备信息",
                Status = "unknown",
                DeviceClass = "Bluetooth",
                InstanceId = string.Empty,
            });
        }

        return devices;
    }

    private static async Task<string> ExecutePowerShellAsync(string script, CancellationToken cancellationToken)
    {
        using var process = new Process();
        process.StartInfo = new ProcessStartInfo
        {
            FileName = "powershell.exe",
            Arguments = $"-NoProfile -ExecutionPolicy Bypass -EncodedCommand {EncodeScript(script)}",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8,
        };

        process.Start();
        var outputTask = process.StandardOutput.ReadToEndAsync(cancellationToken);
        var errorTask = process.StandardError.ReadToEndAsync(cancellationToken);
        await process.WaitForExitAsync(cancellationToken);

        var output = (await outputTask).Trim();
        var error = (await errorTask).Trim();
        if (process.ExitCode != 0 && string.IsNullOrWhiteSpace(output))
        {
            return string.IsNullOrWhiteSpace(error) ? string.Empty : error;
        }

        return output;
    }

    private static List<T> ParseJsonList<T>(string? json, Func<JsonElement, T> selector)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return new List<T>();
        }

        try
        {
            using var document = JsonDocument.Parse(json);
            return document.RootElement.ValueKind switch
            {
                JsonValueKind.Array => document.RootElement.EnumerateArray().Select(selector).ToList(),
                JsonValueKind.Object => new List<T> { selector(document.RootElement) },
                _ => new List<T>(),
            };
        }
        catch
        {
            return new List<T>();
        }
    }

    private static int ParsePortNumber(string portName)
    {
        if (portName.StartsWith("COM", StringComparison.OrdinalIgnoreCase)
            && int.TryParse(portName.AsSpan(3), out var number))
        {
            return number;
        }

        return int.MaxValue;
    }

    private static string EncodeScript(string script)
    {
        return Convert.ToBase64String(Encoding.Unicode.GetBytes(script));
    }

    private static string EscapeForSingleQuotedPowerShell(string value)
    {
        return value.Replace("'", "''");
    }
}

