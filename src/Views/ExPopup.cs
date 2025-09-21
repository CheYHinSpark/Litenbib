using Avalonia;
using Avalonia.Animation;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.VisualTree;
using System;
using System.Diagnostics;
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
            set => SetCurrentValue(IsOpenExProperty, value);    // SetCurrentValue
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
            IsLightDismissEnabledProperty.OverrideDefaultValue<ExPopup>(false);
            IsOpenExProperty.Changed.AddClassHandler<ExPopup>(OnIsOpenExChanged);
        }

        // 在Popup打开时订阅全局事件
        protected async Task OnOpen()
        {
            OverlayDismissEventPassThrough = true;
            isAnimating = true;
            if (TopLevel.GetTopLevel(PlacementTarget) is Visual v)
            { dismissLayer ??= LightDismissOverlayLayer.GetLightDismissOverlayLayer(v); }
            if (dismissLayer != null)
            {
                dismissLayer.IsVisible = true;
                dismissLayer.InputPassThroughElement = PlacementTarget;
                dismissLayer.PointerPressed += PointerPressedDismissOverlay;
            }
            if (TopLevel.GetTopLevel(PlacementTarget) is Window window)
            {
                window.Deactivated += Window_Deactivated;
            }


            IsOpen = true;
            if (OpenAnimation != null)
            {
                await OpenAnimation.RunAsync(Child!);
            }
        }

        // 在Popup关闭时取消订阅，防止内存泄漏
        protected async Task OnClose()
        {
            isAnimating = true;
            if (dismissLayer != null)
            {
                dismissLayer.PointerPressed -= PointerPressedDismissOverlay;
                dismissLayer.InputPassThroughElement = null;
                dismissLayer.IsVisible = false;
            }

            if (CloseAnimation != null)
            {
                await CloseAnimation.RunAsync(Child!);
            }
            IsOpen = false;
        }

        private void Window_Deactivated(object? sender, EventArgs e)
        {
            IsOpen = false;
            Debug.Write("From Window ");
            IsOpenEx = false;
            if (TopLevel.GetTopLevel(PlacementTarget) is Window window)
            { window.Deactivated -= Window_Deactivated; }
        }

        private void PointerPressedDismissOverlay(object? sender, PointerPressedEventArgs e)
        {
            if (!isAnimating && IsOpenEx && dismissLayer != null)
            {
                if (OverlayDismissEventPassThrough)
                { PassThroughEvent(e); }
                Debug.Write("From OverlayDismiss ");
                IsOpenEx = false;
            }
        }

        private static void PassThroughEvent(PointerPressedEventArgs e)
        {
            if (e.Source is LightDismissOverlayLayer layer &&
                layer.GetVisualRoot() is InputElement root)
            {
                var p = e.GetCurrentPoint(root);
                var hit = root.InputHitTest(p.Position, x => x != layer);

                if (hit != null)
                {
                    e.Pointer.Capture(hit);
                    hit.RaiseEvent(e);
                    e.Handled = true;
                }
            }
        }

        private static async void OnIsOpenExChanged(ExPopup popup, AvaloniaPropertyChangedEventArgs e)
        {
            if (e.NewValue is not bool b) { return; }
            Debug.WriteLine($"Set IsOpenEx {b}");
            if (b == true)
            { await popup.OnOpen(); }
            else
            { await popup.OnClose(); }
            popup.isAnimating = false;
        }
    }
}
