using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Platform;
using Avalonia.Platform.Storage;
using Avalonia.VisualTree;
using Litenbib.Models;
using Litenbib.ViewModels;
using MsBox.Avalonia;
using MsBox.Avalonia.Enums;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

namespace Litenbib.Views
{
    public partial class MainWindow : StyledWindow
    {
        //private bool _isDragging = false;
        //private BibtexViewModel _draggedItem = null!;
        private MainWindowViewModel _viewModel = null!;

        public MainWindow()
        {
            InitializeComponent();
        }

        protected async override void OnInitialized()
        {
            base.OnInitialized();
            await Task.Delay(1); // 确保界面元素加载完成
            //MainTabControl.AddHandler(PointerMovedEvent, TabControl_PointerMoved, RoutingStrategies.Bubble);
            if (DataContext is MainWindowViewModel mwvm)
            {
                _viewModel = mwvm;
                await _viewModel.OpenFileInit();
            }
        }

        private void TitleBar_PointerPressed(object? sender, PointerPressedEventArgs e)
        {
            TitleBarGrid.IsHitTestVisible = false;
            this.BeginMoveDrag(e);
        }

        private void TitleBar_PointerReleased(object? sender, PointerReleasedEventArgs e)
        { TitleBarGrid.IsHitTestVisible = true; }

        protected override async Task<bool> OnCloseButtonClicked()
        {
            // 检查是否需要保存
            if (_viewModel.NeedSave)
            {
                var box = MessageBoxManager.GetMessageBoxStandard(
                    "Warning", "Some files have been edited, but not saved. Do you want to save them?",
                    ButtonEnum.YesNoCancel);
                var result = await box.ShowAsync();
                if (result == ButtonResult.Cancel)
                { return false; }
                else if (result == ButtonResult.Yes)
                { await Task.Run(() => _viewModel.SaveAllCommand); }
            }
            // 保存最近打开的文件列表到本地配置文件
            _viewModel.SaveLocalConfig();
            return true;
        }

        //private void TabItem_PointerPressed(object? sender, PointerPressedEventArgs e)
        //{
        //    Debug.WriteLine("Start drag TabItem");
        //    if (sender is not Control c || c.DataContext is not BibtexViewModel bvm) { return; }
        //    Debug.WriteLine(c.GetType());
        //    _draggedItem = bvm;
        //    _isDragging = true;
        //    // Capture the pointer to ensure we get events even when outside the control
        //    e.Pointer.Capture(c);
        //}

        //private void TabControl_PointerMoved(object? sender, PointerEventArgs e)
        //{
        //    if (!_isDragging || _draggedItem == null) { return; }

        //    var position = e.GetPosition(MainTabControl);

        //    // Find the TabItem at the current pointer position
        //    var hitTestResult = MainTabControl.GetVisualAt(position);
        //    if (hitTestResult is Visual visual)
        //    //foreach (var visual in hitTestResult)
        //    {
        //        Debug.WriteLine(visual.GetType());
        //        var targetGrid = visual.FindAncestorOfType<TabItem>();
        //        if (targetGrid != null && targetGrid.DataContext is BibtexViewModel targetItem && targetItem != _draggedItem)
        //        {
        //            Debug.WriteLine("Found target TabItem to swap");
        //            //// Found a target tab to swap with
        //            //var sourceIndex = _viewModel.BibtexViewers.IndexOf(_draggedItem);
        //            //var targetIndex = _viewModel.BibtexViewers.IndexOf(targetItem);

        //            //// Reorder the collection
        //            //if (sourceIndex != targetIndex)
        //            //{
        //            //    _viewModel.BibtexViewers.Move(sourceIndex, targetIndex);
        //            //    _viewModel.SelectedFile = _draggedItem; // Keep the dragged item selected
        //            //}

        //            //// Break the loop once we've swapped
        //            ////break;
        //        }
        //    }
        //}

        //private void TabItem_PointerReleased(object? sender, PointerReleasedEventArgs e)
        //{
        //    Debug.WriteLine("Stop drag TabItem");
        //    _isDragging = false;
        //    _draggedItem = null!;
        //    e.Pointer.Capture(null); // Release the pointer capture
        //}
    }
}