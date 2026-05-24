using Avalonia.Interactivity;
using Litenbib.ViewModels;

namespace Litenbib.Views;

public partial class TaskDialogView : StyledWindow
{
    public TaskDialogView()
    {
        InitializeComponent();
        DataContext = new TaskDialogViewModel();
    }

    public TaskDialogView(ITaskDialogContentViewModel content)
    {
        InitializeComponent();
        DataContext = new TaskDialogViewModel(content);
    }

    public ITaskDialogContentViewModel? ContentViewModel =>
        (DataContext as TaskDialogViewModel)?.Content;

    private void CancelButton_Click(object? sender, RoutedEventArgs e)
    {
        Close(false);
    }

    private void ApplyButton_Click(object? sender, RoutedEventArgs e)
    {
        Close(true);
    }
}
