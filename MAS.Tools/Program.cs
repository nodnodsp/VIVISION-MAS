using MAS.Application.Services;
using MAS.Core.Entities;
using MAS.Core.Enums;
using MAS.Infrastructure.Database;
using MAS.Infrastructure.Repositories;

var bootstrapper = new SqliteScriptBootstrapper();
await bootstrapper.EnsureCreatedAsync();
await DefaultDataSeeder.EnsureSeedDataAsync();

var sampleRepository = new SqliteSampleRepository();
var standardSampleRepository = new SqliteStandardSampleRepository();
var templateRepository = new SqliteToleranceTemplateRepository();
var taskRepository = new SqliteMeasurementTaskRepository();
var measurementRecordRepository = new SqliteMeasurementRecordRepository();
var angleResultRepository = new SqliteMeasurementAngleResultRepository();
var effectResultRepository = new SqliteMeasurementEffectResultRepository();
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

var taskRecords = await measurementRecordRepository.GetByTaskIdAsync(task.Id);
if (taskRecords.Count == 0)
{
    var record = new MeasurementRecord
    {
        TaskId = task.Id,
        RecordNo = 1,
        RecordType = "trial",
        TotalDeltaE = 0.68,
        TotalEffectDiff = 0.32,
        PassStatus = PassStatus.Pass,
        ResultSummary = "演示记录：综合色差与效果差均在容差内。",
        MeasuredAt = DateTime.UtcNow,
    };

    await measurementRecordRepository.AddAsync(record);

    await angleResultRepository.AddRangeAsync(new[]
    {
        new MeasurementAngleResult
        {
            RecordId = record.Id,
            AngleCode = "15as-15",
            CieL = 51.24,
            CieA = -0.32,
            CieB = -1.68,
            DeltaE = 0.52,
            PassStatus = PassStatus.Pass,
        },
        new MeasurementAngleResult
        {
            RecordId = record.Id,
            AngleCode = "45as110",
            CieL = 63.14,
            CieA = -1.04,
            CieB = -3.18,
            DeltaE = 0.81,
            PassStatus = PassStatus.Pass,
        },
    });

    await effectResultRepository.AddRangeAsync(new[]
    {
        new MeasurementEffectResult
        {
            RecordId = record.Id,
            AngleCode = "45as110",
            SparkleValue = 4.12,
            SparkleDiff = 0.18,
            GraininessValue = 2.06,
            GraininessDiff = 0.14,
            EffectPassStatus = PassStatus.Pass,
        },
    });
}

var samples = await sampleRepository.GetAllAsync();
var standards = await standardSampleRepository.GetAllAsync();
var templates = await templateRepository.GetAllAsync();
var tasks = await taskRepository.GetAllAsync();
var records = await measurementRecordRepository.GetAllAsync();
var latestRecord = records.FirstOrDefault();
var angleResults = latestRecord is null ? Array.Empty<MeasurementAngleResult>() : await angleResultRepository.GetByRecordIdAsync(latestRecord.Id);
var effectResults = latestRecord is null ? Array.Empty<MeasurementEffectResult>() : await effectResultRepository.GetByRecordIdAsync(latestRecord.Id);

Console.WriteLine("MAS V1 development tool");
Console.WriteLine($"Database file: {bootstrapper.DatabasePath}");
Console.WriteLine($"Schema resource: {bootstrapper.SchemaScriptPath}");
Console.WriteLine($"Samples: {samples.Count}");
Console.WriteLine($"Standard samples: {standards.Count}");
Console.WriteLine($"Templates: {templates.Count}");
Console.WriteLine($"Tasks: {tasks.Count}");
Console.WriteLine($"Measurement records: {records.Count}");
Console.WriteLine($"Latest task: {task.TaskCode}");
if (latestRecord is not null)
{
    Console.WriteLine($"Latest record: {latestRecord.Id} / DeltaE={latestRecord.TotalDeltaE} / Effect={latestRecord.TotalEffectDiff}");
    Console.WriteLine($"Angle results: {angleResults.Count}");
    Console.WriteLine($"Effect results: {effectResults.Count}");
}
Console.WriteLine("Database bootstrap and repository verification completed successfully.");
