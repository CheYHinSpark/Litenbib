using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Litenbib.Models;
using Litenbib.ViewModels;
using System;
using System.Diagnostics;

namespace Litenbib.Views;

public partial class BibtexViewer : UserControl
{
    public BibtexViewer()
    {
        InitializeComponent();
        DataContext = new BibtexViewerViewModel();
    }

    // 选中事件
    private void DataGrid_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (sender == null || DataContext == null) { return; }
        ((BibtexViewerViewModel)DataContext).ChangeShowing(((DataGrid)sender).SelectedIndex);
    }
}