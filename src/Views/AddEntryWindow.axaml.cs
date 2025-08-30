using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Litenbib.ViewModels;
using System.Diagnostics;

namespace Litenbib.Views;

public partial class AddEntryWindow : StyledWindow
{
    public AddEntryWindow()
    {
        InitializeComponent();
        this.DataContext = new AddEntryViewModel();
    }

    private void AddButton_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    { this.Close(true); }

    private void CancelButton_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    { this.Close(false); }
}