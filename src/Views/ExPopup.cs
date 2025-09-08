using Avalonia;
using Avalonia.Animation;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.LogicalTree;
using Avalonia.Platform;
using Avalonia.Styling;
using Avalonia.VisualTree;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Litenbib.Views
{
    public class ExPopup : Popup
    {
        /// <summary> Defines the IsOpenEx property. </summary>
        public static readonly StyledProperty<bool> IsOpenExProperty =
            AvaloniaProperty.Register<ExPopup, bool>(nameof(IsOpenEx), false);

        /// <summary> Gets or sets a value indicating whether the expopup is open. </summary>
        public bool IsOpenEx
        {
            get => GetValue(IsOpenExProperty);
            set => SetCurrentValue(IsOpenExProperty, value);
        }

        public static readonly StyledProperty<Animation> OpenAnimationProperty =
            AvaloniaProperty.Register<ExPopup, Animation>(nameof(OpenAnimation));
        public Animation OpenAnimation
        {
            get => GetValue(OpenAnimationProperty);
            set => SetValue(OpenAnimationProperty, value);
        }
        public static readonly StyledProperty<Animation> CloseAnimationProperty =
            AvaloniaProperty.Register<ExPopup, Animation>(nameof(CloseAnimation));
        public Animation CloseAnimation
        {
            get => GetValue(CloseAnimationProperty);
            set => SetValue(CloseAnimationProperty, value);
        }

        private LightDismissOverlayLayer? dismissLayer;
        protected bool isAnimating;
        static ExPopup()
        {
            IsOpenExProperty.Changed.AddClassHandler<ExPopup>(OnIsOpenExChanged);
            IsLightDismissEnabledProperty.OverrideDefaultValue<ExPopup>(false);
        }

        // 在Popup打开时订阅全局事件
        protected void OnOpen()
        {
            Debug.WriteLine("OnOpening");
            isAnimating = true;
            dismissLayer ??= LightDismissOverlayLayer.GetLightDismissOverlayLayer(TopLevel.GetTopLevel(PlacementTarget)!);
            if (dismissLayer != null)
            {
                dismissLayer.IsVisible = true;
                dismissLayer.InputPassThroughElement = PlacementTarget;
                dismissLayer.PointerPressed += PointerPressedDismissOverlay;
                Debug.WriteLine("Connect");
            }
            if (TopLevel.GetTopLevel(PlacementTarget) is Window window)
            {
                window.Deactivated += Window_Deactivated;
            }
        }

        

        private void Window_Deactivated(object? sender, EventArgs e)
        {
            IsOpen = false;
            IsOpenEx = false;
            if (TopLevel.GetTopLevel(PlacementTarget) is Window window)
            { window.Deactivated -= Window_Deactivated; }
        }

        // 在Popup关闭时取消订阅，防止内存泄漏
        protected void OnClose()
        {
            Debug.WriteLine("OnClosing");
            isAnimating = true;

            if (dismissLayer == null) { return; }
            dismissLayer.PointerPressed -= PointerPressedDismissOverlay;
            dismissLayer.InputPassThroughElement = null;
            dismissLayer.IsVisible = false;
            Debug.WriteLine("Detach");
        }

        private void PointerPressedDismissOverlay(object? sender, PointerPressedEventArgs e)
        {
            Debug.WriteLine("Click On Dismiss");
            if (!isAnimating && IsOpenEx && dismissLayer != null)
            {
                e.Handled = false;
                IsOpenEx = false;
            }
        }

        private static async void OnIsOpenExChanged(ExPopup popup, AvaloniaPropertyChangedEventArgs e)
        {
            if (e.NewValue is not bool b) { return; }
            Debug.WriteLine($"Changing IsOpenEx {b}");
            if (b == true)
            {
                popup.OnOpen();
                popup.IsOpen = true;
                if (popup.OpenAnimation != null)
                { await popup.OpenAnimation.RunAsync(popup.Child!); }
            }
            else
            {
                popup.OnClose();
                if (popup.CloseAnimation != null)
                { await popup.CloseAnimation.RunAsync(popup.Child!); }
                popup.IsOpen = false;
            }
            popup.isAnimating = false;
            Debug.WriteLine($"Changed IsOpenEx {b} {e.Sender}");
        }
    }
}
