using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Litenbib.ViewModels;
using System.Diagnostics;

namespace Litenbib.Views;

public partial class AddEntryView : StyledWindow
{
    public AddEntryView()
    {
        InitializeComponent();
        this.DataContext = new AddEntryViewModel();
    }

    private void AddButton_Click(object? sender, RoutedEventArgs e)
    { this.Close(true); }

    private void CancelButton_Click(object? sender, RoutedEventArgs e)
    { this.Close(false); }
}