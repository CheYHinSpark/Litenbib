using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Litenbib.ViewModels;

namespace Litenbib.Views;

public partial class BibtexEntryTable : UserControl
{
    public BibtexEntryTable()
    {
        InitializeComponent();
        DataContext = new BibtexEntryViewModel();
    }
}