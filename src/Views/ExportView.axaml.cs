using Avalonia.Interactivity;
using Litenbib.Models;
using Litenbib.ViewModels;
using System.Collections.Generic;

namespace Litenbib.Views;

public partial class ExportView : StyledWindow
{
    public ExportView()
    {
        InitializeComponent();
        this.DataContext = new ExportViewModel();
    }

    public ExportView(List<BibtexEntry> list, string path)
    {
        InitializeComponent();
        this.DataContext = new ExportViewModel(list, path);
    }

    private void CancelButton_Click(object? sender, RoutedEventArgs e)
    { this.Close(false); }
}
