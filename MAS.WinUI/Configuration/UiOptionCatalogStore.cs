using System.IO;
using System.Text.Json;

namespace MAS.WinUI.Configuration;

public sealed class UiOptionCatalogStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true,
    };

    public string CatalogPath { get; }

    public UiOptionCatalogStore()
    {
        CatalogPath = Path.Combine(AppContext.BaseDirectory, "Data", "ui_option_catalog.json");
    }

    public async Task<UiOptionCatalog> LoadAsync(CancellationToken cancellationToken = default)
    {
        var directory = Path.GetDirectoryName(CatalogPath);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        if (!File.Exists(CatalogPath))
        {
            var defaults = new UiOptionCatalog();
            await SaveAsync(defaults, cancellationToken);
            return defaults;
        }

        await using var stream = File.OpenRead(CatalogPath);
        var catalog = await JsonSerializer.DeserializeAsync<UiOptionCatalog>(stream, JsonOptions, cancellationToken);
        return catalog ?? new UiOptionCatalog();
    }

    public async Task SaveAsync(UiOptionCatalog catalog, CancellationToken cancellationToken = default)
    {
        var directory = Path.GetDirectoryName(CatalogPath);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        await using var stream = File.Create(CatalogPath);
        await JsonSerializer.SerializeAsync(stream, catalog, JsonOptions, cancellationToken);
    }
}

