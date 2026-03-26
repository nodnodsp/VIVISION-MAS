using System.Text.Json;

namespace MAS.Infrastructure.Configuration;

public sealed class AppSettingsStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true,
    };

    public string SettingsPath { get; }

    public AppSettingsStore()
    {
        SettingsPath = Path.Combine(AppContext.BaseDirectory, "Data", "appsettings.json");
    }

    public async Task<AppSettings> LoadAsync(CancellationToken cancellationToken = default)
    {
        var directory = Path.GetDirectoryName(SettingsPath);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        if (!File.Exists(SettingsPath))
        {
            var defaults = new AppSettings();
            await SaveAsync(defaults, cancellationToken);
            return defaults;
        }

        await using var stream = File.OpenRead(SettingsPath);
        var settings = await JsonSerializer.DeserializeAsync<AppSettings>(stream, JsonOptions, cancellationToken);
        return settings ?? new AppSettings();
    }

    public async Task SaveAsync(AppSettings settings, CancellationToken cancellationToken = default)
    {
        var directory = Path.GetDirectoryName(SettingsPath);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        settings.UpdatedAt = DateTime.UtcNow;
        await using var stream = File.Create(SettingsPath);
        await JsonSerializer.SerializeAsync(stream, settings, JsonOptions, cancellationToken);
    }
}
