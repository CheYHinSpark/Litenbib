using Avalonia.Controls;
using Avalonia.Interactivity;
using System.Threading.Tasks;

namespace Litenbib.Views
{
    public class StyledWindow : Window
    {
        protected async void TitleButton_Click(object? sender, RoutedEventArgs e)
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
                    {
                        if (await OnCloseButtonClicked())
                        { this.Close(false); }
                    }
                    break;
            }
        }

        protected virtual async Task<bool> OnCloseButtonClicked()
        { return await Task.FromResult(true); }
    }
}
