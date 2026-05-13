using Avalonia.Interactivity;
using Litenbib.Models;
using Litenbib.ViewModels;
using System.Collections.Generic;

namespace Litenbib.Views;

public partial class VenueNameNormalizationView : StyledWindow
{
    public VenueNameNormalizationView()
    {
        InitializeComponent();
        DataContext = new VenueNameNormalizationViewModel();
    }

    public VenueNameNormalizationView(IEnumerable<BibtexEntry> entries)
    {
        InitializeComponent();
        DataContext = new VenueNameNormalizationViewModel(entries);
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
