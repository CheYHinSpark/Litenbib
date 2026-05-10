using Avalonia;
using Avalonia.Animation;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
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

        private TopLevel? _dismissTopLevel;
        private Window? _dismissWindow;
        private bool _dismissPending;
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
            AttachDismissHandlers();


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
            DetachDismissHandlers();

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
            DetachDismissHandlers();
        }

        private void AttachDismissHandlers()
        {
            var target = PlacementTarget ?? Child;
            if (target == null)
            {
                return;
            }

            var topLevel = TopLevel.GetTopLevel(target);
            if (topLevel == null)
            {
                return;
            }

            if (_dismissTopLevel != topLevel)
            {
                _dismissTopLevel?.RemoveHandler(InputElement.PointerPressedEvent, TopLevel_PointerPressed);
                _dismissTopLevel = topLevel;
                _dismissTopLevel.AddHandler(
                    InputElement.PointerPressedEvent,
                    TopLevel_PointerPressed,
                    RoutingStrategies.Tunnel,
                    handledEventsToo: true);
            }

            if (topLevel is Window window && _dismissWindow != window)
            {
                if (_dismissWindow != null)
                {
                    _dismissWindow.Deactivated -= Window_Deactivated;
                }

                _dismissWindow = window;
                _dismissWindow.Deactivated += Window_Deactivated;
            }
        }

        private void DetachDismissHandlers()
        {
            _dismissTopLevel?.RemoveHandler(InputElement.PointerPressedEvent, TopLevel_PointerPressed);
            _dismissTopLevel = null;

            if (_dismissWindow != null)
            {
                _dismissWindow.Deactivated -= Window_Deactivated;
                _dismissWindow = null;
            }

            _dismissPending = false;
        }

        private void TopLevel_PointerPressed(object? sender, PointerPressedEventArgs e)
        {
            if (isAnimating || !IsOpenEx || IsInsidePopupChild(e.Source as Visual))
            {
                return;
            }

            DismissAfterCurrentInput();
        }

        private bool IsInsidePopupChild(Visual? visual)
        {
            return visual != null &&
                   Child is Visual child &&
                   (visual == child || child.IsVisualAncestorOf(visual));
        }

        private void DismissAfterCurrentInput()
        {
            if (_dismissPending)
            {
                return;
            }

            _dismissPending = true;
            Dispatcher.UIThread.Post(() =>
            {
                _dismissPending = false;
                if (!isAnimating && IsOpenEx)
                {
                    Debug.Write("From TopLevelDismiss ");
                    IsOpenEx = false;
                }
            }, DispatcherPriority.Background);
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
