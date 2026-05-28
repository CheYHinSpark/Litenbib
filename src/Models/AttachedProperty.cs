using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Threading;

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
                bool isFocused = e.NewValue is true;
                tb.SetCurrentValue(IsFocusedProperty, isFocused);
                tb.SetCurrentValue(BindToProperty, isFocused ? tb : null);
            });
        }
    }

    public static class TextBoxUndo
    {
        public static readonly AttachedProperty<object?> ClearOnScopeChangeProperty =
            AvaloniaProperty.RegisterAttached<Control, object?>(
                "ClearOnScopeChange", typeof(TextBoxUndo), defaultBindingMode: BindingMode.OneWay);

        public static object? GetClearOnScopeChange(Control element) =>
            element.GetValue(ClearOnScopeChangeProperty);

        public static void SetClearOnScopeChange(Control element, object? value) =>
            element.SetValue(ClearOnScopeChangeProperty, value);

        static TextBoxUndo()
        {
            ClearOnScopeChangeProperty.Changed.AddClassHandler<TextBox>((textBox, _) =>
            {
                Dispatcher.UIThread.Post(() => ClearUndoStack(textBox), DispatcherPriority.Background);
            });
        }

        private static void ClearUndoStack(TextBox textBox)
        {
            if (!textBox.IsUndoEnabled)
            {
                return;
            }

            textBox.SetCurrentValue(TextBox.IsUndoEnabledProperty, false);
            textBox.SetCurrentValue(TextBox.IsUndoEnabledProperty, true);
        }
    }
}
