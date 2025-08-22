using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Platform;
using System;
using System.Globalization;

namespace Litenbib.Views
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            this.PropertyChanged += OnPropertyChanged;
        }

        private void OnPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
        {
        
        }

        private void TitleButton_Click(object? sender, RoutedEventArgs e)
        {
            if (e.Source is not Button button) { return; }
            switch (button.Name)
            {
                case null:
                    return;
                case "MinButton":
                    { WindowState = WindowState.Minimized; }
                    break;
                case "MaxButton":
                    {
                        if (WindowState == WindowState.Maximized)
                        { WindowState = WindowState.Normal; }
                        else
                        { WindowState = WindowState.Maximized; }
                    }
                    break;
                case "CloseButton":
                    { Close(); }
                    break;
            }
        }

        private void TitleBar_PointerPressed(object? sender, Avalonia.Input.PointerPressedEventArgs e)
        {
            this.BeginMoveDrag(e);
        }
    }
}