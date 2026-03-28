using System.Collections.ObjectModel;
using System.Windows;
using MAS.WinUI.Configuration;

namespace MAS.WinUI;

public partial class DisplaySettingsWindow : Window
{
    private readonly UiOptionCatalog _catalog;
    private readonly List<string> _defaultVisibleItems;
    private readonly Dictionary<string, List<string>> _categoryItems;
    private readonly ObservableCollection<string> _currentAvailableItems = [];
    private readonly ObservableCollection<string> _currentVisibleItems = [];

    public List<string> SelectedItems { get; private set; } = [];

    public DisplaySettingsWindow(UiOptionCatalog catalog, IEnumerable<string> visibleItems)
    {
        InitializeComponent();
        _catalog = catalog;
        _defaultVisibleItems = catalog.DefaultDisplayItems.ToList();
        _categoryItems = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase)
        {
            ["颜色"] = catalog.AvailableDisplayItems.ToList(),
            ["色差"] = ["ΔL*", "Δa*", "Δb*", "ΔC*", "ΔH*"],
            ["光谱数据"] = ["X", "Y", "Z"],
            ["颜色指数"] = ["R", "G", "B"]
        };

        CategoryListBox.ItemsSource = _categoryItems.Keys;
        VisibleItemsListBox.ItemsSource = _currentVisibleItems;
        AvailableItemsListBox.ItemsSource = _currentAvailableItems;

        foreach (var item in visibleItems.Where(x => !string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.OrdinalIgnoreCase))
        {
            _currentVisibleItems.Add(item);
        }

        if (_currentVisibleItems.Count == 0)
        {
            foreach (var item in _defaultVisibleItems)
            {
                _currentVisibleItems.Add(item);
            }
        }

        CategoryListBox.SelectedIndex = 0;
        RefreshAvailableItems();
    }

    private void CategoryListBox_OnSelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
        RefreshAvailableItems();
    }

    private void MoveSelectedToVisible_OnClick(object sender, RoutedEventArgs e)
    {
        if (AvailableItemsListBox.SelectedItem is string item && !_currentVisibleItems.Contains(item))
        {
            _currentVisibleItems.Add(item);
            RefreshAvailableItems();
        }
    }

    private void MoveSelectedToAvailable_OnClick(object sender, RoutedEventArgs e)
    {
        if (VisibleItemsListBox.SelectedItem is string item)
        {
            _currentVisibleItems.Remove(item);
            RefreshAvailableItems();
        }
    }

    private void MoveAllToVisible_OnClick(object sender, RoutedEventArgs e)
    {
        foreach (var item in _catalog.AvailableDisplayItems)
        {
            if (!_currentVisibleItems.Contains(item))
            {
                _currentVisibleItems.Add(item);
            }
        }

        RefreshAvailableItems();
    }

    private void MoveAllToAvailable_OnClick(object sender, RoutedEventArgs e)
    {
        _currentVisibleItems.Clear();
        RefreshAvailableItems();
    }

    private void MoveUp_OnClick(object sender, RoutedEventArgs e)
    {
        MoveVisibleItem(-1);
    }

    private void MoveDown_OnClick(object sender, RoutedEventArgs e)
    {
        MoveVisibleItem(1);
    }

    private void RestoreDefaultButton_OnClick(object sender, RoutedEventArgs e)
    {
        _currentVisibleItems.Clear();
        foreach (var item in _defaultVisibleItems)
        {
            _currentVisibleItems.Add(item);
        }

        RefreshAvailableItems();
    }

    private void AcceptButton_OnClick(object sender, RoutedEventArgs e)
    {
        SelectedItems = _currentVisibleItems.ToList();
        DialogResult = true;
        Close();
    }

    private void RefreshAvailableItems()
    {
        var selectedCategory = CategoryListBox.SelectedItem?.ToString() ?? "颜色";
        var items = _categoryItems.TryGetValue(selectedCategory, out var values) ? values : _catalog.AvailableDisplayItems;
        var available = items.Where(item => !_currentVisibleItems.Contains(item)).ToList();
        _currentAvailableItems.Clear();
        foreach (var item in available)
        {
            _currentAvailableItems.Add(item);
        }
    }

    private void MoveVisibleItem(int offset)
    {
        if (VisibleItemsListBox.SelectedItem is not string selected)
        {
            return;
        }

        var index = _currentVisibleItems.IndexOf(selected);
        if (index < 0)
        {
            return;
        }

        var targetIndex = index + offset;
        if (targetIndex < 0 || targetIndex >= _currentVisibleItems.Count)
        {
            return;
        }

        _currentVisibleItems.Move(index, targetIndex);
        VisibleItemsListBox.SelectedItem = selected;
    }
}
