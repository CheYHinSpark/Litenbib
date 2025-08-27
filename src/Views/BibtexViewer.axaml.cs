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

    // 选中事件
    private void DataGrid_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (sender == null || DataContext == null) { return; }
        viewModel.ChangeShowing(((DataGrid)sender).SelectedItem);
        ChangeLayout();
        isDetailShowing = true;
    }


    private void ChangeLayout()
    {
        if (isColumns)
        {
            RootGrid.RowDefinitions[2].Height = GridLength.Auto;
            RootGrid.RowDefinitions[2].MinHeight = 0;
            RootGrid.ColumnDefinitions[2].Width = GridLength.Star;
            RootGrid.ColumnDefinitions[2].MinWidth = 400;
            Grid.SetRow(Splitter, 0);
            Grid.SetRow(DetailPanel, 0);
            Grid.SetColumn(Splitter, 1);
            Grid.SetColumn(DetailPanel, 2);
        }
        else
        {
            RootGrid.RowDefinitions[2].Height = GridLength.Star;
            RootGrid.RowDefinitions[2].MinHeight = 250;
            RootGrid.ColumnDefinitions[2].Width = GridLength.Auto;
            RootGrid.ColumnDefinitions[2].MinWidth = 0;
            Grid.SetRow(Splitter, 1);
            Grid.SetRow(DetailPanel, 2);
            Grid.SetColumn(Splitter, 0);
            Grid.SetColumn(DetailPanel, 0);
        }
    }

    private void ChangeButton_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        isColumns = !isColumns;
        ChangeLayout();
    }
    private void CloseButton_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        RootGrid.RowDefinitions[2].MinHeight = 0;
        RootGrid.RowDefinitions[2].Height = GridLength.Parse("0");
        RootGrid.ColumnDefinitions[2].MinWidth = 0;
        RootGrid.ColumnDefinitions[2].Width = GridLength.Parse("0");
        isDetailShowing = false;
    }

    private void GridSplitter_DragDelta(object? sender, Avalonia.Input.VectorEventArgs e)
    {
        if (isDetailShowing == false)
        {
            isDetailShowing = true;
            ChangeLayout();
        }
    }
}