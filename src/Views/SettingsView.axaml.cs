using Avalonia.Interactivity;
using Litenbib.Models;
using Litenbib.ViewModels;

namespace Litenbib.Views;

public partial class SettingsView : StyledWindow
{
    public SettingsView()
    {
        InitializeComponent();
        DataContext = new SettingsViewModel();
    }

    public SettingsView(AppSettings settings)
    {
        InitializeComponent();
        DataContext = new SettingsViewModel(settings);
    }

    private void ApplyButton_Click(object? sender, RoutedEventArgs e)
    { Close(true); }

    private void CancelButton_Click(object? sender, RoutedEventArgs e)
    { Close(false); }
}
