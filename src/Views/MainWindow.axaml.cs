using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Platform;
using Avalonia.Platform.Storage;
using Litenbib.Models;
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
            this.BeginMoveDrag(e);
        }
    }
}