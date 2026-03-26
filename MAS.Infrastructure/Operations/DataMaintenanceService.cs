using System.IO.Compression;

namespace MAS.Infrastructure.Operations;

public sealed record BackupResult(string DatabaseBackupPath, string? SettingsBackupPath);
public sealed record DiagnosticsResult(string PackagePath);
public sealed record RestoreResult(string RestoredDatabasePath, string? RestoredSettingsPath, string SafetyBackupPath);

public sealed class DataMaintenanceService
{
    public string DataFolderPath { get; }
    public string BackupFolderPath { get; }
    public string DiagnosticsFolderPath { get; }

    public DataMaintenanceService()
    {
        DataFolderPath = Path.Combine(AppContext.BaseDirectory, "Data");
        BackupFolderPath = Path.Combine(AppContext.BaseDirectory, "Exports", "Backups");
        DiagnosticsFolderPath = Path.Combine(AppContext.BaseDirectory, "Exports", "Diagnostics");
    }

    public Task<BackupResult> BackupAsync(string databasePath, string settingsPath, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        Directory.CreateDirectory(BackupFolderPath);
        var timestamp = DateTime.Now.ToString("yyyyMMdd-HHmmss");
        var databaseBackupPath = Path.Combine(BackupFolderPath, $"MASQC_backup_{timestamp}.db");
        var settingsBackupPath = Path.Combine(BackupFolderPath, $"appsettings_backup_{timestamp}.json");

        File.Copy(databasePath, databaseBackupPath, overwrite: true);
        string? finalSettingsPath = null;
        if (File.Exists(settingsPath))
        {
            File.Copy(settingsPath, settingsBackupPath, overwrite: true);
            finalSettingsPath = settingsBackupPath;
        }

        return Task.FromResult(new BackupResult(databaseBackupPath, finalSettingsPath));
    }

    public async Task<DiagnosticsResult> ExportDiagnosticsAsync(
        string databasePath,
        string settingsPath,
        string runtimeLog,
        IReadOnlyDictionary<string, string> summary,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        Directory.CreateDirectory(DiagnosticsFolderPath);
        var timestamp = DateTime.Now.ToString("yyyyMMdd-HHmmss");
        var tempFolder = Path.Combine(DiagnosticsFolderPath, $"diagnostics_{timestamp}");
        Directory.CreateDirectory(tempFolder);

        var summaryLines = summary.Select(kvp => $"{kvp.Key}: {kvp.Value}").ToList();
        await File.WriteAllLinesAsync(Path.Combine(tempFolder, "summary.txt"), summaryLines, cancellationToken);

        if (File.Exists(databasePath))
        {
            File.Copy(databasePath, Path.Combine(tempFolder, "MASQC.db"), overwrite: true);
        }

        if (File.Exists(settingsPath))
        {
            File.Copy(settingsPath, Path.Combine(tempFolder, "appsettings.json"), overwrite: true);
        }

        await File.WriteAllTextAsync(Path.Combine(tempFolder, "runtime-log.txt"), runtimeLog, cancellationToken);

        var zipPath = Path.Combine(DiagnosticsFolderPath, $"MAS_diagnostics_{timestamp}.zip");
        if (File.Exists(zipPath))
        {
            File.Delete(zipPath);
        }

        ZipFile.CreateFromDirectory(tempFolder, zipPath);
        Directory.Delete(tempFolder, recursive: true);
        return new DiagnosticsResult(zipPath);
    }

    public async Task<RestoreResult> RestoreLatestBackupAsync(string databasePath, string settingsPath, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        Directory.CreateDirectory(BackupFolderPath);

        var latestDatabaseBackup = new DirectoryInfo(BackupFolderPath)
            .GetFiles("MASQC_backup_*.db")
            .OrderByDescending(x => x.LastWriteTimeUtc)
            .FirstOrDefault();

        if (latestDatabaseBackup is null)
        {
            throw new FileNotFoundException("未找到可恢复的数据库备份文件。", BackupFolderPath);
        }

        var latestSettingsBackup = new DirectoryInfo(BackupFolderPath)
            .GetFiles("appsettings_backup_*.json")
            .OrderByDescending(x => x.LastWriteTimeUtc)
            .FirstOrDefault();

        var safety = await BackupAsync(databasePath, settingsPath, cancellationToken);
        File.Copy(latestDatabaseBackup.FullName, databasePath, overwrite: true);

        string? restoredSettingsPath = null;
        if (latestSettingsBackup is not null)
        {
            File.Copy(latestSettingsBackup.FullName, settingsPath, overwrite: true);
            restoredSettingsPath = settingsPath;
        }

        return new RestoreResult(databasePath, restoredSettingsPath, safety.DatabaseBackupPath);
    }
}
