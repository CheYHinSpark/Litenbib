using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Platform;
using Litenbib.Models;
using System;
using System.Globalization;
using System.Threading.Tasks;

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
            if (e.Property == WindowStateProperty)
            {
                switch (e.NewValue)
                {
                    case null:
                        return;
                    case WindowState.Maximized:
                        break;
                }
            }
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

        private void TitleBar_PointerPressed(object? sender, PointerPressedEventArgs e)
        {
            this.BeginMoveDrag(e);
        }

        private void Button_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {

            _ = Search();
        }

        private async Task Search()
        {
            var doi = "10.48550/arXiv.2406.13931";
            var result = await DoiResolver.ResolveDoiOfficialAsync(doi);
            testblock.Text = result;
           //{ testblock.Text = "1231245"; }
        }

    }
}