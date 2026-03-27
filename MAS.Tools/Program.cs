using MAS.Application.Services;
using MAS.Core.Entities;
using MAS.Infrastructure.Configuration;
using MAS.Infrastructure.Database;
using MAS.Infrastructure.Repositories;
using MAS.Infrastructure.Runtime;

var bootstrapper = new SqliteScriptBootstrapper();
await bootstrapper.EnsureCreatedAsync();
await DefaultDataSeeder.EnsureSeedDataAsync();

var instrumentRepository = new SqliteInstrumentRepository();
var calibrationRecordRepository = new SqliteCalibrationRecordRepository();
var sampleRepository = new SqliteSampleRepository();
var standardSampleRepository = new SqliteStandardSampleRepository();
var templateRepository = new SqliteToleranceTemplateRepository();
var taskRepository = new SqliteMeasurementTaskRepository();
var measurementRecordRepository = new SqliteMeasurementRecordRepository();
var angleResultRepository = new SqliteMeasurementAngleResultRepository();
var effectResultRepository = new SqliteMeasurementEffectResultRepository();
var reportExportRepository = new SqliteReportExportRepository();
var operationLogRepository = new SqliteOperationLogRepository();
var rawPacketRepository = new SqliteRawPacketRepository();
var appSettingsStore = new AppSettingsStore();
var appSettings = await appSettingsStore.LoadAsync();
var runtimeFactory = new InstrumentRuntimeFactory();
var runtimeServices = runtimeFactory.Create(appSettings, instrumentRepository, calibrationRecordRepository, rawPacketRepository);

var instrumentConnectionService = runtimeServices.ConnectionService;
var taskService = new MeasurementTaskService();
var workflowService = new MeasurementWorkflowService(
    taskRepository,
    measurementRecordRepository,
    angleResultRepository,
    effectResultRepository,
    runtimeServices.MeasurementService);
var reportService = new MeasurementReportService(
    measurementRecordRepository,
    taskRepository,
    angleResultRepository,
    effectResultRepository,
    sampleRepository,
    standardSampleRepository,
    templateRepository,
    reportExportRepository,
    operationLogRepository);

var instrument = await instrumentRepository.GetDefaultAsync()
                 ?? throw new InvalidOperationException("未找到默认仪器。");
var connectedInstrument = await instrumentConnectionService.ConnectAsync(instrument.Id);
var whiteCalibration = await instrumentConnectionService.CalibrateAsync(instrument.Id, "white");

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
    instrumentId: instrument.Id,
    sampleId: sample.Id,
    standardSampleId: standardSample.Id,
    templateId: template.Id);
await taskRepository.AddAsync(task);

var standardResult = await workflowService.ExecuteMeasurementAsync(task.TaskCode, "standard");
var trialResult = await workflowService.ExecuteMeasurementAsync(task.TaskCode, "trial");
var markdownReportResult = await reportService.ExportRecordReportAsync(trialResult.Record.Id, "md");
var htmlReportResult = await reportService.ExportRecordReportAsync(trialResult.Record.Id, "html");
var csvReportResult = await reportService.ExportRecordReportAsync(trialResult.Record.Id, "csv");

var instruments = await instrumentRepository.GetAllAsync();
var calibrations = await calibrationRecordRepository.GetAllAsync();
var samples = await sampleRepository.GetAllAsync();
var standards = await standardSampleRepository.GetAllAsync();
var templates = await templateRepository.GetAllAsync();
var tasks = await taskRepository.GetAllAsync();
var records = await measurementRecordRepository.GetAllAsync();
var exports = await reportExportRepository.GetAllAsync();
var logs = await operationLogRepository.GetRecentAsync(10);
var rawPackets = await rawPacketRepository.GetRecentAsync(10);
var latestRecord = records.FirstOrDefault();
var angleResults = latestRecord is null ? Array.Empty<MeasurementAngleResult>() : await angleResultRepository.GetByRecordIdAsync(latestRecord.Id);
var effectResults = latestRecord is null ? Array.Empty<MeasurementEffectResult>() : await effectResultRepository.GetByRecordIdAsync(latestRecord.Id);
var latestTask = await taskRepository.GetByCodeAsync(task.TaskCode);

Console.WriteLine("MAS V1 development tool");
Console.WriteLine($"Database file: {bootstrapper.DatabasePath}");
Console.WriteLine($"Schema resource: {bootstrapper.SchemaScriptPath}");
Console.WriteLine($"Runtime mode: {appSettings.InstrumentRuntimeMode} / {runtimeServices.RuntimeDescription}");
Console.WriteLine($"Instruments: {instruments.Count} / Default status={connectedInstrument.Status} / Port={connectedInstrument.PortName}");
Console.WriteLine($"Calibration records: {calibrations.Count} / Latest={whiteCalibration.CalibrationType}:{whiteCalibration.ResultCode}");
Console.WriteLine($"Samples: {samples.Count}");
Console.WriteLine($"Standard samples: {standards.Count}");
Console.WriteLine($"Templates: {templates.Count}");
Console.WriteLine($"Tasks: {tasks.Count}");
Console.WriteLine($"Measurement records: {records.Count}");
Console.WriteLine($"Report exports: {exports.Count} / Latest={csvReportResult.ReportCode}");
Console.WriteLine($"Recent operation logs: {logs.Count}");
Console.WriteLine($"Recent raw packets: {rawPackets.Count}");
Console.WriteLine($"Latest task: {task.TaskCode} / Status={latestTask?.Status}");
Console.WriteLine($"Standard measurement record: {standardResult.Record.RecordNo} / DeltaE={standardResult.Record.TotalDeltaE}");
Console.WriteLine($"Trial measurement record: {trialResult.Record.RecordNo} / DeltaE={trialResult.Record.TotalDeltaE}");
Console.WriteLine($"Markdown report path: {markdownReportResult.FilePath}");
Console.WriteLine($"Html report path: {htmlReportResult.FilePath}");
Console.WriteLine($"Csv report path: {csvReportResult.FilePath}");
if (latestRecord is not null)
{
    Console.WriteLine($"Latest record: {latestRecord.Id} / Type={latestRecord.RecordType} / DeltaE={latestRecord.TotalDeltaE} / Effect={latestRecord.TotalEffectDiff}");
    Console.WriteLine($"Angle results: {angleResults.Count}");
    Console.WriteLine($"Effect results: {effectResults.Count}");
}
Console.WriteLine("Database bootstrap, workflow and report verification completed successfully.");


