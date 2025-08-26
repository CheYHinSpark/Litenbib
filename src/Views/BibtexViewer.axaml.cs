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
    private readonly BibtexViewerViewModel viewModel;
    public BibtexViewer()
    {
        InitializeComponent();
        viewModel = new BibtexViewerViewModel();
        DataContext = viewModel;
    }

    // 选中事件
    private void DataGrid_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (sender == null || DataContext == null) { return; }
        viewModel.ChangeShowing(((DataGrid)sender).SelectedIndex);
    }
}