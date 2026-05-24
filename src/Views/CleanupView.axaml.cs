using Avalonia.Interactivity;
using Litenbib.Models;
using Litenbib.ViewModels;
using System.Collections.Generic;

namespace Litenbib.Views;

public partial class CleanupView : StyledWindow
{
    public CleanupView()
    {
        InitializeComponent();
        DataContext = new CleanupViewModel();
    }

    public CleanupView(IReadOnlyList<BibtexEntry> entries)
    {
        InitializeComponent();
        DataContext = new CleanupViewModel(entries);
    }

    private void CancelButton_Click(object? sender, RoutedEventArgs e)
    {
        Close(false);
    }

    private void ApplyButton_Click(object? sender, RoutedEventArgs e)
    {
        Close(true);
    }
}
