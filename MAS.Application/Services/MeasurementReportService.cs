using System.Net;
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

    public async Task<MeasurementReportExportResult> ExportRecordReportAsync(string recordId, string fileFormat = "md", CancellationToken cancellationToken = default)
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

        var normalizedFormat = NormalizeFormat(fileFormat);
        var reportCode = $"RPT-{DateTime.Now:yyyyMMdd-HHmmss}";
        var exportFolder = Path.Combine(AppContext.BaseDirectory, "Exports", "Reports");
        Directory.CreateDirectory(exportFolder);
        var filePath = Path.Combine(exportFolder, $"{reportCode}_{task.TaskCode}_{record.RecordType}.{normalizedFormat}");

        var content = normalizedFormat switch
        {
            "html" => BuildHtmlReport(reportCode, task, record, sample, standardSample, template, angleResults, effectResults),
            "csv" => BuildCsvReport(reportCode, task, record, sample, standardSample, template, angleResults, effectResults),
            _ => BuildMarkdownReport(reportCode, task, record, sample, standardSample, template, angleResults, effectResults),
        };

        await File.WriteAllTextAsync(filePath, content, Encoding.UTF8, cancellationToken);

        var export = new ReportExport
        {
            RecordId = record.Id,
            ReportCode = reportCode,
            FileFormat = normalizedFormat,
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
            OperationDesc = $"导出{normalizedFormat.ToUpperInvariant()}测量报告 {reportCode}",
            OperationResult = "success",
            CreatedAt = DateTime.UtcNow,
        }, cancellationToken);

        return new MeasurementReportExportResult
        {
            FilePath = filePath,
            FileFormat = normalizedFormat,
            ReportCode = reportCode,
        };
    }

    private static string NormalizeFormat(string? fileFormat)
    {
        return fileFormat?.Trim().ToLowerInvariant() switch
        {
            "md" or "markdown" => "md",
            "html" or "htm" => "html",
            "csv" => "csv",
            _ => "md",
        };
    }

    private static string BuildMarkdownReport(
        string reportCode,
        MeasurementTask task,
        MeasurementRecord record,
        Sample? sample,
        StandardSample? standardSample,
        ToleranceTemplate? template,
        IReadOnlyList<MeasurementAngleResult> angleResults,
        IReadOnlyList<MeasurementEffectResult> effectResults)
    {
        var builder = new StringBuilder();
        builder.AppendLine("# 多角度测色仪测量报告");
        builder.AppendLine();
        builder.AppendLine("## 基本信息");
        builder.AppendLine($"- 报告编号：{reportCode}");
        builder.AppendLine($"- 任务编号：{task.TaskCode}");
        builder.AppendLine($"- 记录类型：{record.RecordType}");
        builder.AppendLine($"- 测量时间：{record.MeasuredAt:yyyy-MM-dd HH:mm:ss}");
        builder.AppendLine($"- 判定结果：{record.PassStatus}");
        builder.AppendLine($"- 综合色差：{FormatNumber(record.TotalDeltaE)}");
        builder.AppendLine($"- 效果差：{FormatNumber(record.TotalEffectDiff)}");
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
            builder.AppendLine($"| {angle.AngleCode} | {FormatNumber(angle.CieL)} | {FormatNumber(angle.CieA)} | {FormatNumber(angle.CieB)} | {FormatNumber(angle.DeltaE)} | {angle.PassStatus} |");
        }

        builder.AppendLine();
        builder.AppendLine("## 效果结果");
        builder.AppendLine("| 角度 | Sparkle | Sparkle差值 | Graininess | Graininess差值 | 判定 |");
        builder.AppendLine("| --- | ---: | ---: | ---: | ---: | --- |");
        foreach (var effect in effectResults)
        {
            builder.AppendLine($"| {effect.AngleCode} | {FormatNumber(effect.SparkleValue)} | {FormatNumber(effect.SparkleDiff)} | {FormatNumber(effect.GraininessValue)} | {FormatNumber(effect.GraininessDiff)} | {effect.EffectPassStatus} |");
        }

        return builder.ToString();
    }

    private static string BuildHtmlReport(
        string reportCode,
        MeasurementTask task,
        MeasurementRecord record,
        Sample? sample,
        StandardSample? standardSample,
        ToleranceTemplate? template,
        IReadOnlyList<MeasurementAngleResult> angleResults,
        IReadOnlyList<MeasurementEffectResult> effectResults)
    {
        var builder = new StringBuilder();
        builder.AppendLine("<!DOCTYPE html>");
        builder.AppendLine("<html lang=\"zh-CN\">");
        builder.AppendLine("<head>");
        builder.AppendLine("<meta charset=\"utf-8\" />");
        builder.AppendLine("<title>多角度测色仪测量报告</title>");
        builder.AppendLine("<style>");
        builder.AppendLine("body{font-family:'Microsoft YaHei UI',sans-serif;background:#f8fafc;color:#0f172a;margin:24px;}");
        builder.AppendLine("h1,h2{color:#0f172a;} .card{background:#ffffff;border:1px solid #cbd5e1;border-radius:12px;padding:16px;margin-bottom:16px;}");
        builder.AppendLine("table{width:100%;border-collapse:collapse;background:#ffffff;} th,td{border:1px solid #cbd5e1;padding:8px 10px;text-align:left;} th{background:#e2e8f0;}");
        builder.AppendLine(".meta{display:grid;grid-template-columns:repeat(2,minmax(220px,1fr));gap:10px;} .strong{font-weight:700;color:#1d4ed8;}");
        builder.AppendLine("</style>");
        builder.AppendLine("</head>");
        builder.AppendLine("<body>");
        builder.AppendLine("<h1>多角度测色仪测量报告</h1>");
        builder.AppendLine("<div class=\"card meta\">");
        builder.AppendLine($"<div><span class=\"strong\">报告编号：</span>{Encode(reportCode)}</div>");
        builder.AppendLine($"<div><span class=\"strong\">任务编号：</span>{Encode(task.TaskCode)}</div>");
        builder.AppendLine($"<div><span class=\"strong\">记录类型：</span>{Encode(record.RecordType)}</div>");
        builder.AppendLine($"<div><span class=\"strong\">测量时间：</span>{Encode(record.MeasuredAt.ToString("yyyy-MM-dd HH:mm:ss"))}</div>");
        builder.AppendLine($"<div><span class=\"strong\">判定结果：</span>{Encode(record.PassStatus.ToString())}</div>");
        builder.AppendLine($"<div><span class=\"strong\">综合色差：</span>{Encode(FormatNumber(record.TotalDeltaE))}</div>");
        builder.AppendLine($"<div><span class=\"strong\">效果差：</span>{Encode(FormatNumber(record.TotalEffectDiff))}</div>");
        builder.AppendLine($"<div><span class=\"strong\">试样：</span>{Encode($"{sample?.SampleCode ?? "-"} / {sample?.SampleName ?? "-"}")}</div>");
        builder.AppendLine($"<div><span class=\"strong\">标准样：</span>{Encode($"{standardSample?.StandardCode ?? "-"} / {standardSample?.StandardName ?? "-"}")}</div>");
        builder.AppendLine($"<div><span class=\"strong\">容差模板：</span>{Encode($"{template?.TemplateCode ?? "-"} / {template?.TemplateName ?? "-"}")}</div>");
        builder.AppendLine("</div>");
        builder.AppendLine("<div class=\"card\">");
        builder.AppendLine("<h2>结果摘要</h2>");
        builder.AppendLine($"<p>{Encode(record.ResultSummary ?? "-")}</p>");
        builder.AppendLine("</div>");
        builder.AppendLine("<div class=\"card\">");
        builder.AppendLine("<h2>角度结果</h2>");
        builder.AppendLine("<table><thead><tr><th>角度</th><th>L*</th><th>a*</th><th>b*</th><th>ΔE</th><th>判定</th></tr></thead><tbody>");
        foreach (var angle in angleResults)
        {
            builder.AppendLine($"<tr><td>{Encode(angle.AngleCode)}</td><td>{Encode(FormatNumber(angle.CieL))}</td><td>{Encode(FormatNumber(angle.CieA))}</td><td>{Encode(FormatNumber(angle.CieB))}</td><td>{Encode(FormatNumber(angle.DeltaE))}</td><td>{Encode(angle.PassStatus.ToString())}</td></tr>");
        }

        builder.AppendLine("</tbody></table>");
        builder.AppendLine("</div>");
        builder.AppendLine("<div class=\"card\">");
        builder.AppendLine("<h2>效果结果</h2>");
        builder.AppendLine("<table><thead><tr><th>角度</th><th>Sparkle</th><th>Sparkle差值</th><th>Graininess</th><th>Graininess差值</th><th>判定</th></tr></thead><tbody>");
        foreach (var effect in effectResults)
        {
            builder.AppendLine($"<tr><td>{Encode(effect.AngleCode)}</td><td>{Encode(FormatNumber(effect.SparkleValue))}</td><td>{Encode(FormatNumber(effect.SparkleDiff))}</td><td>{Encode(FormatNumber(effect.GraininessValue))}</td><td>{Encode(FormatNumber(effect.GraininessDiff))}</td><td>{Encode(effect.EffectPassStatus.ToString())}</td></tr>");
        }

        builder.AppendLine("</tbody></table>");
        builder.AppendLine("</div>");
        builder.AppendLine("</body>");
        builder.AppendLine("</html>");
        return builder.ToString();
    }

    private static string BuildCsvReport(
        string reportCode,
        MeasurementTask task,
        MeasurementRecord record,
        Sample? sample,
        StandardSample? standardSample,
        ToleranceTemplate? template,
        IReadOnlyList<MeasurementAngleResult> angleResults,
        IReadOnlyList<MeasurementEffectResult> effectResults)
    {
        var builder = new StringBuilder();
        AppendCsvRow(builder, "Section", "Field1", "Field2", "Field3", "Field4", "Field5", "Field6");
        AppendCsvRow(builder, "BasicInfo", "ReportCode", reportCode);
        AppendCsvRow(builder, "BasicInfo", "TaskCode", task.TaskCode);
        AppendCsvRow(builder, "BasicInfo", "RecordType", record.RecordType);
        AppendCsvRow(builder, "BasicInfo", "MeasuredAt", record.MeasuredAt.ToString("yyyy-MM-dd HH:mm:ss"));
        AppendCsvRow(builder, "BasicInfo", "PassStatus", record.PassStatus.ToString());
        AppendCsvRow(builder, "BasicInfo", "TotalDeltaE", FormatNumber(record.TotalDeltaE));
        AppendCsvRow(builder, "BasicInfo", "TotalEffectDiff", FormatNumber(record.TotalEffectDiff));
        AppendCsvRow(builder, "Sample", "TrialSample", sample?.SampleCode ?? "-", sample?.SampleName ?? "-");
        AppendCsvRow(builder, "Sample", "StandardSample", standardSample?.StandardCode ?? "-", standardSample?.StandardName ?? "-");
        AppendCsvRow(builder, "Sample", "Template", template?.TemplateCode ?? "-", template?.TemplateName ?? "-");
        AppendCsvRow(builder, "Summary", record.ResultSummary ?? "-");
        AppendCsvRow(builder, "AngleResult", "AngleCode", "L*", "a*", "b*", "DeltaE", "PassStatus");
        foreach (var angle in angleResults)
        {
            AppendCsvRow(builder, "AngleResult", angle.AngleCode, FormatNumber(angle.CieL), FormatNumber(angle.CieA), FormatNumber(angle.CieB), FormatNumber(angle.DeltaE), angle.PassStatus.ToString());
        }

        AppendCsvRow(builder, "EffectResult", "AngleCode", "Sparkle", "SparkleDiff", "Graininess", "GraininessDiff", "PassStatus");
        foreach (var effect in effectResults)
        {
            AppendCsvRow(builder, "EffectResult", effect.AngleCode, FormatNumber(effect.SparkleValue), FormatNumber(effect.SparkleDiff), FormatNumber(effect.GraininessValue), FormatNumber(effect.GraininessDiff), effect.EffectPassStatus.ToString());
        }

        return builder.ToString();
    }

    private static void AppendCsvRow(StringBuilder builder, params string?[] values)
    {
        builder.AppendLine(string.Join(",", values.Select(EscapeCsv)));
    }

    private static string EscapeCsv(string? value)
    {
        var text = value ?? string.Empty;
        if (text.Contains('"'))
        {
            text = text.Replace("\"", "\"\"");
        }

        return text.IndexOfAny([',', '"', '\r', '\n']) >= 0 ? $"\"{text}\"" : text;
    }

    private static string Encode(string? value) => WebUtility.HtmlEncode(value ?? string.Empty);

    private static string FormatNumber(double? value) => value?.ToString("0.00") ?? "-";
}

