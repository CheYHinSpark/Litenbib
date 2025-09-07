using Avalonia;
using Avalonia.Animation;
using Avalonia.Animation.Easings;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Styling;
using System;
using System.Collections;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace Litenbib.Models
{
    public static class FocusEx
    {
        // 定义附加属性
        public static readonly AttachedProperty<bool> IsFocusedProperty =
            AvaloniaProperty.RegisterAttached<Control, bool>(
                "IsFocused", typeof(FocusEx), default, false, BindingMode.OneWayToSource);

        // Getter
        public static bool GetIsFocused(AvaloniaObject element) =>
            element.GetValue(IsFocusedProperty);

        // Setter
        public static void SetIsFocused(AvaloniaObject element, bool value) =>
            element.SetValue(IsFocusedProperty, value);

        public static readonly AttachedProperty<object?> BindToProperty =
            AvaloniaProperty.RegisterAttached<Control, object?>(
                "BindTo", typeof(FocusEx), defaultBindingMode: BindingMode.OneWayToSource);
        
        public static object? GetBindTo(Control element) =>
            element.GetValue(BindToProperty);

        public static void SetBindTo(Control element, object? value) =>
            element.SetValue(BindToProperty, value);

        static FocusEx()
        {
            // 当附加属性第一次被用到时，监听 TextBox 的 IsFocused 变化
            IsFocusedProperty.Changed.AddClassHandler<TextBox>((tb, e) =>
            {
                // 不需要写逻辑，这个是给 XAML 双向绑定时用的
            });

            // 监听 TextBox 自身的 IsFocused 属性变化，并同步到附加属性
            TextBox.IsFocusedProperty.Changed.AddClassHandler<TextBox>((tb, e) =>
            {
                tb.SetValue(IsFocusedProperty, e.NewValue);
                if ((bool?)e.NewValue == true)
                { tb.SetValue(BindToProperty, tb); }
                else
                { tb.SetValue(BindToProperty, null); }
            });
        }
    }

    //public static class PopupIsOpenEx
    //{
    //    public static readonly AttachedProperty<bool> IsOpenExProperty =
    //        AvaloniaProperty.RegisterAttached<Popup, bool>("IsOpenEx", typeof(PopupIsOpenEx), false);


    //    // Getter
    //    public static bool GetIsOpenEx(Popup popup) =>
    //        popup.GetValue(IsOpenExProperty);

    //    // Setter
    //    public static void SetIsOpenEx(Popup popup, bool value) =>
    //        popup.SetValue(IsOpenExProperty, value);

    //    static PopupIsOpenEx()
    //    {
    //        IsOpenExProperty.Changed.AddClassHandler<Popup>((popup, e) =>
    //        {
    //            OnIsOpenExChanged(e);
    //        });
    //    }

    //    private static async void OnIsOpenExChanged(AvaloniaPropertyChangedEventArgs e)
    //    {
    //        if (e.Sender is Popup popup && e.NewValue is bool b)
    //        {
    //            if (b)
    //            {
    //                popup.IsOpen = true;
    //                // 播放打开动画
    //                if (popup.Child is Control child)
    //                {
    //                    Debug.WriteLine(b);
    //                    // 播放关闭动画
    //                    var animation = new Animation
    //                    {
    //                        Duration = TimeSpan.FromMilliseconds(2000),
    //                        Children =
    //                        {
    //                            new KeyFrame
    //                            {
    //                                Cue = new Cue(0),
    //                                Setters =
    //                                {
    //                                    new Setter(Visual.OpacityProperty, 0.0),
    //                                }
    //                            },
    //                            new KeyFrame
    //                            {
    //                                Cue = new Cue(1),
    //                                Setters =
    //                                {
    //                                    new Setter(Visual.OpacityProperty, 1.0),
    //                                }
    //                            }
    //                        }
    //                    };
    //                    await animation.RunAsync(child, CancellationToken.None);
    //                }
    //            }
    //            else
    //            {
    //                if (popup.Child is Control child)
    //                {
    //                    Debug.WriteLine(b);
    //                    // 播放关闭动画
    //                    var animation = new Animation
    //                    {
    //                        Duration = TimeSpan.FromMilliseconds(2000),
    //                        Children =
    //                        {
    //                            new KeyFrame
    //                            {
    //                                Cue = new Cue(0),
    //                                Setters =
    //                                {
    //                                    new Setter(Visual.OpacityProperty, 1.0),
    //                                }
    //                            },
    //                            new KeyFrame
    //                            {
    //                                Cue = new Cue(1),
    //                                Setters =
    //                                {
    //                                    new Setter(Visual.OpacityProperty, 0.0),
    //                                }
    //                            }
    //                        }
    //                    };
    //                    await animation.RunAsync(child, CancellationToken.None);
    //                }
    //                popup.IsOpen = false; // 动画结束后再真正关闭
    //            }
    //        }
    //    }
    //}
}