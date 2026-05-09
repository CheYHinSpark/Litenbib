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
    public sealed class ToastNotification(string message, string type, TimeSpan displayTime) : INotifyPropertyChanged
    {
        public string Message { get; } = message;

        public string Type { get; } = type;

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
        public static ObservableCollection<ToastNotification> Messages { get; } = [];

        public static void Info(string message)
        { Show(message, "info", TimeSpan.FromSeconds(3)); }

        public static void Error(string message)
        { Show(message, "error", TimeSpan.FromSeconds(6)); }

        public static void Dismiss(ToastNotification? notification)
        {
            if (notification == null) { return; }

            if (Dispatcher.UIThread.CheckAccess())
            { Messages.Remove(notification); }
            else
            { Dispatcher.UIThread.Post(() => Messages.Remove(notification)); }
        }

        private static void Show(string message, string type, TimeSpan displayTime)
        {
            if (string.IsNullOrWhiteSpace(message)) { return; }

            ToastNotification notification = new(message.Trim(), type, displayTime);
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
