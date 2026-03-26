using MAS.Application.Abstractions;
using MAS.Core.Entities;
using MAS.Infrastructure.Database;
using Microsoft.Data.Sqlite;

namespace MAS.Infrastructure.Repositories;

public sealed class SqliteToleranceTemplateRepository : IToleranceTemplateRepository
{
    public async Task AddAsync(ToleranceTemplate template, CancellationToken cancellationToken = default)
    {
        await using var connection = SqliteConnectionFactory.Create();
        await connection.OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = @"
INSERT INTO tolerance_templates (id, template_code, template_name, product_type, delta_e_formula, overall_lower_limit, overall_upper_limit, effect_lower_limit, effect_upper_limit, warning_enabled, is_default, status, created_at, updated_at)
VALUES ($id, $template_code, $template_name, $product_type, $delta_e_formula, $overall_lower_limit, $overall_upper_limit, $effect_lower_limit, $effect_upper_limit, $warning_enabled, $is_default, $status, $created_at, $updated_at);";
        command.Parameters.AddWithValue("$id", template.Id);
        command.Parameters.AddWithValue("$template_code", template.TemplateCode);
        command.Parameters.AddWithValue("$template_name", template.TemplateName);
        command.Parameters.AddWithValue("$product_type", DBNull.Value);
        command.Parameters.AddWithValue("$delta_e_formula", template.DeltaEFormula);
        command.Parameters.AddWithValue("$overall_lower_limit", DBNull.Value);
        command.Parameters.AddWithValue("$overall_upper_limit", (object?)template.OverallUpperLimit ?? DBNull.Value);
        command.Parameters.AddWithValue("$effect_lower_limit", DBNull.Value);
        command.Parameters.AddWithValue("$effect_upper_limit", (object?)template.EffectUpperLimit ?? DBNull.Value);
        command.Parameters.AddWithValue("$warning_enabled", 1);
        command.Parameters.AddWithValue("$is_default", template.IsDefault ? 1 : 0);
        command.Parameters.AddWithValue("$status", template.Status);
        command.Parameters.AddWithValue("$created_at", template.CreatedAt.ToString("O"));
        command.Parameters.AddWithValue("$updated_at", template.UpdatedAt.ToString("O"));
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task<ToleranceTemplate?> GetByIdAsync(string templateId, CancellationToken cancellationToken = default)
    {
        await using var connection = SqliteConnectionFactory.Create();
        await connection.OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = @"SELECT id, template_code, template_name, delta_e_formula, overall_upper_limit, effect_upper_limit, is_default, status, created_at, updated_at
FROM tolerance_templates WHERE id = $id LIMIT 1;";
        command.Parameters.AddWithValue("$id", templateId);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        return await reader.ReadAsync(cancellationToken) ? Map(reader) : null;
    }

    public async Task<ToleranceTemplate?> GetByCodeAsync(string templateCode, CancellationToken cancellationToken = default)
    {
        await using var connection = SqliteConnectionFactory.Create();
        await connection.OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = @"SELECT id, template_code, template_name, delta_e_formula, overall_upper_limit, effect_upper_limit, is_default, status, created_at, updated_at
FROM tolerance_templates WHERE template_code = $template_code LIMIT 1;";
        command.Parameters.AddWithValue("$template_code", templateCode);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        return await reader.ReadAsync(cancellationToken) ? Map(reader) : null;
    }

    public async Task<IReadOnlyList<ToleranceTemplate>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var items = new List<ToleranceTemplate>();
        await using var connection = SqliteConnectionFactory.Create();
        await connection.OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = @"SELECT id, template_code, template_name, delta_e_formula, overall_upper_limit, effect_upper_limit, is_default, status, created_at, updated_at
FROM tolerance_templates ORDER BY created_at;";
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            items.Add(Map(reader));
        }

        return items;
    }

    private static ToleranceTemplate Map(SqliteDataReader reader)
    {
        return new ToleranceTemplate
        {
            Id = reader.GetString(0),
            TemplateCode = reader.GetString(1),
            TemplateName = reader.GetString(2),
            DeltaEFormula = reader.GetString(3),
            OverallUpperLimit = reader.IsDBNull(4) ? null : reader.GetDouble(4),
            EffectUpperLimit = reader.IsDBNull(5) ? null : reader.GetDouble(5),
            IsDefault = reader.GetInt32(6) == 1,
            Status = reader.GetString(7),
            CreatedAt = DateTime.Parse(reader.GetString(8)),
            UpdatedAt = DateTime.Parse(reader.GetString(9)),
        };
    }
}
