using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Litenbib.Models;
using Litenbib.ViewModels;
using System;
using System.Diagnostics;

namespace Litenbib.Views;

public partial class BibtexView : UserControl
{
    private bool isColumns = true;

    private bool isDetailShowing = false;

    public BibtexView()
    {
        InitializeComponent();
    }

    // 选中事件
    private void DataGrid_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (sender == null || DataContext == null) { return; }
        ((BibtexViewModel)DataContext).ChangeShowing(((DataGrid)sender).SelectedItem);
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
            Splitter.Margin = Thickness.Parse("0 24");
            DetailPanel.CornerRadius = CornerRadius.Parse("24 0 0 24");
            DetailPanel.Margin = Thickness.Parse("8 8 0 0");
            DetailPanel.Padding = Thickness.Parse("8 8 4 8");
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
            Splitter.Margin = Thickness.Parse("24 0");
            DetailPanel.CornerRadius = CornerRadius.Parse("24 24 0 0");
            DetailPanel.Margin = Thickness.Parse("0 8 0 0");
            DetailPanel.Padding = Thickness.Parse("8 8 8 4");
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