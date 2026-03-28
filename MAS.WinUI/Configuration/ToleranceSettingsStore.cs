using System.IO;
using System.Text.Json;

namespace MAS.WinUI.Configuration;

public sealed class ToleranceSettingsStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true,
    };

    public string SettingsPath { get; }

    public ToleranceSettingsStore()
    {
        SettingsPath = Path.Combine(AppContext.BaseDirectory, "Data", "tolerance_settings.json");
    }

    public async Task<ToleranceSettingsDocument> LoadAsync(CancellationToken cancellationToken = default)
    {
        var directory = Path.GetDirectoryName(SettingsPath);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        if (!File.Exists(SettingsPath))
        {
            var defaults = new ToleranceSettingsDocument();
            await SaveAsync(defaults, cancellationToken);
            return defaults;
        }

        await using var stream = File.OpenRead(SettingsPath);
        var document = await JsonSerializer.DeserializeAsync<ToleranceSettingsDocument>(stream, JsonOptions, cancellationToken);
        return document ?? new ToleranceSettingsDocument();
    }

    public async Task SaveAsync(ToleranceSettingsDocument document, CancellationToken cancellationToken = default)
    {
        var directory = Path.GetDirectoryName(SettingsPath);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        await using var stream = File.Create(SettingsPath);
        await JsonSerializer.SerializeAsync(stream, document, JsonOptions, cancellationToken);
    }
}

