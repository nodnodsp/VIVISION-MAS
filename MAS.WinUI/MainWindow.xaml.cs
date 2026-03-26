using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using MAS.Application.Services;
using MAS.Core.Entities;
using MAS.Core.Enums;
using MAS.Infrastructure.Database;
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
    private IReadOnlyList<MeasurementTask> _allTasks = Array.Empty<MeasurementTask>();
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
        AppendLog("应用已启动。准备加载数据库状态。");
    }

    private async void MainWindow_OnLoaded(object sender, RoutedEventArgs e)
    {
        await EnsureDatabaseReadyAsync();
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
        SelectTask(task);
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
            var result = await _reportService.ExportRecordReportAsync(record.Id);
            AppendLog($"报告已导出: {result.ReportCode}");
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
            SelectTask(task);
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
            SelectTask(result.Task);
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

        _allTasks = tasks;
        _allMeasurementRecords = records;
        _allReportExports = reportExports;
        _allOperationLogs = operationLogs;

        InstrumentGrid.ItemsSource = instruments;
        SampleGrid.ItemsSource = samples;
        StandardSampleGrid.ItemsSource = standards;
        TemplateGrid.ItemsSource = templates;
        TaskGrid.ItemsSource = _allTasks;
        ApplyRecordFilters();
        ApplyReportFilters();

        var selectedInstrument = instruments.FirstOrDefault();
        if (selectedInstrument is not null)
        {
            SelectInstrument(selectedInstrument);
            CalibrationRecordGrid.ItemsSource = calibrationRecords.Where(x => x.InstrumentId == selectedInstrument.Id).ToList();
        }

        var latestTask = tasks.LastOrDefault();
        if (latestTask is not null)
        {
            SelectTask(latestTask);
        }

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
        }

        var selectedReport = selectedReportId is null
            ? reportExports.OrderByDescending(x => x.ExportedAt).FirstOrDefault()
            : reportExports.FirstOrDefault(x => x.Id == selectedReportId) ?? reportExports.OrderByDescending(x => x.ExportedAt).FirstOrDefault();

        ReportExportGrid.SelectedItem = selectedReport;
        LoadReportPreview(selectedReport);
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
        AngleResultGrid.ItemsSource = await _angleResultRepository.GetByRecordIdAsync(recordId);
        EffectResultGrid.ItemsSource = await _effectResultRepository.GetByRecordIdAsync(recordId);
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
        ReportPreviewTextBox.Text = content.Length <= 6000 ? content : content[..6000] + Environment.NewLine + Environment.NewLine + "...（预览已截断）";
    }

    private void ClearReportPreview()
    {
        ReportMetaTextBlock.Text = "请选择一条导出记录查看报告内容。";
        ReportPathTextBlock.Text = "-";
        ReportPreviewTextBox.Text = string.Empty;
    }

    private void SelectInstrument(Instrument instrument)
    {
        InstrumentNameTextBox.Text = instrument.InstrumentName;
        InstrumentModelTextBox.Text = instrument.Model;
        InstrumentConnectionTypeTextBox.Text = instrument.ConnectionType;
        InstrumentPortTextBox.Text = instrument.PortName ?? "-";
        InstrumentStatusTextBox.Text = instrument.Status;
    }

    private void SelectTask(MeasurementTask task)
    {
        TaskCodeText.Text = task.TaskCode;
        TaskStatusText.Text = $"任务状态: {task.Status}";
        RecordTaskCodeTextBox.Text = task.TaskCode;
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


