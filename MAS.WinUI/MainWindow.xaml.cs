using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Windows;
using System.Windows.Controls;
using MAS.Application.Services;
using MAS.Core.Entities;
using MAS.Core.Enums;
using MAS.Infrastructure.Configuration;
using MAS.Infrastructure.Database;
using MAS.Infrastructure.Operations;
using MAS.Infrastructure.Repositories;

namespace MAS.WinUI;

public partial class MainWindow : Window
{
    private readonly SqliteScriptBootstrapper _bootstrapper = new();
    private readonly MeasurementTaskService _taskService = new();
    private readonly SqliteInstrumentRepository _instrumentRepository = new();
    private readonly SqliteCalibrationRecordRepository _calibrationRecordRepository = new();
    private readonly SqliteSampleRepository _sampleRepository = new();
    private readonly SqliteStandardSampleRepository _standardSampleRepository = new();
    private readonly SqliteToleranceTemplateRepository _templateRepository = new();
    private readonly SqliteMeasurementTaskRepository _taskRepository = new();
    private readonly SqliteMeasurementRecordRepository _measurementRecordRepository = new();
    private readonly SqliteMeasurementAngleResultRepository _angleResultRepository = new();
    private readonly SqliteMeasurementEffectResultRepository _effectResultRepository = new();
    private readonly SqliteReportExportRepository _reportExportRepository = new();
    private readonly SqliteOperationLogRepository _operationLogRepository = new();
    private readonly MeasurementWorkflowService _workflowService;
    private readonly SimulatedInstrumentConnectionService _instrumentConnectionService;
    private readonly MeasurementReportService _reportService;
    private readonly AppSettingsStore _appSettingsStore = new();
    private readonly DataMaintenanceService _dataMaintenanceService = new();
    private AppSettings _appSettings = new();
    private IReadOnlyList<MeasurementTask> _allTasks = Array.Empty<MeasurementTask>();
    private IReadOnlyList<Sample> _allSamples = Array.Empty<Sample>();
    private IReadOnlyList<StandardSample> _allStandardSamples = Array.Empty<StandardSample>();
    private IReadOnlyList<ToleranceTemplate> _allTemplates = Array.Empty<ToleranceTemplate>();
    private IReadOnlyList<MeasurementRecord> _allMeasurementRecords = Array.Empty<MeasurementRecord>();
    private IReadOnlyList<ReportExport> _allReportExports = Array.Empty<ReportExport>();
    private IReadOnlyList<OperationLog> _allOperationLogs = Array.Empty<OperationLog>();

    public MainWindow()
    {
        _instrumentConnectionService = new SimulatedInstrumentConnectionService(
            _instrumentRepository,
            _calibrationRecordRepository);

        _workflowService = new MeasurementWorkflowService(
            _taskRepository,
            _measurementRecordRepository,
            _angleResultRepository,
            _effectResultRepository,
            new SimulatedInstrumentMeasurementService());

        _reportService = new MeasurementReportService(
            _measurementRecordRepository,
            _taskRepository,
            _angleResultRepository,
            _effectResultRepository,
            _sampleRepository,
            _standardSampleRepository,
            _templateRepository,
            _reportExportRepository,
            _operationLogRepository);

        InitializeComponent();
        DatabasePathText.Text = _bootstrapper.DatabasePath;
        SettingsPathTextBlock.Text = _appSettingsStore.SettingsPath;
        AppendLog("应用已启动。准备加载数据库状态。");
    }

    private async void MainWindow_OnLoaded(object sender, RoutedEventArgs e)
    {
        await EnsureDatabaseReadyAsync();
        await LoadSettingsAsync();
        await RefreshAllDataAsync();
    }

    private async void InitializeDatabaseButton_OnClick(object sender, RoutedEventArgs e)
    {
        await EnsureDatabaseReadyAsync(forceLog: true);
        await RefreshAllDataAsync();
    }

    private async void RefreshDataButton_OnClick(object sender, RoutedEventArgs e)
    {
        await RefreshAllDataAsync();
        AppendLog("主数据、报告与日志已刷新。");
    }

    private async void SaveSettingsButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (!TryReadSettingsFromInputs(out var settings))
        {
            MessageBox.Show(this, "系统设置中的数值项格式不正确。", "系统设置", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        _appSettings = settings;
        await _appSettingsStore.SaveAsync(_appSettings);
        ApplySettingsToInputs();
        UpdateSettingsSummary();
        AppendLog("系统设置已保存。");
    }

    private async void ReloadSettingsButton_OnClick(object sender, RoutedEventArgs e)
    {
        await LoadSettingsAsync();
        AppendLog("系统设置已重新加载。");
    }

    private async void BackupDataButton_OnClick(object sender, RoutedEventArgs e)
    {
        await EnsureDatabaseReadyAsync();
        try
        {
            var backup = await _dataMaintenanceService.BackupAsync(_bootstrapper.DatabasePath, _appSettingsStore.SettingsPath);
            UpdateSettingsSummary();
            AppendLog($"数据备份已生成: {backup.DatabaseBackupPath}");
            MessageBox.Show(this, $"数据备份完成。\n{backup.DatabaseBackupPath}", "系统设置", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            AppendLog($"数据备份失败: {ex.Message}");
            MessageBox.Show(this, ex.Message, "系统设置", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async void RestoreLatestBackupButton_OnClick(object sender, RoutedEventArgs e)
    {
        await EnsureDatabaseReadyAsync();
        if (MessageBox.Show(this, "恢复最近备份会覆盖当前数据库和设置文件，是否继续？", "系统设置", MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes)
        {
            return;
        }

        try
        {
            var restore = await _dataMaintenanceService.RestoreLatestBackupAsync(_bootstrapper.DatabasePath, _appSettingsStore.SettingsPath);
            await LoadSettingsAsync();
            await RefreshAllDataAsync();
            AppendLog($"已恢复最近备份，恢复前安全备份: {restore.SafetyBackupPath}");
            MessageBox.Show(this, $"恢复完成。\n数据库: {restore.RestoredDatabasePath}\n恢复前安全备份: {restore.SafetyBackupPath}", "系统设置", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            AppendLog($"恢复最近备份失败: {ex.Message}");
            MessageBox.Show(this, ex.Message, "系统设置", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async void ExportDiagnosticsButton_OnClick(object sender, RoutedEventArgs e)
    {
        await EnsureDatabaseReadyAsync();
        try
        {
            var summary = new Dictionary<string, string>
            {
                ["GeneratedAt"] = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                ["DatabasePath"] = _bootstrapper.DatabasePath,
                ["SettingsPath"] = _appSettingsStore.SettingsPath,
                ["TaskCount"] = _allTasks.Count.ToString(CultureInfo.InvariantCulture),
                ["RecordCount"] = _allMeasurementRecords.Count.ToString(CultureInfo.InvariantCulture),
                ["ReportCount"] = _allReportExports.Count.ToString(CultureInfo.InvariantCulture),
                ["LogCount"] = _allOperationLogs.Count.ToString(CultureInfo.InvariantCulture),
            };

            var diagnostics = await _dataMaintenanceService.ExportDiagnosticsAsync(
                _bootstrapper.DatabasePath,
                _appSettingsStore.SettingsPath,
                LogTextBox.Text,
                summary);

            UpdateSettingsSummary();
            AppendLog($"诊断包已导出: {diagnostics.PackagePath}");
            MessageBox.Show(this, $"诊断包导出完成。\n{diagnostics.PackagePath}", "系统设置", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            AppendLog($"诊断包导出失败: {ex.Message}");
            MessageBox.Show(this, ex.Message, "系统设置", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void OpenBackupFolderButton_OnClick(object sender, RoutedEventArgs e)
    {
        try
        {
            Directory.CreateDirectory(_dataMaintenanceService.BackupFolderPath);
            Process.Start(new ProcessStartInfo { FileName = _dataMaintenanceService.BackupFolderPath, UseShellExecute = true });
            AppendLog($"已打开备份目录: {_dataMaintenanceService.BackupFolderPath}");
        }
        catch (Exception ex)
        {
            AppendLog($"打开备份目录失败: {ex.Message}");
            MessageBox.Show(this, ex.Message, "系统设置", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void OpenDataFolderButton_OnClick(object sender, RoutedEventArgs e)
    {
        try
        {
            Directory.CreateDirectory(_dataMaintenanceService.DataFolderPath);
            Process.Start(new ProcessStartInfo { FileName = _dataMaintenanceService.DataFolderPath, UseShellExecute = true });
            AppendLog($"已打开数据目录: {_dataMaintenanceService.DataFolderPath}");
        }
        catch (Exception ex)
        {
            AppendLog($"打开数据目录失败: {ex.Message}");
            MessageBox.Show(this, ex.Message, "系统设置", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async void ConnectInstrumentButton_OnClick(object sender, RoutedEventArgs e)
    {
        await ChangeInstrumentConnectionAsync(true);
    }

    private async void DisconnectInstrumentButton_OnClick(object sender, RoutedEventArgs e)
    {
        await ChangeInstrumentConnectionAsync(false);
    }

    private async void WhiteCalibrationButton_OnClick(object sender, RoutedEventArgs e)
    {
        await CalibrateInstrumentAsync("white");
    }

    private async void BlackCalibrationButton_OnClick(object sender, RoutedEventArgs e)
    {
        await CalibrateInstrumentAsync("black");
    }

    private async void InstrumentGrid_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (InstrumentGrid.SelectedItem is Instrument instrument)
        {
            SelectInstrument(instrument);
            CalibrationRecordGrid.ItemsSource = await _calibrationRecordRepository.GetByInstrumentIdAsync(instrument.Id);
        }
    }

    private void ClearSampleInputsButton_OnClick(object sender, RoutedEventArgs e)
    {
        ClearSampleInputs();
        ClearSampleSummary();
    }

    private void ClearStandardInputsButton_OnClick(object sender, RoutedEventArgs e)
    {
        ClearStandardInputs();
        ClearStandardSummary();
    }

    private void ClearTemplateInputsButton_OnClick(object sender, RoutedEventArgs e)
    {
        ClearTemplateInputs();
        ClearTemplateSummary();
    }

    private async void SampleGrid_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (SampleGrid.SelectedItem is Sample sample)
        {
            ApplySampleToInputs(sample);
            await LoadSampleSummaryAsync(sample);
        }
    }

    private async void StandardSampleGrid_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (StandardSampleGrid.SelectedItem is StandardSample standardSample)
        {
            ApplyStandardToInputs(standardSample);
            await LoadStandardSummaryAsync(standardSample);
        }
    }

    private async void TemplateGrid_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (TemplateGrid.SelectedItem is ToleranceTemplate template)
        {
            ApplyTemplateToInputs(template);
            await LoadTemplateSummaryAsync(template);
        }
    }

    private async void SaveSampleButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(SampleCodeTextBox.Text) || string.IsNullOrWhiteSpace(SampleNameTextBox.Text))
        {
            MessageBox.Show(this, "样品编号和样品名称不能为空。", "保存试样", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        await EnsureDatabaseReadyAsync();

        if (await _sampleRepository.GetByCodeAsync(SampleCodeTextBox.Text.Trim()) is not null)
        {
            MessageBox.Show(this, "样品编号已存在，请更换。", "保存试样", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var sample = new Sample
        {
            SampleCode = SampleCodeTextBox.Text.Trim(),
            SampleName = SampleNameTextBox.Text.Trim(),
            BatchNo = NullIfWhiteSpace(SampleBatchTextBox.Text),
            MaterialName = NullIfWhiteSpace(SampleMaterialTextBox.Text),
            ColorName = NullIfWhiteSpace(SampleColorTextBox.Text),
            Status = "active",
        };

        await _sampleRepository.AddAsync(sample);
        await WriteOperationLogAsync("sample", "create", "success", $"新增试样 {sample.SampleCode}");
        AppendLog($"已保存试样: {sample.SampleCode}");
        ClearSampleInputs();
        await RefreshAllDataAsync();
    }

    private async void SaveTemplateButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(TemplateCodeTextBox.Text) || string.IsNullOrWhiteSpace(TemplateNameTextBox.Text))
        {
            MessageBox.Show(this, "模板编号和模板名称不能为空。", "保存模板", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        await EnsureDatabaseReadyAsync();

        if (await _templateRepository.GetByCodeAsync(TemplateCodeTextBox.Text.Trim()) is not null)
        {
            MessageBox.Show(this, "模板编号已存在，请更换。", "保存模板", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        if (!TryParseRequiredDouble(TemplateOverallLimitTextBox.Text, out var overallLimit))
        {
            MessageBox.Show(this, "综合色差上限格式不正确。", "保存模板", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (!TryParseRequiredDouble(TemplateEffectLimitTextBox.Text, out var effectLimit))
        {
            MessageBox.Show(this, "效果差上限格式不正确。", "保存模板", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var template = new ToleranceTemplate
        {
            TemplateCode = TemplateCodeTextBox.Text.Trim(),
            TemplateName = TemplateNameTextBox.Text.Trim(),
            DeltaEFormula = string.IsNullOrWhiteSpace(TemplateFormulaTextBox.Text) ? "DE00" : TemplateFormulaTextBox.Text.Trim(),
            OverallUpperLimit = overallLimit,
            EffectUpperLimit = effectLimit,
            IsDefault = false,
            Status = "active",
        };

        await _templateRepository.AddAsync(template);
        await WriteOperationLogAsync("template", "create", "success", $"新增容差模板 {template.TemplateCode}");
        AppendLog($"已保存模板: {template.TemplateCode}");
        ClearTemplateInputs();
        await RefreshAllDataAsync();
    }

    private async void SaveStandardSampleButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(StandardCodeTextBox.Text) || string.IsNullOrWhiteSpace(StandardNameTextBox.Text))
        {
            MessageBox.Show(this, "标准样编号和名称不能为空。", "保存标准样", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        await EnsureDatabaseReadyAsync();

        if (await _standardSampleRepository.GetByCodeAsync(StandardCodeTextBox.Text.Trim()) is not null)
        {
            MessageBox.Show(this, "标准样编号已存在，请更换。", "保存标准样", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var templateCode = string.IsNullOrWhiteSpace(StandardTemplateCodeTextBox.Text) ? "TPL-DEFAULT" : StandardTemplateCodeTextBox.Text.Trim();
        var template = await _templateRepository.GetByCodeAsync(templateCode);
        if (template is null)
        {
            MessageBox.Show(this, "模板编号不存在，请先创建模板。", "保存标准样", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var standardSample = new StandardSample
        {
            LibraryId = "library-default",
            StandardCode = StandardCodeTextBox.Text.Trim(),
            StandardName = StandardNameTextBox.Text.Trim(),
            VersionNo = 1,
            MaterialName = NullIfWhiteSpace(StandardMaterialTextBox.Text),
            ColorName = NullIfWhiteSpace(StandardColorTextBox.Text),
            ToleranceTemplateId = template.Id,
            IsActive = true,
            IsDefaultVersion = true,
        };

        await _standardSampleRepository.AddAsync(standardSample);
        await WriteOperationLogAsync("standard_sample", "create", "success", $"新增标准样 {standardSample.StandardCode}");
        AppendLog($"已保存标准样: {standardSample.StandardCode}");
        ClearStandardInputs();
        await RefreshAllDataAsync();
    }

    private async void ApplyTaskFilterButton_OnClick(object sender, RoutedEventArgs e)
    {
        await ApplyTaskFiltersAsync();
    }

    private async void ClearTaskFilterButton_OnClick(object sender, RoutedEventArgs e)
    {
        TaskFilterCodeTextBox.Clear();
        TaskFilterStatusComboBox.SelectedIndex = 0;
        await ApplyTaskFiltersAsync();
    }

    private async void CreateDraftTaskButton_OnClick(object sender, RoutedEventArgs e)
    {
        await EnsureDatabaseReadyAsync();

        var sample = SampleGrid.SelectedItem as Sample ?? (await _sampleRepository.GetAllAsync()).LastOrDefault();
        var standard = StandardSampleGrid.SelectedItem as StandardSample ?? (await _standardSampleRepository.GetAllAsync()).LastOrDefault();
        var template = TemplateGrid.SelectedItem as ToleranceTemplate ?? (await _templateRepository.GetAllAsync()).LastOrDefault();

        if (sample is null || standard is null || template is null)
        {
            MessageBox.Show(this, "请至少先保存一条试样、标准样和模板数据。", "生成任务草稿", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var task = _taskService.CreateDraftTask(
            instrumentId: "instrument-default",
            sampleId: sample.Id,
            standardSampleId: standard.Id,
            templateId: template.Id);

        await _taskRepository.AddAsync(task);
        await WriteOperationLogAsync("task", "create", "success", $"创建任务草稿 {task.TaskCode}", task.Id);
        await SelectTaskAsync(task);
        AppendLog($"已创建任务草稿: {task.TaskCode}");
        await RefreshAllDataAsync();
    }

    private async void ExecuteStandardMeasurementButton_OnClick(object sender, RoutedEventArgs e)
    {
        await ExecuteWorkflowMeasurementAsync("standard", "标准样");
    }

    private async void ExecuteTrialMeasurementButton_OnClick(object sender, RoutedEventArgs e)
    {
        await ExecuteWorkflowMeasurementAsync("trial", "试样");
    }

    private async void SaveMeasurementRecordButton_OnClick(object sender, RoutedEventArgs e)
    {
        await EnsureDatabaseReadyAsync();

        var taskCode = await ResolveTaskCodeAsync();
        if (taskCode is null)
        {
            MessageBox.Show(this, "请先填写或选择任务编号。", "保存测量记录", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var task = await _taskRepository.GetByCodeAsync(taskCode);
        if (task is null)
        {
            MessageBox.Show(this, "任务编号不存在，请先创建任务草稿。", "保存测量记录", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (!int.TryParse(RecordNoTextBox.Text, out var recordNo) || recordNo <= 0)
        {
            var existing = await _measurementRecordRepository.GetByTaskIdAsync(task.Id);
            recordNo = existing.Count + 1;
        }

        if (!TryParseOptionalDouble(RecordTotalDeltaETextBox.Text, out var totalDeltaE) ||
            !TryParseOptionalDouble(RecordTotalEffectDiffTextBox.Text, out var totalEffectDiff) ||
            !TryParseOptionalDouble(RecordCieLTextBox.Text, out var cieL) ||
            !TryParseOptionalDouble(RecordCieATextBox.Text, out var cieA) ||
            !TryParseOptionalDouble(RecordCieBTextBox.Text, out var cieB) ||
            !TryParseOptionalDouble(RecordAngleDeltaETextBox.Text, out var angleDeltaE) ||
            !TryParseOptionalDouble(RecordSparkleValueTextBox.Text, out var sparkleValue) ||
            !TryParseOptionalDouble(RecordSparkleDiffTextBox.Text, out var sparkleDiff) ||
            !TryParseOptionalDouble(RecordGraininessValueTextBox.Text, out var graininessValue) ||
            !TryParseOptionalDouble(RecordGraininessDiffTextBox.Text, out var graininessDiff))
        {
            MessageBox.Show(this, "数值字段格式不正确，请检查输入。", "保存测量记录", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var passStatus = GetSelectedPassStatus();
        var angleCode = string.IsNullOrWhiteSpace(RecordAngleCodeTextBox.Text) ? "45as110" : RecordAngleCodeTextBox.Text.Trim();
        var record = new MeasurementRecord
        {
            TaskId = task.Id,
            RecordNo = recordNo,
            RecordType = string.IsNullOrWhiteSpace(RecordTypeTextBox.Text) ? "trial" : RecordTypeTextBox.Text.Trim(),
            TotalDeltaE = totalDeltaE,
            TotalEffectDiff = totalEffectDiff,
            PassStatus = passStatus,
            ResultSummary = NullIfWhiteSpace(RecordSummaryTextBox.Text),
            MeasuredAt = DateTime.UtcNow,
        };

        await _measurementRecordRepository.AddAsync(record);
        await _angleResultRepository.AddRangeAsync(new[]
        {
            new MeasurementAngleResult
            {
                RecordId = record.Id,
                AngleCode = angleCode,
                CieL = cieL,
                CieA = cieA,
                CieB = cieB,
                DeltaE = angleDeltaE,
                PassStatus = passStatus,
            },
        });
        await _effectResultRepository.AddRangeAsync(new[]
        {
            new MeasurementEffectResult
            {
                RecordId = record.Id,
                AngleCode = angleCode,
                SparkleValue = sparkleValue,
                SparkleDiff = sparkleDiff,
                GraininessValue = graininessValue,
                GraininessDiff = graininessDiff,
                EffectPassStatus = passStatus,
            },
        });

        await WriteOperationLogAsync("measurement", "manual_save", "success", $"手工保存测量记录 {record.RecordNo}", task.Id, record.Id);
        AppendLog($"已保存手工记录: Task={taskCode}, RecordNo={record.RecordNo}");
        await RefreshAllDataAsync(record.Id);
    }

    private void ApplyRecordFilterButton_OnClick(object sender, RoutedEventArgs e)
    {
        ApplyRecordFilters();
    }

    private void ClearRecordFilterButton_OnClick(object sender, RoutedEventArgs e)
    {
        RecordFilterTaskCodeTextBox.Clear();
        RecordFilterTypeComboBox.SelectedIndex = 0;
        ApplyRecordFilters();
    }

    private void ApplyReportFilterButton_OnClick(object sender, RoutedEventArgs e)
    {
        ApplyReportFilters();
    }

    private void ClearReportFilterButton_OnClick(object sender, RoutedEventArgs e)
    {
        ReportFilterTextBox.Clear();
        OperationLogFilterTextBox.Clear();
        ApplyReportFilters();
    }

    private async void ExportReportButton_OnClick(object sender, RoutedEventArgs e)
    {
        await EnsureDatabaseReadyAsync();

        var record = MeasurementRecordGrid.SelectedItem as MeasurementRecord ?? (await _measurementRecordRepository.GetAllAsync()).FirstOrDefault();
        if (record is null)
        {
            MessageBox.Show(this, "当前没有可导出的测量记录。", "导出报告", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        try
        {
            var format = GetSelectedReportFormat();
            var result = await _reportService.ExportRecordReportAsync(record.Id, format);
            AppendLog($"报告已导出: {result.ReportCode} / {result.FileFormat.ToUpperInvariant()}");
            await RefreshAllDataAsync(record.Id);
        }
        catch (Exception ex)
        {
            AppendLog($"导出报告失败: {ex.Message}");
            await WriteOperationLogAsync("report", "export", "failed", ex.Message, recordId: record.Id);
            MessageBox.Show(this, ex.Message, "导出报告", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void OpenSelectedReportButton_OnClick(object sender, RoutedEventArgs e)
    {
        try
        {
            if (ReportExportGrid.SelectedItem is not ReportExport export || string.IsNullOrWhiteSpace(export.FilePath) || !File.Exists(export.FilePath))
            {
                MessageBox.Show(this, "请先选择一条已导出的有效报告。", "打开报告", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            Process.Start(new ProcessStartInfo { FileName = export.FilePath, UseShellExecute = true });
            AppendLog($"已打开报告文件: {export.ReportCode}");
        }
        catch (Exception ex)
        {
            AppendLog($"打开报告文件失败: {ex.Message}");
        }
    }

    private void ReportExportGrid_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        LoadReportPreview(ReportExportGrid.SelectedItem as ReportExport);
    }

    private void OpenReportFolderButton_OnClick(object sender, RoutedEventArgs e)
    {
        try
        {
            var folder = GetReportFolderPath();
            Directory.CreateDirectory(folder);
            Process.Start(new ProcessStartInfo { FileName = folder, UseShellExecute = true });
            AppendLog($"已打开报告目录: {folder}");
        }
        catch (Exception ex)
        {
            AppendLog($"打开报告目录失败: {ex.Message}");
        }
    }

    private async void TaskGrid_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (TaskGrid.SelectedItem is MeasurementTask task)
        {
            await SelectTaskAsync(task);
            var records = await _measurementRecordRepository.GetByTaskIdAsync(task.Id);
            MeasurementRecordGrid.ItemsSource = records;
            var latestRecord = records.FirstOrDefault();
            MeasurementRecordGrid.SelectedItem = latestRecord;
            if (latestRecord is not null)
            {
                await LoadMeasurementDetailsAsync(latestRecord.Id);
            }
        }
    }

    private async void MeasurementRecordGrid_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (MeasurementRecordGrid.SelectedItem is MeasurementRecord record)
        {
            await LoadMeasurementDetailsAsync(record.Id);
        }
    }

    private void OpenSchemaButton_OnClick(object sender, RoutedEventArgs e)
    {
        try
        {
            var folder = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "MAS.Infrastructure", "Database", "Schema");
            var fullPath = Path.GetFullPath(folder);
            Process.Start(new ProcessStartInfo { FileName = fullPath, UseShellExecute = true });
            AppendLog($"已打开建表脚本目录: {fullPath}");
        }
        catch (Exception ex)
        {
            AppendLog($"打开建表脚本目录失败: {ex.Message}");
        }
    }

    private async Task ChangeInstrumentConnectionAsync(bool connect)
    {
        await EnsureDatabaseReadyAsync();
        var instrument = InstrumentGrid.SelectedItem as Instrument ?? await _instrumentRepository.GetDefaultAsync();
        if (instrument is null)
        {
            MessageBox.Show(this, "未找到可用仪器。", "仪器连接", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var updated = connect
            ? await _instrumentConnectionService.ConnectAsync(instrument.Id)
            : await _instrumentConnectionService.DisconnectAsync(instrument.Id);
        await WriteOperationLogAsync("instrument", connect ? "connect" : "disconnect", "success", $"仪器{(connect ? "连接" : "断开")} {updated.InstrumentCode}");
        SelectInstrument(updated);
        AppendLog(connect ? $"仪器已连接: {updated.InstrumentCode}" : $"仪器已断开: {updated.InstrumentCode}");
        await RefreshAllDataAsync();
    }

    private async Task CalibrateInstrumentAsync(string calibrationType)
    {
        await EnsureDatabaseReadyAsync();
        var instrument = InstrumentGrid.SelectedItem as Instrument ?? await _instrumentRepository.GetDefaultAsync();
        if (instrument is null)
        {
            MessageBox.Show(this, "未找到可用仪器。", "仪器校准", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var record = await _instrumentConnectionService.CalibrateAsync(instrument.Id, calibrationType);
        await WriteOperationLogAsync("instrument", "calibration", "success", $"{(calibrationType == "white" ? "白板" : "黑腔")}校准完成", recordId: record.Id);
        AppendLog($"{(calibrationType == "white" ? "白板" : "黑腔")}校准完成: {instrument.InstrumentCode}");
        await RefreshAllDataAsync();
        CalibrationRecordGrid.ItemsSource = await _calibrationRecordRepository.GetByInstrumentIdAsync(record.InstrumentId);
    }

    private async Task ExecuteWorkflowMeasurementAsync(string recordType, string displayName)
    {
        await EnsureDatabaseReadyAsync();

        var taskCode = await ResolveTaskCodeAsync();
        if (taskCode is null)
        {
            MessageBox.Show(this, "请先创建或选择任务草稿。", $"执行{displayName}测量", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        try
        {
            var result = await _workflowService.ExecuteMeasurementAsync(taskCode, recordType);
            await WriteOperationLogAsync("measurement", recordType, "success", $"{displayName}测量完成", result.Task.Id, result.Record.Id);
            await SelectTaskAsync(result.Task);
            ApplyRecordToInputs(result.Record, result.AngleResults.FirstOrDefault(), result.EffectResults.FirstOrDefault());
            AppendLog($"{displayName}测量完成: {result.Task.TaskCode} / 记录 {result.Record.RecordNo}");
            await RefreshAllDataAsync(result.Record.Id);
        }
        catch (Exception ex)
        {
            AppendLog($"{displayName}测量失败: {ex.Message}");
            await WriteOperationLogAsync("measurement", recordType, "failed", ex.Message);
            MessageBox.Show(this, ex.Message, $"执行{displayName}测量", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async Task<string?> ResolveTaskCodeAsync()
    {
        var taskCode = string.IsNullOrWhiteSpace(RecordTaskCodeTextBox.Text)
            ? (TaskGrid.SelectedItem as MeasurementTask)?.TaskCode
            : RecordTaskCodeTextBox.Text.Trim();

        if (!string.IsNullOrWhiteSpace(taskCode))
        {
            return taskCode;
        }

        var latestTask = (await _taskRepository.GetAllAsync()).LastOrDefault();
        return latestTask?.TaskCode;
    }

    private async Task LoadSettingsAsync()
    {
        _appSettings = await _appSettingsStore.LoadAsync();
        ApplySettingsToInputs();
        UpdateSettingsSummary();
    }

    private void ApplySettingsToInputs()
    {
        SelectComboItemByText(SettingsTaskTypeComboBox, _appSettings.DefaultTaskType);
        SelectComboItemByText(SettingsMeasurementModeComboBox, _appSettings.DefaultMeasurementMode);
        SettingsAverageCountTextBox.Text = _appSettings.DefaultAverageCount.ToString(CultureInfo.InvariantCulture);
        SettingsIntervalSecondsTextBox.Text = _appSettings.DefaultIntervalSeconds.ToString(CultureInfo.InvariantCulture);
        SettingsTemplateCodeTextBox.Text = _appSettings.DefaultTemplateCode;
        SettingsPreviewLengthTextBox.Text = _appSettings.ReportPreviewMaxLength.ToString(CultureInfo.InvariantCulture);

        if (string.IsNullOrWhiteSpace(StandardTemplateCodeTextBox.Text) || StandardTemplateCodeTextBox.Text == "TPL-DEFAULT")
        {
            StandardTemplateCodeTextBox.Text = _appSettings.DefaultTemplateCode;
        }

        if (string.IsNullOrWhiteSpace(RecordTypeTextBox.Text) || RecordTypeTextBox.Text == "trial" || RecordTypeTextBox.Text == "standard")
        {
            RecordTypeTextBox.Text = _appSettings.DefaultTaskType;
        }
    }

    private bool TryReadSettingsFromInputs(out AppSettings settings)
    {
        settings = new AppSettings();

        if (!int.TryParse(SettingsAverageCountTextBox.Text, out var averageCount) || averageCount <= 0)
        {
            return false;
        }

        if (!int.TryParse(SettingsIntervalSecondsTextBox.Text, out var intervalSeconds) || intervalSeconds <= 0)
        {
            return false;
        }

        if (!int.TryParse(SettingsPreviewLengthTextBox.Text, out var previewLength) || previewLength < 500)
        {
            return false;
        }

        settings.DefaultTaskType = (SettingsTaskTypeComboBox.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "trial";
        settings.DefaultMeasurementMode = (SettingsMeasurementModeComboBox.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Single";
        settings.DefaultAverageCount = averageCount;
        settings.DefaultIntervalSeconds = intervalSeconds;
        settings.DefaultTemplateCode = string.IsNullOrWhiteSpace(SettingsTemplateCodeTextBox.Text) ? "TPL-DEFAULT" : SettingsTemplateCodeTextBox.Text.Trim();
        settings.ReportPreviewMaxLength = previewLength;
        return true;
    }

    private void UpdateSettingsSummary()
    {
        SettingsPathTextBlock.Text = _appSettingsStore.SettingsPath;
        SettingsUpdatedAtTextBlock.Text = $"最后更新时间: {_appSettings.UpdatedAt:yyyy-MM-dd HH:mm:ss}";
        SettingsSummaryTextBlock.Text = $"默认任务类型 {_appSettings.DefaultTaskType}，默认测量模式 {_appSettings.DefaultMeasurementMode}，平均次数 {_appSettings.DefaultAverageCount}，间隔 {_appSettings.DefaultIntervalSeconds}s。";
        SettingsImpactTextBlock.Text = $"默认模板 {_appSettings.DefaultTemplateCode}，报告预览最大字符数 {_appSettings.ReportPreviewMaxLength}。新建任务、标准样模板默认值和报告预览会使用这些设置。";
    }
    private async Task EnsureDatabaseReadyAsync(bool forceLog = false)
    {
        await _bootstrapper.EnsureCreatedAsync();
        await DefaultDataSeeder.EnsureSeedDataAsync();
        DatabaseStatusText.Text = "已初始化";
        DatabasePathText.Text = _bootstrapper.DatabasePath;
        if (forceLog)
        {
            AppendLog($"数据库初始化完成: {_bootstrapper.DatabasePath}");
        }
    }

    private async Task RefreshAllDataAsync(string? selectedRecordId = null)
    {
        var selectedReportId = (ReportExportGrid.SelectedItem as ReportExport)?.Id;

        var instruments = await _instrumentRepository.GetAllAsync();
        var calibrationRecords = await _calibrationRecordRepository.GetAllAsync();
        var samples = await _sampleRepository.GetAllAsync();
        var standards = await _standardSampleRepository.GetAllAsync();
        var templates = await _templateRepository.GetAllAsync();
        var tasks = await _taskRepository.GetAllAsync();
        var records = await _measurementRecordRepository.GetAllAsync();
        var reportExports = await _reportExportRepository.GetAllAsync();
        var operationLogs = await _operationLogRepository.GetRecentAsync(50);

        _allSamples = samples;
        _allStandardSamples = standards;
        _allTemplates = templates;
        _allTasks = tasks;
        _allMeasurementRecords = records;
        _allReportExports = reportExports;
        _allOperationLogs = operationLogs;

        InstrumentGrid.ItemsSource = instruments;
        SampleGrid.ItemsSource = _allSamples;
        StandardSampleGrid.ItemsSource = _allStandardSamples;
        TemplateGrid.ItemsSource = _allTemplates;
        ApplyRecordFilters();
        ApplyReportFilters();

        var selectedSample = _allSamples.FirstOrDefault();
        SampleGrid.SelectedItem = selectedSample;
        if (selectedSample is not null)
        {
            ApplySampleToInputs(selectedSample);
            await LoadSampleSummaryAsync(selectedSample);
        }
        else
        {
            ClearSampleSummary();
        }

        var selectedStandard = _allStandardSamples.FirstOrDefault();
        StandardSampleGrid.SelectedItem = selectedStandard;
        if (selectedStandard is not null)
        {
            ApplyStandardToInputs(selectedStandard);
            await LoadStandardSummaryAsync(selectedStandard);
        }
        else
        {
            ClearStandardSummary();
        }

        var selectedTemplate = _allTemplates.FirstOrDefault();
        TemplateGrid.SelectedItem = selectedTemplate;
        if (selectedTemplate is not null)
        {
            ApplyTemplateToInputs(selectedTemplate);
            await LoadTemplateSummaryAsync(selectedTemplate);
        }
        else
        {
            ClearTemplateSummary();
        }

        var selectedInstrument = instruments.FirstOrDefault();
        if (selectedInstrument is not null)
        {
            SelectInstrument(selectedInstrument);
            CalibrationRecordGrid.ItemsSource = calibrationRecords.Where(x => x.InstrumentId == selectedInstrument.Id).ToList();
        }

        var latestTask = tasks.LastOrDefault();
        await ApplyTaskFiltersAsync(latestTask?.Id);

        var selectedRecord = selectedRecordId is null
            ? records.FirstOrDefault()
            : records.FirstOrDefault(x => x.Id == selectedRecordId) ?? records.FirstOrDefault();

        MeasurementRecordGrid.SelectedItem = selectedRecord;
        if (selectedRecord is not null)
        {
            await LoadMeasurementDetailsAsync(selectedRecord.Id);
        }
        else
        {
            AngleResultGrid.ItemsSource = Array.Empty<MeasurementAngleResult>();
            EffectResultGrid.ItemsSource = Array.Empty<MeasurementEffectResult>();
            ClearMeasurementSummary();
        }

        var selectedReport = selectedReportId is null
            ? reportExports.OrderByDescending(x => x.ExportedAt).FirstOrDefault()
            : reportExports.FirstOrDefault(x => x.Id == selectedReportId) ?? reportExports.OrderByDescending(x => x.ExportedAt).FirstOrDefault();

        ReportExportGrid.SelectedItem = selectedReport;
        LoadReportPreview(selectedReport);
    }

    private async Task ApplyTaskFiltersAsync(string? preferredTaskId = null)
    {
        var codeFilter = TaskFilterCodeTextBox?.Text?.Trim();
        var statusFilter = (TaskFilterStatusComboBox?.SelectedItem as ComboBoxItem)?.Content?.ToString();

        var filtered = _allTasks.Where(task =>
        {
            var matchesCode = string.IsNullOrWhiteSpace(codeFilter) || task.TaskCode.Contains(codeFilter, StringComparison.OrdinalIgnoreCase);
            var matchesStatus = string.IsNullOrWhiteSpace(statusFilter) || statusFilter == "全部" || string.Equals(task.Status.ToString(), statusFilter, StringComparison.OrdinalIgnoreCase);
            return matchesCode && matchesStatus;
        }).ToList();

        TaskGrid.ItemsSource = filtered;

        MeasurementTask? nextTask = null;
        if (!string.IsNullOrWhiteSpace(preferredTaskId))
        {
            nextTask = filtered.FirstOrDefault(x => x.Id == preferredTaskId);
        }

        nextTask ??= (TaskGrid.SelectedItem as MeasurementTask) is MeasurementTask selected && filtered.Any(x => x.Id == selected.Id)
            ? selected
            : filtered.FirstOrDefault();

        if (nextTask is null)
        {
            TaskGrid.SelectedItem = null;
            ClearTaskSummary();
            return;
        }

        TaskGrid.SelectedItem = nextTask;
        await SelectTaskAsync(nextTask);
    }

    private void ApplyRecordFilters()
    {
        var taskCodeFilter = RecordFilterTaskCodeTextBox?.Text?.Trim();
        var recordTypeFilter = (RecordFilterTypeComboBox?.SelectedItem as ComboBoxItem)?.Content?.ToString();

        var filtered = _allMeasurementRecords.Where(record =>
        {
            var matchesTask = string.IsNullOrWhiteSpace(taskCodeFilter) || _allTasks.Any(task => task.Id == record.TaskId && task.TaskCode.Contains(taskCodeFilter, StringComparison.OrdinalIgnoreCase));
            var matchesType = string.IsNullOrWhiteSpace(recordTypeFilter) || recordTypeFilter == "全部" || string.Equals(record.RecordType, recordTypeFilter, StringComparison.OrdinalIgnoreCase);
            return matchesTask && matchesType;
        }).ToList();

        MeasurementRecordGrid.ItemsSource = filtered;
    }

    private void ApplyReportFilters()
    {
        var reportFilter = ReportFilterTextBox?.Text?.Trim();
        var moduleFilter = OperationLogFilterTextBox?.Text?.Trim();

        ReportExportGrid.ItemsSource = _allReportExports.Where(export =>
            string.IsNullOrWhiteSpace(reportFilter) || export.ReportCode.Contains(reportFilter, StringComparison.OrdinalIgnoreCase)).ToList();

        OperationLogGrid.ItemsSource = _allOperationLogs.Where(log =>
            string.IsNullOrWhiteSpace(moduleFilter) || log.ModuleName.Contains(moduleFilter, StringComparison.OrdinalIgnoreCase)).ToList();
    }

    private async Task LoadMeasurementDetailsAsync(string recordId)
    {
        var angleResults = await _angleResultRepository.GetByRecordIdAsync(recordId);
        var effectResults = await _effectResultRepository.GetByRecordIdAsync(recordId);

        AngleResultGrid.ItemsSource = angleResults;
        EffectResultGrid.ItemsSource = effectResults;

        var record = _allMeasurementRecords.FirstOrDefault(x => x.Id == recordId) ?? await _measurementRecordRepository.GetByIdAsync(recordId);
        if (record is not null)
        {
            await LoadMeasurementSummaryAsync(record, angleResults, effectResults);
        }
        else
        {
            ClearMeasurementSummary();
        }
    }

    private void LoadReportPreview(ReportExport? export)
    {
        if (export is null)
        {
            ClearReportPreview();
            return;
        }

        ReportMetaTextBlock.Text = $"报告编号: {export.ReportCode} | 格式: {export.FileFormat} | 状态: {export.ExportStatus} | 时间: {export.ExportedAt:yyyy-MM-dd HH:mm:ss}";
        ReportPathTextBlock.Text = string.IsNullOrWhiteSpace(export.FilePath) ? "-" : export.FilePath;

        if (string.IsNullOrWhiteSpace(export.FilePath) || !File.Exists(export.FilePath))
        {
            ReportPreviewTextBox.Text = "报告文件不存在，可能已被移动或删除。";
            return;
        }

        var content = File.ReadAllText(export.FilePath);
        var previewLength = Math.Max(500, _appSettings.ReportPreviewMaxLength);
        ReportPreviewTextBox.Text = content.Length <= previewLength ? content : content[..previewLength] + Environment.NewLine + Environment.NewLine + "...（预览已截断）";
    }

    private void ClearReportPreview()
    {
        ReportMetaTextBlock.Text = "请选择一条导出记录查看报告内容。";
        ReportPathTextBlock.Text = "-";
        ReportPreviewTextBox.Text = string.Empty;
    }

    private Task LoadSampleSummaryAsync(Sample sample)
    {
        var linkedTasks = _allTasks.Where(x => x.SampleId == sample.Id).ToList();
        var linkedRecords = _allMeasurementRecords.Where(record => linkedTasks.Any(task => task.Id == record.TaskId)).OrderByDescending(x => x.MeasuredAt).ToList();
        var latestRecord = linkedRecords.FirstOrDefault();

        SampleSummaryTextBlock.Text = $"试样 {sample.SampleCode} / {sample.SampleName}，材质 {(string.IsNullOrWhiteSpace(sample.MaterialName) ? "未填写" : sample.MaterialName)}，颜色 {(string.IsNullOrWhiteSpace(sample.ColorName) ? "未填写" : sample.ColorName)}，当前状态 {sample.Status}。";
        SampleUsageTextBlock.Text = $"关联任务 {linkedTasks.Count} 条，关联测量记录 {linkedRecords.Count} 条，最近测量 {(latestRecord is null ? "暂无" : $"{latestRecord.RecordType} / {latestRecord.PassStatus} / {latestRecord.MeasuredAt:yyyy-MM-dd HH:mm:ss}")}";
        return Task.CompletedTask;
    }

    private async Task LoadStandardSummaryAsync(StandardSample standardSample)
    {
        var linkedTemplate = string.IsNullOrWhiteSpace(standardSample.ToleranceTemplateId) ? null : await _templateRepository.GetByIdAsync(standardSample.ToleranceTemplateId);
        var linkedTasks = _allTasks.Where(x => x.StandardSampleId == standardSample.Id).ToList();
        var latestRecord = _allMeasurementRecords.Where(record => linkedTasks.Any(task => task.Id == record.TaskId)).OrderByDescending(x => x.MeasuredAt).FirstOrDefault();

        StandardSummaryTextBlock.Text = $"标准样 {standardSample.StandardCode} / {standardSample.StandardName}，版本 {standardSample.VersionNo}，材质 {(string.IsNullOrWhiteSpace(standardSample.MaterialName) ? "未填写" : standardSample.MaterialName)}，颜色 {(string.IsNullOrWhiteSpace(standardSample.ColorName) ? "未填写" : standardSample.ColorName)}。";
        StandardUsageTextBlock.Text = $"关联模板 {(linkedTemplate is null ? (standardSample.ToleranceTemplateId ?? "未绑定") : $"{linkedTemplate.TemplateCode} / {linkedTemplate.TemplateName}")}，任务引用 {linkedTasks.Count} 次，最近测量 {(latestRecord is null ? "暂无" : $"{latestRecord.RecordType} / {latestRecord.PassStatus} / ΔE {latestRecord.TotalDeltaE?.ToString("0.00", CultureInfo.InvariantCulture) ?? "-"}")}";
    }

    private Task LoadTemplateSummaryAsync(ToleranceTemplate template)
    {
        var linkedStandards = _allStandardSamples.Where(x => x.ToleranceTemplateId == template.Id).ToList();
        var linkedTasks = _allTasks.Where(x => x.TemplateId == template.Id).ToList();
        var latestRecord = _allMeasurementRecords.Where(record => linkedTasks.Any(task => task.Id == record.TaskId)).OrderByDescending(x => x.MeasuredAt).FirstOrDefault();

        TemplateSummaryTextBlock.Text = $"模板 {template.TemplateCode} / {template.TemplateName}，公式 {template.DeltaEFormula}，综合色差上限 {template.OverallUpperLimit?.ToString("0.00", CultureInfo.InvariantCulture) ?? "-"}，效果差上限 {template.EffectUpperLimit?.ToString("0.00", CultureInfo.InvariantCulture) ?? "-"}。";
        TemplateUsageTextBlock.Text = $"默认模板: {(template.IsDefault ? "是" : "否")}，关联标准样 {linkedStandards.Count} 条，关联任务 {linkedTasks.Count} 条，最近测量 {(latestRecord is null ? "暂无" : $"{latestRecord.RecordType} / {latestRecord.PassStatus} / {latestRecord.MeasuredAt:yyyy-MM-dd HH:mm:ss}")}";
        return Task.CompletedTask;
    }

    private void ApplySampleToInputs(Sample sample)
    {
        SampleCodeTextBox.Text = sample.SampleCode;
        SampleNameTextBox.Text = sample.SampleName;
        SampleBatchTextBox.Text = sample.BatchNo ?? string.Empty;
        SampleMaterialTextBox.Text = sample.MaterialName ?? string.Empty;
        SampleColorTextBox.Text = sample.ColorName ?? string.Empty;
    }

    private void ApplyStandardToInputs(StandardSample standardSample)
    {
        StandardCodeTextBox.Text = standardSample.StandardCode;
        StandardNameTextBox.Text = standardSample.StandardName;
        StandardMaterialTextBox.Text = standardSample.MaterialName ?? string.Empty;
        StandardColorTextBox.Text = standardSample.ColorName ?? string.Empty;
        StandardTemplateCodeTextBox.Text = _allTemplates.FirstOrDefault(x => x.Id == standardSample.ToleranceTemplateId)?.TemplateCode ?? string.Empty;
    }

    private void ApplyTemplateToInputs(ToleranceTemplate template)
    {
        TemplateCodeTextBox.Text = template.TemplateCode;
        TemplateNameTextBox.Text = template.TemplateName;
        TemplateFormulaTextBox.Text = template.DeltaEFormula;
        TemplateOverallLimitTextBox.Text = template.OverallUpperLimit?.ToString("0.00", CultureInfo.InvariantCulture) ?? string.Empty;
        TemplateEffectLimitTextBox.Text = template.EffectUpperLimit?.ToString("0.00", CultureInfo.InvariantCulture) ?? string.Empty;
    }

    private void ClearSampleSummary()
    {
        SampleSummaryTextBlock.Text = "请选择一条试样查看摘要。";
        SampleUsageTextBlock.Text = "-";
    }

    private void ClearStandardSummary()
    {
        StandardSummaryTextBlock.Text = "请选择一条标准样查看摘要。";
        StandardUsageTextBlock.Text = "-";
    }

    private void ClearTemplateSummary()
    {
        TemplateSummaryTextBlock.Text = "请选择一条模板查看摘要。";
        TemplateUsageTextBlock.Text = "-";
    }

    private async Task LoadTaskSummaryAsync(MeasurementTask task)
    {
        var instrument = await _instrumentRepository.GetByIdAsync(task.InstrumentId);
        var sample = string.IsNullOrWhiteSpace(task.SampleId) ? null : await _sampleRepository.GetByIdAsync(task.SampleId);
        var standard = string.IsNullOrWhiteSpace(task.StandardSampleId) ? null : await _standardSampleRepository.GetByIdAsync(task.StandardSampleId);
        var template = string.IsNullOrWhiteSpace(task.TemplateId) ? null : await _templateRepository.GetByIdAsync(task.TemplateId);
        var taskRecords = _allMeasurementRecords.Where(x => x.TaskId == task.Id).OrderByDescending(x => x.MeasuredAt).ToList();
        var latestRecord = taskRecords.FirstOrDefault();

        TaskContextTextBlock.Text = $"任务 {task.TaskCode} 当前状态为 {task.Status}，绑定仪器 {(instrument?.InstrumentName ?? task.InstrumentId)}，累计记录 {taskRecords.Count} 条。";
        TaskBindingTextBlock.Text = $"试样: {(sample is null ? (task.SampleId ?? "未绑定") : $"{sample.SampleCode} / {sample.SampleName}")} | 标准样: {(standard is null ? (task.StandardSampleId ?? "未绑定") : $"{standard.StandardCode} / {standard.StandardName}")} | 模板: {(template is null ? (task.TemplateId ?? "未绑定") : $"{template.TemplateCode} / {template.TemplateName}")} | 最近测量: {(latestRecord is null ? "暂无" : $"{latestRecord.RecordType} / {latestRecord.PassStatus} / ΔE {latestRecord.TotalDeltaE?.ToString("0.00", CultureInfo.InvariantCulture) ?? "-"}")}";
    }

    private async Task LoadMeasurementSummaryAsync(MeasurementRecord record, IReadOnlyList<MeasurementAngleResult> angleResults, IReadOnlyList<MeasurementEffectResult> effectResults)
    {
        var task = _allTasks.FirstOrDefault(x => x.Id == record.TaskId) ?? await _taskRepository.GetByIdAsync(record.TaskId);
        var maxAngleDelta = angleResults.Where(x => x.DeltaE.HasValue).Select(x => x.DeltaE!.Value).DefaultIfEmpty().Max();
        var avgAngleDelta = angleResults.Where(x => x.DeltaE.HasValue).Select(x => x.DeltaE!.Value).DefaultIfEmpty().Average();
        var maxSparkleDiff = effectResults.Where(x => x.SparkleDiff.HasValue).Select(x => x.SparkleDiff!.Value).DefaultIfEmpty().Max();
        var maxGraininessDiff = effectResults.Where(x => x.GraininessDiff.HasValue).Select(x => x.GraininessDiff!.Value).DefaultIfEmpty().Max();

        MeasurementOverviewTextBlock.Text = $"记录 #{record.RecordNo} / {record.RecordType} / 判定 {record.PassStatus} / 所属任务 {(task?.TaskCode ?? record.TaskId)} / 总 ΔE {record.TotalDeltaE?.ToString("0.00", CultureInfo.InvariantCulture) ?? "-"} / 效果差 {record.TotalEffectDiff?.ToString("0.00", CultureInfo.InvariantCulture) ?? "-"}";
        MeasurementDetailTextBlock.Text = $"测量时间: {record.MeasuredAt:yyyy-MM-dd HH:mm:ss} | 角度结果: {angleResults.Count} 条，最大 ΔE {maxAngleDelta:0.00}，平均 ΔE {avgAngleDelta:0.00} | 效果结果: {effectResults.Count} 条，最大 Sparkle 差 {maxSparkleDiff:0.00}，最大 Graininess 差 {maxGraininessDiff:0.00} | 摘要: {(string.IsNullOrWhiteSpace(record.ResultSummary) ? "-" : record.ResultSummary)}";
    }

    private void UpdateDashboardSummary(
        IReadOnlyList<Instrument> instruments,
        IReadOnlyList<CalibrationRecord> calibrationRecords,
        IReadOnlyList<MeasurementTask> tasks,
        IReadOnlyList<MeasurementRecord> records,
        IReadOnlyList<ReportExport> reportExports,
        IReadOnlyList<OperationLog> operationLogs)
    {
        var defaultInstrument = instruments.FirstOrDefault();
        DashboardInstrumentStatusText.Text = defaultInstrument?.Status switch
        {
            "connected" => "已连接",
            "calibrated" => "已校准",
            _ => "未连接",
        };

        var latestCalibration = calibrationRecords.OrderByDescending(x => x.FinishedAt ?? x.StartedAt).FirstOrDefault();
        DashboardInstrumentDetailText.Text = defaultInstrument is null
            ? "未找到默认仪器。"
            : $"{defaultInstrument.InstrumentName} / 端口 {(string.IsNullOrWhiteSpace(defaultInstrument.PortName) ? "-" : defaultInstrument.PortName)} / 最近校准 {(latestCalibration is null ? "暂无" : $"{latestCalibration.CalibrationType}:{latestCalibration.ResultCode}")}";

        var completedTasks = tasks.Count(x => x.Status == MAS.Core.Enums.TaskStatus.Completed);
        DashboardTaskCountText.Text = $"{tasks.Count} / {reportExports.Count}";
        var latestReport = reportExports.OrderByDescending(x => x.ExportedAt).FirstOrDefault();
        DashboardTaskReportDetailText.Text = $"完成任务 {completedTasks} 条，测量记录 {records.Count} 条，最近报告 {(latestReport is null ? "暂无" : latestReport.ReportCode)}";

        var latestLog = operationLogs.OrderByDescending(x => x.CreatedAt).FirstOrDefault();
        DashboardRecentActivityText.Text = latestLog is null
            ? "当前暂无最新活动。"
            : $"最近活动: [{latestLog.ModuleName}] {latestLog.OperationType} / {latestLog.OperationResult} / {latestLog.CreatedAt:yyyy-MM-dd HH:mm:ss}";
    }

    private void ClearTaskSummary()
    {
        TaskSummaryTextBlock.Text = "请选择任务查看概览。";
        TaskMetricTextBlock.Text = "-";
        TaskRecentActivityTextBlock.Text = "暂无最近活动。";
        TaskRecentReportTextBlock.Text = "-";
        TaskContextTextBlock.Text = "请选择任务查看上下文。";
        TaskBindingTextBlock.Text = "-";
    }

    private void ClearMeasurementSummary()
    {
        MeasurementOverviewTextBlock.Text = "请选择记录查看分析摘要。";
        MeasurementDetailTextBlock.Text = "-";
    }

    private void SelectInstrument(Instrument instrument)
    {
        InstrumentNameTextBox.Text = instrument.InstrumentName;
        InstrumentModelTextBox.Text = instrument.Model;
        InstrumentConnectionTypeTextBox.Text = instrument.ConnectionType;
        InstrumentPortTextBox.Text = instrument.PortName ?? "-";
        InstrumentStatusTextBox.Text = instrument.Status;
    }

    private async Task SelectTaskAsync(MeasurementTask task)
    {
        TaskCodeText.Text = task.TaskCode;
        TaskStatusText.Text = $"任务状态: {task.Status}";
        RecordTaskCodeTextBox.Text = task.TaskCode;
        await LoadTaskSummaryAsync(task);
        UpdateTaskOverview(task);
    }

        private void UpdateTaskOverview(MeasurementTask task)
    {
        var taskRecords = _allMeasurementRecords.Where(x => x.TaskId == task.Id).OrderByDescending(x => x.MeasuredAt).ToList();
        var standardCount = taskRecords.Count(x => string.Equals(x.RecordType, "standard", StringComparison.OrdinalIgnoreCase));
        var trialCount = taskRecords.Count(x => string.Equals(x.RecordType, "trial", StringComparison.OrdinalIgnoreCase));
        var latestRecord = taskRecords.FirstOrDefault();
        var latestReport = latestRecord is null
            ? null
            : _allReportExports.Where(x => x.RecordId == latestRecord.Id).OrderByDescending(x => x.ExportedAt).FirstOrDefault();
        var latestActivity = _allOperationLogs.Where(x => x.TaskId == task.Id).OrderByDescending(x => x.CreatedAt).FirstOrDefault();

        TaskSummaryTextBlock.Text = $"任务 {task.TaskCode} 当前状态 {task.Status}，测量模式 {task.MeasurementMode}，平均次数 {task.AverageCount}，间隔 {task.IntervalSeconds}s。";
        TaskMetricTextBlock.Text = $"记录总数 {taskRecords.Count}，标准样记录 {standardCount}，试样记录 {trialCount}，最近测量 {(latestRecord is null ? "暂无" : $"{latestRecord.RecordType} / {latestRecord.PassStatus} / ΔE {latestRecord.TotalDeltaE?.ToString("0.00", CultureInfo.InvariantCulture) ?? "-"}")}";
        TaskRecentActivityTextBlock.Text = latestActivity is null
            ? "暂无最近活动。"
            : $"最近活动: [{latestActivity.ModuleName}] {latestActivity.OperationType} / {latestActivity.OperationResult} / {latestActivity.CreatedAt:yyyy-MM-dd HH:mm:ss}";
        TaskRecentReportTextBlock.Text = latestReport is null
            ? "最近报告: 暂无"
            : $"最近报告: {latestReport.ReportCode} / {latestReport.ExportedAt:yyyy-MM-dd HH:mm:ss}";
    }
private void ApplyRecordToInputs(MeasurementRecord record, MeasurementAngleResult? angle, MeasurementEffectResult? effect)
    {
        RecordTaskCodeTextBox.Text = TaskCodeText.Text;
        RecordNoTextBox.Text = record.RecordNo.ToString(CultureInfo.InvariantCulture);
        RecordTypeTextBox.Text = record.RecordType;
        RecordTotalDeltaETextBox.Text = record.TotalDeltaE?.ToString("0.00", CultureInfo.InvariantCulture) ?? string.Empty;
        RecordTotalEffectDiffTextBox.Text = record.TotalEffectDiff?.ToString("0.00", CultureInfo.InvariantCulture) ?? string.Empty;
        RecordSummaryTextBox.Text = record.ResultSummary ?? string.Empty;
        SetPassStatus(record.PassStatus);

        RecordAngleCodeTextBox.Text = angle?.AngleCode ?? string.Empty;
        RecordCieLTextBox.Text = angle?.CieL?.ToString("0.00", CultureInfo.InvariantCulture) ?? string.Empty;
        RecordCieATextBox.Text = angle?.CieA?.ToString("0.00", CultureInfo.InvariantCulture) ?? string.Empty;
        RecordCieBTextBox.Text = angle?.CieB?.ToString("0.00", CultureInfo.InvariantCulture) ?? string.Empty;
        RecordAngleDeltaETextBox.Text = angle?.DeltaE?.ToString("0.00", CultureInfo.InvariantCulture) ?? string.Empty;

        RecordSparkleValueTextBox.Text = effect?.SparkleValue?.ToString("0.00", CultureInfo.InvariantCulture) ?? string.Empty;
        RecordSparkleDiffTextBox.Text = effect?.SparkleDiff?.ToString("0.00", CultureInfo.InvariantCulture) ?? string.Empty;
        RecordGraininessValueTextBox.Text = effect?.GraininessValue?.ToString("0.00", CultureInfo.InvariantCulture) ?? string.Empty;
        RecordGraininessDiffTextBox.Text = effect?.GraininessDiff?.ToString("0.00", CultureInfo.InvariantCulture) ?? string.Empty;
    }

    private static void SelectComboItemByText(ComboBox comboBox, string? text)
    {
        var target = text?.Trim();
        foreach (var item in comboBox.Items.OfType<ComboBoxItem>())
        {
            if (string.Equals(item.Content?.ToString(), target, StringComparison.OrdinalIgnoreCase))
            {
                comboBox.SelectedItem = item;
                return;
            }
        }
    }

    private string GetSelectedReportFormat()
    {
        return (ReportFormatComboBox.SelectedItem as ComboBoxItem)?.Content?.ToString()?.Trim().ToLowerInvariant() ?? "md";
    }

    private void SetPassStatus(PassStatus passStatus)
    {
        foreach (var item in RecordPassStatusComboBox.Items.OfType<ComboBoxItem>())
        {
            if (string.Equals(item.Content?.ToString(), passStatus.ToString(), StringComparison.OrdinalIgnoreCase))
            {
                RecordPassStatusComboBox.SelectedItem = item;
                return;
            }
        }

        RecordPassStatusComboBox.SelectedIndex = 0;
    }

    private async Task WriteOperationLogAsync(string moduleName, string operationType, string operationResult, string description, string? taskId = null, string? recordId = null)
    {
        await _operationLogRepository.AddAsync(new OperationLog
        {
            TaskId = taskId,
            RecordId = recordId,
            ModuleName = moduleName,
            OperationType = operationType,
            OperationDesc = description,
            OperationResult = operationResult,
            CreatedAt = DateTime.UtcNow,
        });
    }
    private static string GetReportFolderPath()

    {
        return Path.Combine(AppContext.BaseDirectory, "Exports", "Reports");
    }

    private void ClearSampleInputs()
    {
        SampleCodeTextBox.Clear();
        SampleNameTextBox.Clear();
        SampleBatchTextBox.Clear();
        SampleMaterialTextBox.Clear();
        SampleColorTextBox.Clear();
    }

    private void ClearTemplateInputs()
    {
        TemplateCodeTextBox.Clear();
        TemplateNameTextBox.Clear();
        TemplateFormulaTextBox.Text = "DE00";
        TemplateOverallLimitTextBox.Text = "1.0";
        TemplateEffectLimitTextBox.Text = "1.0";
    }

    private void ClearStandardInputs()
    {
        StandardCodeTextBox.Clear();
        StandardNameTextBox.Clear();
        StandardMaterialTextBox.Clear();
        StandardColorTextBox.Clear();
        StandardTemplateCodeTextBox.Text = "TPL-DEFAULT";
    }

    private static bool TryParseRequiredDouble(string value, out double result)
    {
        return double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out result) ||
               double.TryParse(value, NumberStyles.Float, CultureInfo.CurrentCulture, out result);
    }

    private static bool TryParseOptionalDouble(string? value, out double? result)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            result = null;
            return true;
        }

        if (double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var invariant) ||
            double.TryParse(value, NumberStyles.Float, CultureInfo.CurrentCulture, out invariant))
        {
            result = invariant;
            return true;
        }

        result = null;
        return false;
    }

    private PassStatus GetSelectedPassStatus()
    {
        var text = (RecordPassStatusComboBox.SelectedItem as ComboBoxItem)?.Content?.ToString();
        return Enum.TryParse<PassStatus>(text, out var status) ? status : PassStatus.Pass;
    }

    private static string? NullIfWhiteSpace(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private void AppendLog(string message)
    {
        LogTextBox.AppendText($"[{DateTime.Now:HH:mm:ss}] {message}{Environment.NewLine}");
        LogTextBox.ScrollToEnd();
    }
}






























