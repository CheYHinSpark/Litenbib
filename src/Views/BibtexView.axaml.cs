using Avalonia;
using Avalonia.Animation;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using Avalonia.Styling;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Litenbib.Models;
using Litenbib.ViewModels;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Litenbib.Views;

public partial class BibtexView : UserControl
{
    private const double RightDetailOpenMinWidth = 500;

    private const double RightDetailOpenMaxWidth = 800;

    private const double BottomDetailOpenMinHeight = 300;

    private const double BottomDetailOpenMaxHeight = 480;

    public static readonly StyledProperty<double> DetailColumnMinWidthProperty =
        AvaloniaProperty.Register<BibtexView, double>(nameof(DetailColumnMinWidth));

    public static readonly StyledProperty<double> DetailColumnMaxWidthProperty =
        AvaloniaProperty.Register<BibtexView, double>(nameof(DetailColumnMaxWidth));

    public static readonly StyledProperty<double> DetailRowMinHeightProperty =
        AvaloniaProperty.Register<BibtexView, double>(nameof(DetailRowMinHeight));

    public static readonly StyledProperty<double> DetailRowMaxHeightProperty =
        AvaloniaProperty.Register<BibtexView, double>(nameof(DetailRowMaxHeight));

    public double DetailColumnMinWidth
    {
        get => GetValue(DetailColumnMinWidthProperty);
        private set => SetValue(DetailColumnMinWidthProperty, value);
    }

    public double DetailColumnMaxWidth
    {
        get => GetValue(DetailColumnMaxWidthProperty);
        private set => SetValue(DetailColumnMaxWidthProperty, value);
    }

    public double DetailRowMinHeight
    {
        get => GetValue(DetailRowMinHeightProperty);
        private set => SetValue(DetailRowMinHeightProperty, value);
    }

    public double DetailRowMaxHeight
    {
        get => GetValue(DetailRowMaxHeightProperty);
        private set => SetValue(DetailRowMaxHeightProperty, value);
    }

    private bool isDetailShowing = false;

    private BibtexViewModel viewModel = null!;

    private bool changingInside = false;

    public BibtexView()
    {
        InitializeComponent();
        SetPopup();
        ApplyDetailLayout();
        AttachedToVisualTree += OnAttachedToVisualTree;
        DetachedFromVisualTree += OnDetachedFromVisualTree;
    }

    private void OnAttachedToVisualTree(object? sender, VisualTreeAttachmentEventArgs e)
    {
        AppSettingsState.SettingsChanged -= AppSettingsState_SettingsChanged;
        AppSettingsState.SettingsChanged += AppSettingsState_SettingsChanged;
        ApplyDetailLayout();
        CheckExternalChanges();
    }

    private void OnDetachedFromVisualTree(object? sender, VisualTreeAttachmentEventArgs e)
    {
        AppSettingsState.SettingsChanged -= AppSettingsState_SettingsChanged;
    }

    private void AppSettingsState_SettingsChanged(object? sender, EventArgs e)
    {
        if (Dispatcher.UIThread.CheckAccess())
        {
            ApplyDetailLayout();
            return;
        }

        Dispatcher.UIThread.Post(ApplyDetailLayout);
    }

    protected override void OnInitialized()
    {
        base.OnInitialized();
        if (DataContext is BibtexViewModel vm)
        {
            viewModel = vm;
            viewModel.CheckingEvent += (_, e) => { SetSelectionAndScroll(); };
            viewModel.FocusEntryRequested += (_, entry) => { FocusEntry(entry); };
        }
    }


    private void SetSelectionAndScroll()
    {
        changingInside = true;
        DataGridView.SelectAll();
        changingInside = false;
        viewModel.SetSelectedItems(DataGridView.SelectedItems.Cast<BibtexEntry>());
        UpdateSelectAllState();
    }

    private void FocusEntry(BibtexEntry entry)
    {
        Dispatcher.UIThread.Post(() =>
        {
            changingInside = true;
            DataGridView.SelectedItem = entry;
            changingInside = false;
            viewModel.SetSelectedItems([entry]);
            UpdateSelectAllState();
            ShowDetail();
            DataGridView.ScrollIntoView(entry, null);
        });
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

    // ѡ���¼�
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
            UpdateSelectAllState();
        }
    }

    private void SelectAllCheckBox_Click(object? sender, RoutedEventArgs e)
    {
        if (changingInside) { return; }

        if (SelectAllCheckBox.IsChecked == true)
        {
            changingInside = true;
            DataGridView.SelectAll();
            changingInside = false;
            viewModel.SetSelectedItems(DataGridView.SelectedItems.Cast<BibtexEntry>());
            UpdateSelectAllState();
        }
        else
        {
            changingInside = true;
            DataGridView.SelectedItems.Clear();
            changingInside = false;
            viewModel.SetSelectedItems([]);
            UpdateSelectAllState();
        }
    }

    private void UpdateSelectAllState()
    {
        if (viewModel == null)
        {
            return;
        }

        int itemCount = viewModel.BibtexView.Count;
        int selectedCount = DataGridView.SelectedItems.Count;
        bool? isChecked = selectedCount == 0 || itemCount == 0
            ? false
            : selectedCount >= itemCount
                ? true
                : null;

        if (SelectAllCheckBox.IsChecked == isChecked)
        {
            return;
        }

        changingInside = true;
        SelectAllCheckBox.IsChecked = isChecked;
        changingInside = false;
    }

    private void CheckExternalChanges()
    {
        if (viewModel == null || string.IsNullOrWhiteSpace(viewModel.FullPath) || !File.Exists(viewModel.FullPath))
        { return; }
        viewModel.DetectExternalModification();
        if (viewModel.HasExternalChanges)
        {
            viewModel.ShowStatus(I18n.Get("Message.FileChangedOnDiskStatus"));
        }
    }

    private void ShowDetail()
    {
        isDetailShowing = true;
        ApplyDetailLayout();
    }

    private void CloseDetail()
    {
        isDetailShowing = false;
        ApplyDetailLayout();
    }

    private void ChangeButton_Click(object? sender, RoutedEventArgs e)
    {
        isDetailShowing = true;

        AppSettings settings = AppSettingsState.Current.Copy();
        settings.BibtexDetailPlacement = IsDetailLayoutRight
            ? BibtexDetailPlacements.Bottom
            : BibtexDetailPlacements.Right;
        AppSettingsState.Apply(settings);
        _ = SaveLayoutPreferenceAsync();
    }
    private void CloseButton_Click(object? sender, RoutedEventArgs e)
    {
        CloseDetail();
    }

    private void GridSplitter_DragDelta(object? sender, VectorEventArgs e)
    {
        if (IsDetailLayoutRight)
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

    private bool IsDetailLayoutRight =>
        AppSettingsState.Current.BibtexDetailPlacement == BibtexDetailPlacements.Right;

    private void ApplyDetailLayout()
    {
        bool isRight = IsDetailLayoutRight;
        ApplyDetailPlacementClass(MainGrid, isRight);
        ApplyDetailPlacementClass(Splitter, isRight);
        ApplyDetailPlacementClass(DetailPanel, isRight);
        ApplyDetailPlacementClass(BibtexSection, isRight);
        ApplyDetailPlacementClass(RightPath, isRight);
        ApplyDetailPlacementClass(BottomPath, isRight);

        DetailColumnMinWidth = isDetailShowing && isRight ? RightDetailOpenMinWidth : 0;
        DetailColumnMaxWidth = isDetailShowing && isRight ? RightDetailOpenMaxWidth : 0;
        DetailRowMinHeight = isDetailShowing && !isRight ? BottomDetailOpenMinHeight : 0;
        DetailRowMaxHeight = isDetailShowing && !isRight ? BottomDetailOpenMaxHeight : 0;
    }

    private static void ApplyDetailPlacementClass(StyledElement element, bool isRight)
    {
        element.Classes.Set("detail-right", isRight);
        element.Classes.Set("detail-bottom", !isRight);
    }

    private async Task SaveLayoutPreferenceAsync()
    {
        if (TopLevel.GetTopLevel(this) is not Window window
            || window.DataContext is not MainWindowViewModel mainWindowViewModel)
        {
            return;
        }

        await mainWindowViewModel.SaveLocalConfig(window);
    }
}
