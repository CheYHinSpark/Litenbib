using Avalonia.Interactivity;
using Litenbib.ViewModels;

namespace Litenbib.Views;

public partial class BatchFieldDeleteView : StyledWindow
{
    public BatchFieldDeleteView()
    {
        InitializeComponent();
        DataContext = new BatchFieldDeleteViewModel();
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
