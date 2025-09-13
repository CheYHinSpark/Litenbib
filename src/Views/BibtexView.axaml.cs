using Avalonia;
using Avalonia.Animation;
using Avalonia.Animation.Easings;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Styling;
using Litenbib.Models;
using Litenbib.ViewModels;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Litenbib.Views;

public partial class BibtexView : UserControl
{
    private bool isColumns = true;

    private bool isDetailShowing = false;

    private BibtexViewModel viewModel = null!;

    private bool changingInside = false;

    public BibtexView()
    {
        InitializeComponent();
        SetPopup();
    }

    protected override void OnInitialized()
    {
        base.OnInitialized();
        if (DataContext is BibtexViewModel vm)
        {
            viewModel = vm;
            viewModel.CheckingEvent += (_, e) => { SetSelectionAndScroll(); };
        }
    }

    private void SetSelectionAndScroll()
    {
        changingInside = true;
        DataGridView.SelectAll();
        changingInside = false;
    }


    private void SetPopup()
    {
        WarningPopup.OpenAnimation = new Animation()
        {
            Duration = TimeSpan.FromSeconds(0.1),
            FillMode = FillMode.Both,
            Children =
            {
                new KeyFrame
                {
                    Cue = new Cue(0.0),
                    Setters =
                    {
                        new Setter(OpacityProperty, 0.0),
                        new Setter(TranslateTransform.XProperty, -20.0),
                        new Setter(TranslateTransform.YProperty, 40.0)
                    }
                },
                new KeyFrame
                {
                    Cue = new Cue(1.0),
                    Setters =
                    {
                        new Setter(OpacityProperty, 1.0),
                        new Setter(TranslateTransform.XProperty, 0.0),
                        new Setter(TranslateTransform.YProperty, 0.0)
                    }
                }
            }
        };
        WarningPopup.CloseAnimation = new Animation()
        {
            Duration = TimeSpan.FromSeconds(0.1),
            FillMode = FillMode.Both,
            Children =
            {
                new KeyFrame
                {
                    Cue = new Cue(0.0),
                    Setters =
                    {
                        new Setter(OpacityProperty, 1.0),
                        new Setter(TranslateTransform.XProperty, 0.0),
                        new Setter(TranslateTransform.YProperty, 0.0)
                    }
                },
                new KeyFrame
                {
                    Cue = new Cue(1.0),
                    Setters =
                    {
                        new Setter(OpacityProperty, 0.0),
                        new Setter(TranslateTransform.XProperty, -20.0),
                        new Setter(TranslateTransform.YProperty, 40.0)
                    }
                }
            }
        };
    }

    // 选中事件
    private void DataGrid_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (sender is DataGrid grid && !changingInside)
        {
            if (e.AddedItems.Count > 0 && viewModel.ShowingEntry == e.AddedItems[0])
            {
                grid.ScrollIntoView(viewModel.ShowingEntry, null);
                grid.SelectedItems.Add(viewModel.ShowingEntry);
            }
            ShowDetail();
            viewModel.SetSelectedItems(grid.SelectedItems.Cast<BibtexEntry>());
        }
    }

    private void ShowDetail()
    {
        if (isColumns)
        {
            RootGrid.RowDefinitions[2].MinHeight = 0;
            RootGrid.RowDefinitions[2].MaxHeight = 0;
            RootGrid.ColumnDefinitions[2].MinWidth = 500;
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
            RootGrid.RowDefinitions[2].MaxHeight = 400;
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

    private void ChangeButton_Click(object? sender, RoutedEventArgs e)
    {
        isColumns = !isColumns;
        ShowDetail();
    }
    private void CloseButton_Click(object? sender, RoutedEventArgs e)
    {
        CloseDetail();
    }

    private void GridSplitter_DragDelta(object? sender, VectorEventArgs e)
    {
        if (isColumns)
        {
            if (isDetailShowing)
            { if (e.Vector.X > 400 * 0.6) { CloseDetail(); } }
            else
            { if (e.Vector.X < -400 * 0.6) { ShowDetail(); } }
        }
        else
        {
            if (isDetailShowing)
            { if (e.Vector.Y > 200 * 0.6) { CloseDetail(); } }
            else
            { if (e.Vector.Y < -200 * 0.6) { ShowDetail(); } }
        }
    }
}