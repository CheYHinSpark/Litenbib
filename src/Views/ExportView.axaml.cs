using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
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
}