using Avalonia.Interactivity;
using Litenbib.ViewModels;

namespace Litenbib.Views;

public partial class BatchFieldEditView : StyledWindow
{
    public BatchFieldEditView()
    {
        InitializeComponent();
        DataContext = new BatchFieldEditViewModel();
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
