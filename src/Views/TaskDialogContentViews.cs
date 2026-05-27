using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Litenbib.ViewModels;

namespace Litenbib.Views;

public partial class BatchFieldDeleteView : UserControl
{
    public BatchFieldDeleteView()
    {
        InitializeComponent();
    }
}

public partial class CleanupView : UserControl
{
    public CleanupView()
    {
        InitializeComponent();
    }
}

public partial class VenueNameNormalizationView : UserControl
{
    public VenueNameNormalizationView()
    {
        InitializeComponent();
    }
}

public partial class TitleProtectionView : UserControl
{
    public TitleProtectionView()
    {
        InitializeComponent();
    }
}

public partial class RenameFileView : UserControl
{
    public RenameFileView()
    {
        InitializeComponent();
        AttachedToVisualTree += RenameFileView_AttachedToVisualTree;
    }

    private void RenameFileView_AttachedToVisualTree(object? sender, VisualTreeAttachmentEventArgs e)
    {
        Dispatcher.UIThread.Post(() =>
        {
            FileNameTextBox.Focus();
            if (DataContext is RenameFileViewModel viewModel)
            {
                FileNameTextBox.SelectionStart = 0;
                FileNameTextBox.SelectionEnd = viewModel.BaseNameLength;
            }
        });
    }

    private void FileNameTextBox_KeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key != Key.Enter
            || DataContext is not RenameFileViewModel { CanApply: true })
        {
            return;
        }

        e.Handled = true;
        this.FindAncestorOfType<Window>()?.Close(true);
    }
}
