using System.Diagnostics;
using System.IO;
using System.Windows;
using MAS.Application.Services;
using MAS.Infrastructure.Database;

namespace MAS.WinUI;

public partial class MainWindow : Window
{
    private readonly SqliteScriptBootstrapper _bootstrapper = new();
    private readonly MeasurementTaskService _taskService = new();

    public MainWindow()
    {
        InitializeComponent();
        DatabasePathText.Text = _bootstrapper.DatabasePath;
        AppendLog("应用已启动。等待初始化数据库。");
    }

    private async void InitializeDatabaseButton_OnClick(object sender, RoutedEventArgs e)
    {
        try
        {
            InitializeDatabaseButton.IsEnabled = false;
            AppendLog("开始初始化数据库...");
            await _bootstrapper.EnsureCreatedAsync();
            DatabaseStatusText.Text = "已初始化";
            DatabasePathText.Text = _bootstrapper.DatabasePath;
            AppendLog($"数据库初始化完成: {_bootstrapper.DatabasePath}");
        }
        catch (Exception ex)
        {
            DatabaseStatusText.Text = "初始化失败";
            AppendLog($"数据库初始化失败: {ex.Message}");
            MessageBox.Show(this, ex.Message, "数据库初始化失败", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            InitializeDatabaseButton.IsEnabled = true;
        }
    }

    private void CreateDraftTaskButton_OnClick(object sender, RoutedEventArgs e)
    {
        var task = _taskService.CreateDraftTask(
            instrumentId: "demo-instrument",
            sampleId: "demo-sample",
            standardSampleId: "demo-standard",
            templateId: "default-template");

        TaskCodeText.Text = task.TaskCode;
        AppendLog($"已创建任务草稿: {task.TaskCode}");
    }

    private void OpenSchemaButton_OnClick(object sender, RoutedEventArgs e)
    {
        try
        {
            var folder = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "MAS.Infrastructure", "Database", "Schema");
            var fullPath = Path.GetFullPath(folder);
            Process.Start(new ProcessStartInfo
            {
                FileName = fullPath,
                UseShellExecute = true,
            });
            AppendLog($"已打开建表脚本目录: {fullPath}");
        }
        catch (Exception ex)
        {
            AppendLog($"打开建表脚本目录失败: {ex.Message}");
        }
    }

    private void AppendLog(string message)
    {
        LogTextBox.AppendText($"[{DateTime.Now:HH:mm:ss}] {message}{Environment.NewLine}");
        LogTextBox.ScrollToEnd();
    }
}
