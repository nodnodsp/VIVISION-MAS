using System.Windows;
using MAS.WinUI.Configuration;

namespace MAS.WinUI;

public partial class LightSourceObserverWindow : Window
{
    public string SelectedLightSource { get; private set; } = string.Empty;
    public string SelectedObserver { get; private set; } = string.Empty;

    public LightSourceObserverWindow(UiOptionCatalog catalog, string? currentLightSource, string? currentObserver)
    {
        InitializeComponent();
        LightSourceComboBox.ItemsSource = catalog.LightSources;
        ObserverComboBox.ItemsSource = catalog.Observers;
        LightSourceComboBox.SelectedItem = catalog.LightSources.FirstOrDefault(x => string.Equals(x, currentLightSource, StringComparison.OrdinalIgnoreCase))
            ?? catalog.LightSources.FirstOrDefault();
        ObserverComboBox.SelectedItem = catalog.Observers.FirstOrDefault(x => string.Equals(x, currentObserver, StringComparison.OrdinalIgnoreCase))
            ?? catalog.Observers.FirstOrDefault();
    }

    private void AcceptButton_OnClick(object sender, RoutedEventArgs e)
    {
        SelectedLightSource = LightSourceComboBox.SelectedItem?.ToString() ?? string.Empty;
        SelectedObserver = ObserverComboBox.SelectedItem?.ToString() ?? string.Empty;
        DialogResult = true;
        Close();
    }

    private void CancelButton_OnClick(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
