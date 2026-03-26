using System.Text;
using MAS.Application.Abstractions;
using MAS.Application.Models;
using MAS.Core.Entities;

namespace MAS.Application.Services;

public sealed class MeasurementReportService : IMeasurementReportService
{
    private readonly IMeasurementRecordRepository _recordRepository;
    private readonly IMeasurementTaskRepository _taskRepository;
    private readonly IMeasurementAngleResultRepository _angleResultRepository;
    private readonly IMeasurementEffectResultRepository _effectResultRepository;
    private readonly ISampleRepository _sampleRepository;
    private readonly IStandardSampleRepository _standardSampleRepository;
    private readonly IToleranceTemplateRepository _templateRepository;
    private readonly IReportExportRepository _reportExportRepository;
    private readonly IOperationLogRepository _operationLogRepository;

    public MeasurementReportService(
        IMeasurementRecordRepository recordRepository,
        IMeasurementTaskRepository taskRepository,
        IMeasurementAngleResultRepository angleResultRepository,
        IMeasurementEffectResultRepository effectResultRepository,
        ISampleRepository sampleRepository,
        IStandardSampleRepository standardSampleRepository,
        IToleranceTemplateRepository templateRepository,
        IReportExportRepository reportExportRepository,
        IOperationLogRepository operationLogRepository)
    {
        _recordRepository = recordRepository;
        _taskRepository = taskRepository;
        _angleResultRepository = angleResultRepository;
        _effectResultRepository = effectResultRepository;
        _sampleRepository = sampleRepository;
        _standardSampleRepository = standardSampleRepository;
        _templateRepository = templateRepository;
        _reportExportRepository = reportExportRepository;
        _operationLogRepository = operationLogRepository;
    }

    public async Task<MeasurementReportExportResult> ExportRecordReportAsync(string recordId, CancellationToken cancellationToken = default)
    {
        var record = await _recordRepository.GetByIdAsync(recordId, cancellationToken)
                     ?? throw new InvalidOperationException($"测量记录不存在: {recordId}");
        var task = await _taskRepository.GetByIdAsync(record.TaskId, cancellationToken)
                   ?? throw new InvalidOperationException($"关联任务不存在: {record.TaskId}");
        var sample = task.SampleId is null ? null : await _sampleRepository.GetByIdAsync(task.SampleId, cancellationToken);
        var standardSample = task.StandardSampleId is null ? null : await _standardSampleRepository.GetByIdAsync(task.StandardSampleId, cancellationToken);
        var template = task.TemplateId is null ? null : await _templateRepository.GetByIdAsync(task.TemplateId, cancellationToken);
        var angleResults = await _angleResultRepository.GetByRecordIdAsync(record.Id, cancellationToken);
        var effectResults = await _effectResultRepository.GetByRecordIdAsync(record.Id, cancellationToken);

        var reportCode = $"RPT-{DateTime.Now:yyyyMMdd-HHmmss}";
        var exportFolder = Path.Combine(AppContext.BaseDirectory, "Exports", "Reports");
        Directory.CreateDirectory(exportFolder);
        var filePath = Path.Combine(exportFolder, $"{reportCode}_{task.TaskCode}_{record.RecordType}.md");

        var builder = new StringBuilder();
        builder.AppendLine("# 多角度测色仪测量报告");
        builder.AppendLine();
        builder.AppendLine("## 基本信息");
        builder.AppendLine($"- 报告编号：{reportCode}");
        builder.AppendLine($"- 任务编号：{task.TaskCode}");
        builder.AppendLine($"- 记录类型：{record.RecordType}");
        builder.AppendLine($"- 测量时间：{record.MeasuredAt:yyyy-MM-dd HH:mm:ss}");
        builder.AppendLine($"- 判定结果：{record.PassStatus}");
        builder.AppendLine($"- 综合色差：{record.TotalDeltaE:0.00}");
        builder.AppendLine($"- 效果差：{record.TotalEffectDiff:0.00}");
        builder.AppendLine();
        builder.AppendLine("## 样品信息");
        builder.AppendLine($"- 试样：{sample?.SampleCode ?? "-"} / {sample?.SampleName ?? "-"}");
        builder.AppendLine($"- 标准样：{standardSample?.StandardCode ?? "-"} / {standardSample?.StandardName ?? "-"}");
        builder.AppendLine($"- 容差模板：{template?.TemplateCode ?? "-"} / {template?.TemplateName ?? "-"}");
        builder.AppendLine();
        builder.AppendLine("## 结果摘要");
        builder.AppendLine(record.ResultSummary ?? "-");
        builder.AppendLine();
        builder.AppendLine("## 角度结果");
        builder.AppendLine("| 角度 | L* | a* | b* | ΔE | 判定 |");
        builder.AppendLine("| --- | ---: | ---: | ---: | ---: | --- |");
        foreach (var angle in angleResults)
        {
            builder.AppendLine($"| {angle.AngleCode} | {angle.CieL:0.00} | {angle.CieA:0.00} | {angle.CieB:0.00} | {angle.DeltaE:0.00} | {angle.PassStatus} |");
        }
        builder.AppendLine();
        builder.AppendLine("## 效果结果");
        builder.AppendLine("| 角度 | Sparkle | Sparkle差值 | Graininess | Graininess差值 | 判定 |");
        builder.AppendLine("| --- | ---: | ---: | ---: | ---: | --- |");
        foreach (var effect in effectResults)
        {
            builder.AppendLine($"| {effect.AngleCode} | {effect.SparkleValue:0.00} | {effect.SparkleDiff:0.00} | {effect.GraininessValue:0.00} | {effect.GraininessDiff:0.00} | {effect.EffectPassStatus} |");
        }

        await File.WriteAllTextAsync(filePath, builder.ToString(), Encoding.UTF8, cancellationToken);

        var export = new ReportExport
        {
            RecordId = record.Id,
            ReportCode = reportCode,
            FileFormat = "md",
            FilePath = filePath,
            ExportStatus = "success",
            ExportedAt = DateTime.UtcNow,
        };
        await _reportExportRepository.AddAsync(export, cancellationToken);
        await _operationLogRepository.AddAsync(new OperationLog
        {
            TaskId = task.Id,
            RecordId = record.Id,
            ModuleName = "report",
            OperationType = "export",
            OperationDesc = $"导出测量报告 {reportCode}",
            OperationResult = "success",
            CreatedAt = DateTime.UtcNow,
        }, cancellationToken);

        return new MeasurementReportExportResult
        {
            FilePath = filePath,
            FileFormat = "md",
            ReportCode = reportCode,
        };
    }
}
