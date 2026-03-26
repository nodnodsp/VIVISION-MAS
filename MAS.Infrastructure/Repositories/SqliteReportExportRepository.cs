using MAS.Application.Abstractions;
using MAS.Core.Entities;
using MAS.Infrastructure.Database;
using Microsoft.Data.Sqlite;

namespace MAS.Infrastructure.Repositories;

public sealed class SqliteReportExportRepository : IReportExportRepository
{
    public async Task AddAsync(ReportExport export, CancellationToken cancellationToken = default)
    {
        await using var connection = SqliteConnectionFactory.Create();
        await connection.OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = @"
INSERT INTO report_exports (id, record_id, report_code, template_name, file_format, file_path, exported_by, exported_at, export_status, remark)
VALUES ($id, $record_id, $report_code, $template_name, $file_format, $file_path, $exported_by, $exported_at, $export_status, $remark);";
        command.Parameters.AddWithValue("$id", export.Id);
        command.Parameters.AddWithValue("$record_id", export.RecordId);
        command.Parameters.AddWithValue("$report_code", export.ReportCode);
        command.Parameters.AddWithValue("$template_name", DBNull.Value);
        command.Parameters.AddWithValue("$file_format", export.FileFormat);
        command.Parameters.AddWithValue("$file_path", (object?)export.FilePath ?? DBNull.Value);
        command.Parameters.AddWithValue("$exported_by", DBNull.Value);
        command.Parameters.AddWithValue("$exported_at", export.ExportedAt.ToString("O"));
        command.Parameters.AddWithValue("$export_status", export.ExportStatus);
        command.Parameters.AddWithValue("$remark", DBNull.Value);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ReportExport>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var items = new List<ReportExport>();
        await using var connection = SqliteConnectionFactory.Create();
        await connection.OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = @"SELECT id, record_id, report_code, file_format, file_path, exported_at, export_status
FROM report_exports ORDER BY exported_at DESC;";
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            items.Add(Map(reader));
        }

        return items;
    }

    public async Task<IReadOnlyList<ReportExport>> GetByRecordIdAsync(string recordId, CancellationToken cancellationToken = default)
    {
        var items = new List<ReportExport>();
        await using var connection = SqliteConnectionFactory.Create();
        await connection.OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = @"SELECT id, record_id, report_code, file_format, file_path, exported_at, export_status
FROM report_exports WHERE record_id = $record_id ORDER BY exported_at DESC;";
        command.Parameters.AddWithValue("$record_id", recordId);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            items.Add(Map(reader));
        }

        return items;
    }

    private static ReportExport Map(SqliteDataReader reader)
    {
        return new ReportExport
        {
            Id = reader.GetString(0),
            RecordId = reader.GetString(1),
            ReportCode = reader.GetString(2),
            FileFormat = reader.GetString(3),
            FilePath = reader.IsDBNull(4) ? null : reader.GetString(4),
            ExportedAt = DateTime.Parse(reader.GetString(5)),
            ExportStatus = reader.GetString(6),
        };
    }
}
