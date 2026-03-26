using MAS.Application.Services;
using MAS.Core.Entities;
using MAS.Infrastructure.Database;
using MAS.Infrastructure.Repositories;
using Microsoft.Data.Sqlite;

var bootstrapper = new SqliteScriptBootstrapper();
await bootstrapper.EnsureCreatedAsync();
await EnsureBaseDataAsync(bootstrapper.DatabasePath);

var sampleRepository = new SqliteSampleRepository();
var standardSampleRepository = new SqliteStandardSampleRepository();
var templateRepository = new SqliteToleranceTemplateRepository();
var taskRepository = new SqliteMeasurementTaskRepository();
var taskService = new MeasurementTaskService();

var template = await templateRepository.GetByCodeAsync("TPL-DEFAULT") ?? new ToleranceTemplate
{
    TemplateCode = "TPL-DEFAULT",
    TemplateName = "默认容差模板",
    DeltaEFormula = "DE00",
    OverallUpperLimit = 1.0,
    EffectUpperLimit = 1.0,
    IsDefault = true,
    Status = "active",
};
if (await templateRepository.GetByCodeAsync(template.TemplateCode) is null)
{
    await templateRepository.AddAsync(template);
}

template = (await templateRepository.GetByCodeAsync("TPL-DEFAULT"))!;

var sample = await sampleRepository.GetByCodeAsync("SP-DEMO-001") ?? new Sample
{
    SampleCode = "SP-DEMO-001",
    SampleName = "演示试样",
    BatchNo = "LOT-DEMO-001",
    MaterialName = "喷涂金属板",
    ColorName = "银灰",
    Status = "active",
};
if (await sampleRepository.GetByCodeAsync(sample.SampleCode) is null)
{
    await sampleRepository.AddAsync(sample);
}

sample = (await sampleRepository.GetByCodeAsync("SP-DEMO-001"))!;

var standardSample = await standardSampleRepository.GetByCodeAsync("STD-DEMO-001") ?? new StandardSample
{
    LibraryId = "library-default",
    StandardCode = "STD-DEMO-001",
    StandardName = "演示标准样",
    VersionNo = 1,
    MaterialName = "喷涂金属板",
    ColorName = "银灰",
    ToleranceTemplateId = template.Id,
    IsActive = true,
    IsDefaultVersion = true,
};
if (await standardSampleRepository.GetByCodeAsync(standardSample.StandardCode) is null)
{
    await standardSampleRepository.AddAsync(standardSample);
}

standardSample = (await standardSampleRepository.GetByCodeAsync("STD-DEMO-001"))!;

var task = taskService.CreateDraftTask(
    instrumentId: "instrument-default",
    sampleId: sample.Id,
    standardSampleId: standardSample.Id,
    templateId: template.Id);
await taskRepository.AddAsync(task);

var samples = await sampleRepository.GetAllAsync();
var standards = await standardSampleRepository.GetAllAsync();
var templates = await templateRepository.GetAllAsync();
var tasks = await taskRepository.GetAllAsync();

Console.WriteLine("MAS V1 development tool");
Console.WriteLine($"Database file: {bootstrapper.DatabasePath}");
Console.WriteLine($"Schema resource: {bootstrapper.SchemaScriptPath}");
Console.WriteLine($"Samples: {samples.Count}");
Console.WriteLine($"Standard samples: {standards.Count}");
Console.WriteLine($"Templates: {templates.Count}");
Console.WriteLine($"Tasks: {tasks.Count}");
Console.WriteLine($"Latest task: {task.TaskCode}");
Console.WriteLine("Database bootstrap and repository verification completed successfully.");

static async Task EnsureBaseDataAsync(string databasePath)
{
    var connectionString = new SqliteConnectionStringBuilder
    {
        DataSource = databasePath,
        ForeignKeys = true,
    }.ToString();

    await using var connection = new SqliteConnection(connectionString);
    await connection.OpenAsync();

    await using (var command = connection.CreateCommand())
    {
        command.CommandText = @"
INSERT OR IGNORE INTO instruments (id, instrument_code, instrument_name, model, connection_type, is_default, status, created_at, updated_at)
VALUES ('instrument-default', 'INS-DEMO-001', '演示仪器', 'MAS-6A', 'serial', 1, 'idle', $created_at, $updated_at);";
        command.Parameters.AddWithValue("$created_at", DateTime.UtcNow.ToString("O"));
        command.Parameters.AddWithValue("$updated_at", DateTime.UtcNow.ToString("O"));
        await command.ExecuteNonQueryAsync();
    }

    await using (var command = connection.CreateCommand())
    {
        command.CommandText = @"
INSERT OR IGNORE INTO color_libraries (id, library_code, library_name, is_default, created_at, updated_at)
VALUES ('library-default', 'LIB-DEMO-001', '默认颜色库', 1, $created_at, $updated_at);";
        command.Parameters.AddWithValue("$created_at", DateTime.UtcNow.ToString("O"));
        command.Parameters.AddWithValue("$updated_at", DateTime.UtcNow.ToString("O"));
        await command.ExecuteNonQueryAsync();
    }
}
