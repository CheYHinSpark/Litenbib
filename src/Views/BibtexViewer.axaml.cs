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

    private bool isColumns = true;

    private bool isDetailShowing = false;

    public BibtexViewer()
    {
        InitializeComponent();

        viewModel = new BibtexViewerViewModel();
        DataContext = viewModel;
    }

    // ѡ���¼�
    private void DataGrid_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (sender == null || DataContext == null) { return; }
        viewModel.ChangeShowing(((DataGrid)sender).SelectedItem);
        ShowDetail();
    }


    private void ShowDetail()
    {
        if (isColumns)
        {
            RootGrid.RowDefinitions[2].MinHeight = 0;
            RootGrid.RowDefinitions[2].MaxHeight = 0;
            RootGrid.ColumnDefinitions[2].MinWidth = 400;
            RootGrid.ColumnDefinitions[2].MaxWidth = 800;
            Grid.SetRow(Splitter, 0);
            Grid.SetRow(DetailPanel, 0);
            Grid.SetColumn(Splitter, 1);
            Grid.SetColumn(DetailPanel, 2);
        }
        else
        {
            RootGrid.RowDefinitions[2].MinHeight = 250;
            RootGrid.RowDefinitions[2].MaxHeight = 500;
            RootGrid.ColumnDefinitions[2].MinWidth = 0;
            RootGrid.ColumnDefinitions[2].MaxWidth = 0;
            Grid.SetRow(Splitter, 1);
            Grid.SetRow(DetailPanel, 2);
            Grid.SetColumn(Splitter, 0);
            Grid.SetColumn(DetailPanel, 0);
        }
        isDetailShowing = true;
    }

    private void CloseDetail()
    {
        RootGrid.RowDefinitions[2].MinHeight = 0;
        RootGrid.RowDefinitions[2].MaxHeight = 0;
        RootGrid.ColumnDefinitions[2].MinWidth = 0;
        RootGrid.ColumnDefinitions[2].MaxWidth = 0;
        isDetailShowing = false;
    }

    private void ChangeButton_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        isColumns = !isColumns;
        ShowDetail();
    }
    private void CloseButton_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        CloseDetail();
    }

    private void GridSplitter_DragDelta(object? sender, Avalonia.Input.VectorEventArgs e)
    {
        if (isColumns)
        {
            if (isDetailShowing)
            { if (e.Vector.X > 400 * 0.6) { CloseDetail(); } }
            else
            { if (e.Vector.X < - 400 * 0.6) { ShowDetail(); } }
        }
        else
        {
            if (isDetailShowing)
            { if (e.Vector.Y > 250 * 0.6) { CloseDetail(); } }
            else
            { if (e.Vector.Y < -250 * 0.6) { ShowDetail(); } }
        }
    }
}