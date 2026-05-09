using Avalonia.Controls.Notifications;
using Avalonia.Media;
using Avalonia.Threading;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace Litenbib.Models
{
    public sealed class ToastNotification(string message, IBrush background, IBrush foreground, TimeSpan displayTime) : INotifyPropertyChanged
    {
        public string Message { get; } = message;

        public IBrush Background { get; } = background;

        public IBrush Foreground { get; } = foreground;

        public TimeSpan DisplayTime { get; } = displayTime;

        private double _opacity = 1;
        public double Opacity
        {
            get => _opacity;
            set { _opacity = value; OnPropertyChanged(); }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    public static class NotificationCenter
    {
        private static readonly IBrush InfoBackground = Brushes.White;
        private static readonly IBrush InfoForeground = SolidColorBrush.Parse("#111827");
        private static readonly IBrush ErrorBackground = SolidColorBrush.Parse("#cc2929");
        private static readonly IBrush ErrorForeground = Brushes.White;

        public static ObservableCollection<ToastNotification> Messages { get; } = [];

        public static void Info(string message)
        { Show(message, InfoBackground, InfoForeground, TimeSpan.FromSeconds(3)); }

        public static void Error(string message)
        { Show(message, ErrorBackground, ErrorForeground, TimeSpan.FromSeconds(6)); }

        public static void Dismiss(ToastNotification? notification)
        {
            if (notification == null) { return; }

            if (Dispatcher.UIThread.CheckAccess())
            { Messages.Remove(notification); }
            else
            { Dispatcher.UIThread.Post(() => Messages.Remove(notification)); }
        }

        private static void Show(string message, IBrush background, IBrush foreground, TimeSpan displayTime)
        {
            if (string.IsNullOrWhiteSpace(message)) { return; }

            ToastNotification notification = new(message.Trim(), background, foreground, displayTime);
            if (Dispatcher.UIThread.CheckAccess())
            { Messages.Add(notification); }
            else
            { Dispatcher.UIThread.Post(() => Messages.Add(notification)); }

            _ = DismissLaterAsync(notification);
        }

        private static async Task DismissLaterAsync(ToastNotification notification)
        {
            await Task.Delay(notification.DisplayTime);

            await Dispatcher.UIThread.InvokeAsync(() => notification.Opacity = 0);
            await Task.Delay(400);

            await Dispatcher.UIThread.InvokeAsync(() => Messages.Remove(notification));
        }
    }
}
