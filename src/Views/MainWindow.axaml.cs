using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Platform;
using Avalonia.Platform.Storage;
using Litenbib.Models;
using Litenbib.ViewModels;
using MsBox.Avalonia;
using MsBox.Avalonia.Enums;
using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;

namespace Litenbib.Views
{
    public partial class MainWindow : StyledWindow
    {
        public MainWindow()
        {
            InitializeComponent();
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
            if (DataContext is MainWindowViewModel mwvm)
            {
                if (mwvm.NeedSave)
                {
                    var box = MessageBoxManager.GetMessageBoxStandard(
                        "Warning", "Some files have been edited, but not saved. Do you want to save them?",
                        ButtonEnum.YesNoCancel);
                    var result = await box.ShowAsync();
                    if (result == ButtonResult.Cancel)
                    { return false; }
                    else if (result == ButtonResult.Yes)
                    { await Task.Run(() => mwvm.SaveAllCommand); }
                }
            }
            return true;
        }
    }
}