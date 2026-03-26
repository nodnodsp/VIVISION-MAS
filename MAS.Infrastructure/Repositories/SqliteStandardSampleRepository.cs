using MAS.Application.Abstractions;
using MAS.Core.Entities;
using MAS.Infrastructure.Database;
using Microsoft.Data.Sqlite;

namespace MAS.Infrastructure.Repositories;

public sealed class SqliteStandardSampleRepository : IStandardSampleRepository
{
    public async Task AddAsync(StandardSample standardSample, CancellationToken cancellationToken = default)
    {
        await using var connection = SqliteConnectionFactory.Create();
        await connection.OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = @"
INSERT INTO standard_samples (id, library_id, standard_code, standard_name, version_no, material_name, color_name, batch_no, tolerance_template_id, is_active, is_default_version, remark, created_at, updated_at)
VALUES ($id, $library_id, $standard_code, $standard_name, $version_no, $material_name, $color_name, $batch_no, $tolerance_template_id, $is_active, $is_default_version, $remark, $created_at, $updated_at);";
        command.Parameters.AddWithValue("$id", standardSample.Id);
        command.Parameters.AddWithValue("$library_id", standardSample.LibraryId);
        command.Parameters.AddWithValue("$standard_code", standardSample.StandardCode);
        command.Parameters.AddWithValue("$standard_name", standardSample.StandardName);
        command.Parameters.AddWithValue("$version_no", standardSample.VersionNo);
        command.Parameters.AddWithValue("$material_name", (object?)standardSample.MaterialName ?? DBNull.Value);
        command.Parameters.AddWithValue("$color_name", (object?)standardSample.ColorName ?? DBNull.Value);
        command.Parameters.AddWithValue("$batch_no", DBNull.Value);
        command.Parameters.AddWithValue("$tolerance_template_id", (object?)standardSample.ToleranceTemplateId ?? DBNull.Value);
        command.Parameters.AddWithValue("$is_active", standardSample.IsActive ? 1 : 0);
        command.Parameters.AddWithValue("$is_default_version", standardSample.IsDefaultVersion ? 1 : 0);
        command.Parameters.AddWithValue("$remark", DBNull.Value);
        command.Parameters.AddWithValue("$created_at", standardSample.CreatedAt.ToString("O"));
        command.Parameters.AddWithValue("$updated_at", standardSample.UpdatedAt.ToString("O"));
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task<StandardSample?> GetByIdAsync(string standardSampleId, CancellationToken cancellationToken = default)
    {
        await using var connection = SqliteConnectionFactory.Create();
        await connection.OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = @"SELECT id, library_id, standard_code, standard_name, version_no, material_name, color_name, tolerance_template_id, is_active, is_default_version, created_at, updated_at
FROM standard_samples WHERE id = $id LIMIT 1;";
        command.Parameters.AddWithValue("$id", standardSampleId);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        return await reader.ReadAsync(cancellationToken) ? Map(reader) : null;
    }

    public async Task<StandardSample?> GetByCodeAsync(string standardCode, CancellationToken cancellationToken = default)
    {
        await using var connection = SqliteConnectionFactory.Create();
        await connection.OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = @"SELECT id, library_id, standard_code, standard_name, version_no, material_name, color_name, tolerance_template_id, is_active, is_default_version, created_at, updated_at
FROM standard_samples WHERE standard_code = $standard_code LIMIT 1;";
        command.Parameters.AddWithValue("$standard_code", standardCode);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        return await reader.ReadAsync(cancellationToken) ? Map(reader) : null;
    }

    public async Task<IReadOnlyList<StandardSample>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var items = new List<StandardSample>();
        await using var connection = SqliteConnectionFactory.Create();
        await connection.OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = @"SELECT id, library_id, standard_code, standard_name, version_no, material_name, color_name, tolerance_template_id, is_active, is_default_version, created_at, updated_at
FROM standard_samples ORDER BY created_at;";
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            items.Add(Map(reader));
        }

        return items;
    }

    private static StandardSample Map(SqliteDataReader reader)
    {
        return new StandardSample
        {
            Id = reader.GetString(0),
            LibraryId = reader.GetString(1),
            StandardCode = reader.GetString(2),
            StandardName = reader.GetString(3),
            VersionNo = reader.GetInt32(4),
            MaterialName = reader.IsDBNull(5) ? null : reader.GetString(5),
            ColorName = reader.IsDBNull(6) ? null : reader.GetString(6),
            ToleranceTemplateId = reader.IsDBNull(7) ? null : reader.GetString(7),
            IsActive = reader.GetInt32(8) == 1,
            IsDefaultVersion = reader.GetInt32(9) == 1,
            CreatedAt = DateTime.Parse(reader.GetString(10)),
            UpdatedAt = DateTime.Parse(reader.GetString(11)),
        };
    }
}
