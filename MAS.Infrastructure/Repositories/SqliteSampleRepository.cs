using MAS.Application.Abstractions;
using MAS.Core.Entities;
using MAS.Infrastructure.Database;
using Microsoft.Data.Sqlite;

namespace MAS.Infrastructure.Repositories;

public sealed class SqliteSampleRepository : ISampleRepository
{
    public async Task AddAsync(Sample sample, CancellationToken cancellationToken = default)
    {
        await using var connection = SqliteConnectionFactory.Create();
        await connection.OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = @"
INSERT INTO samples (id, sample_code, sample_name, batch_no, material_name, color_name, source_type, status, remark, created_at, updated_at)
VALUES ($id, $sample_code, $sample_name, $batch_no, $material_name, $color_name, $source_type, $status, $remark, $created_at, $updated_at);";
        BindCommonSample(command, sample);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task<Sample?> GetByIdAsync(string sampleId, CancellationToken cancellationToken = default)
    {
        await using var connection = SqliteConnectionFactory.Create();
        await connection.OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = @"SELECT id, sample_code, sample_name, batch_no, material_name, color_name, status, created_at, updated_at
FROM samples WHERE id = $id LIMIT 1;";
        command.Parameters.AddWithValue("$id", sampleId);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        return await reader.ReadAsync(cancellationToken) ? Map(reader) : null;
    }

    public async Task<Sample?> GetByCodeAsync(string sampleCode, CancellationToken cancellationToken = default)
    {
        await using var connection = SqliteConnectionFactory.Create();
        await connection.OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = @"SELECT id, sample_code, sample_name, batch_no, material_name, color_name, status, created_at, updated_at
FROM samples WHERE sample_code = $sample_code LIMIT 1;";
        command.Parameters.AddWithValue("$sample_code", sampleCode);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        return await reader.ReadAsync(cancellationToken) ? Map(reader) : null;
    }

    public async Task<IReadOnlyList<Sample>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var items = new List<Sample>();
        await using var connection = SqliteConnectionFactory.Create();
        await connection.OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = @"SELECT id, sample_code, sample_name, batch_no, material_name, color_name, status, created_at, updated_at
FROM samples ORDER BY created_at;";
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            items.Add(Map(reader));
        }

        return items;
    }

    private static void BindCommonSample(SqliteCommand command, Sample sample)
    {
        command.Parameters.AddWithValue("$id", sample.Id);
        command.Parameters.AddWithValue("$sample_code", sample.SampleCode);
        command.Parameters.AddWithValue("$sample_name", sample.SampleName);
        command.Parameters.AddWithValue("$batch_no", (object?)sample.BatchNo ?? DBNull.Value);
        command.Parameters.AddWithValue("$material_name", (object?)sample.MaterialName ?? DBNull.Value);
        command.Parameters.AddWithValue("$color_name", (object?)sample.ColorName ?? DBNull.Value);
        command.Parameters.AddWithValue("$source_type", DBNull.Value);
        command.Parameters.AddWithValue("$status", sample.Status);
        command.Parameters.AddWithValue("$remark", DBNull.Value);
        command.Parameters.AddWithValue("$created_at", sample.CreatedAt.ToString("O"));
        command.Parameters.AddWithValue("$updated_at", sample.UpdatedAt.ToString("O"));
    }

    private static Sample Map(SqliteDataReader reader)
    {
        return new Sample
        {
            Id = reader.GetString(0),
            SampleCode = reader.GetString(1),
            SampleName = reader.GetString(2),
            BatchNo = reader.IsDBNull(3) ? null : reader.GetString(3),
            MaterialName = reader.IsDBNull(4) ? null : reader.GetString(4),
            ColorName = reader.IsDBNull(5) ? null : reader.GetString(5),
            Status = reader.GetString(6),
            CreatedAt = DateTime.Parse(reader.GetString(7)),
            UpdatedAt = DateTime.Parse(reader.GetString(8)),
        };
    }
}
