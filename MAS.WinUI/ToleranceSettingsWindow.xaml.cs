using System.Collections.ObjectModel;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using MAS.WinUI.Configuration;

namespace MAS.WinUI;

public partial class ToleranceSettingsWindow : Window
{
    private readonly ObservableCollection<ToleranceRangeItem> _colorItems;
    private readonly ObservableCollection<ToleranceRangeItem> _sparkleItems;
    private readonly ObservableCollection<ToleranceRangeItem> _overallItems;

    public ToleranceSettingsDocument Settings { get; private set; }

    public ToleranceSettingsWindow(ToleranceSettingsDocument settings)
    {
        InitializeComponent();
        Settings = Clone(settings);
        _colorItems = new ObservableCollection<ToleranceRangeItem>(Settings.ColorToleranceItems.Select(CloneItem));
        _sparkleItems = new ObservableCollection<ToleranceRangeItem>(Settings.SparkleToleranceItems.Select(CloneItem));
        _overallItems = new ObservableCollection<ToleranceRangeItem>(Settings.OverallToleranceItems.Select(CloneItem));

        ColorToleranceGrid.ItemsSource = _colorItems;
        SparkleToleranceGrid.ItemsSource = _sparkleItems;
        OverallToleranceGrid.ItemsSource = _overallItems;
        SelectComboItemByText(ToleranceTypeComboBox, Settings.ToleranceType);
    }

    private void AcceptButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (!TryNormalizeGridValues(ColorToleranceGrid, _colorItems)
            || !TryNormalizeGridValues(SparkleToleranceGrid, _sparkleItems)
            || !TryNormalizeGridValues(OverallToleranceGrid, _overallItems))
        {
            MessageBox.Show(this, "容差最小值和最大值格式不正确。", "容差设置", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        Settings = new ToleranceSettingsDocument
        {
            ToleranceType = (ToleranceTypeComboBox.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "ΔE*",
            ColorToleranceItems = _colorItems.Select(CloneItem).ToList(),
            SparkleToleranceItems = _sparkleItems.Select(CloneItem).ToList(),
            OverallToleranceItems = _overallItems.Select(CloneItem).ToList(),
        };

        DialogResult = true;
        Close();
    }

    private void CancelButton_OnClick(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    private static bool TryNormalizeGridValues(DataGrid grid, ObservableCollection<ToleranceRangeItem> items)
    {
        grid.CommitEdit(DataGridEditingUnit.Cell, true);
        grid.CommitEdit(DataGridEditingUnit.Row, true);

        foreach (var item in items)
        {
            if (double.IsNaN(item.MinValue) || double.IsNaN(item.MaxValue))
            {
                return false;
            }

            if (item.MinValue > item.MaxValue)
            {
                (item.MinValue, item.MaxValue) = (item.MaxValue, item.MinValue);
            }
        }

        return true;
    }

    private static ToleranceSettingsDocument Clone(ToleranceSettingsDocument settings)
    {
        return new ToleranceSettingsDocument
        {
            ToleranceType = settings.ToleranceType,
            ColorToleranceItems = settings.ColorToleranceItems.Select(CloneItem).ToList(),
            SparkleToleranceItems = settings.SparkleToleranceItems.Select(CloneItem).ToList(),
            OverallToleranceItems = settings.OverallToleranceItems.Select(CloneItem).ToList(),
        };
    }

    private static ToleranceRangeItem CloneItem(ToleranceRangeItem item)
    {
        return new ToleranceRangeItem
        {
            AngleCode = item.AngleCode,
            MinValue = item.MinValue,
            MaxValue = item.MaxValue,
        };
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

        comboBox.SelectedIndex = comboBox.Items.Count > 0 ? 0 : -1;
    }
}
