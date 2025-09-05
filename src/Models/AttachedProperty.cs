using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Interactivity;
using System.Collections;
using System.Collections.Specialized;

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
}