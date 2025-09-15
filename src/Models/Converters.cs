using Avalonia;
using Avalonia.Controls;
using Avalonia.Data.Converters;
using Avalonia.Markup.Xaml.MarkupExtensions;
using Avalonia.Media;
using Litenbib.ViewModels;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;

namespace Litenbib.Models
{
    public class BibTypeConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            // 检查value是否为string类型
            if (value is string text)
            {
                return text.ToLower() switch
                {
                    "article" => "Article",
                    "book" => "Book",
                    "booklet" => "Booklet",
                    "conference" => "Conference",
                    "inbook" => "InBook",
                    "incollection" => "InCollection",
                    "inproceedings" => "InProceedings",
                    "manual" => "Manual",
                    "mastersthesis" => "MastersThesis",
                    "misc" => "Misc",
                    "phdthesis" => "PhdThesis",
                    "proceedings" => "Proceedings",
                    "techreport" => "TechReport",
                    "unpublished" => "Unpublished",
                    _ => null,
                };
            }
            return null;
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        { return value is string text ? text.ToLower() : value; }
    }

    public class FilterModeConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (parameter is string s)
            { return s == "0"; }
            return false;
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is not bool b || parameter is not string s) { return -1; }
            return b ? s : -1;
        }
    }

    public class StringSelectorConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value == null && parameter == null) { return ""; }
            // value 是绑定的第一个字符串，也就是 a
            var primaryString = value as string;
            // parameter 是绑定的第二个字符串，也就是 b
            var fallbackString = parameter as string;
            // 如果 a 非空且不为空白，则返回 a，否则返回 b
            return string.IsNullOrWhiteSpace(primaryString) ? fallbackString : primaryString;
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        { throw new NotSupportedException(); }
    }

    public class TextToBackgroundConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            // 检查value是否为string类型
            if (value is string text)
            {
                // 这里根据你的逻辑返回不同的颜色
                return text.ToLower() switch
                {
                    "article" => SolidColorBrush.Parse("#3d6666"),
                    "book" => SolidColorBrush.Parse("#3d5266"),
                    "booklet" => SolidColorBrush.Parse("#3d3d66"),
                    "conference" => SolidColorBrush.Parse("#523d66"),
                    "inbook" => SolidColorBrush.Parse("#663d66"),
                    "incollection" => SolidColorBrush.Parse("#663d52"),
                    "inproceedings" => SolidColorBrush.Parse("#663d3d"),
                    "manual" => SolidColorBrush.Parse("#66523d"),
                    "mastersthesis" => SolidColorBrush.Parse("#66663d"),
                    "misc" => SolidColorBrush.Parse("#52663d"),
                    "phdthesis" => SolidColorBrush.Parse("#3d663d"),
                    "proceedings" => SolidColorBrush.Parse("#3d6652"),
                    "techreport" => SolidColorBrush.Parse("#525252"),
                    "unpublished" => SolidColorBrush.Parse("#3d3d3d"),
                    _ => Brushes.Transparent, // 默认背景
                };
            }
            // 如果value不是字符串，返回默认颜色
            return Brushes.Transparent;
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            // 双向绑定可能需要实现，这里不需要，直接抛出异常或返回null
            throw new NotSupportedException();
        }
    }

    public class WarningToBrushConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            // 检查value是否为string类型
            if (value is int i)
            {
                // 这里根据你的逻辑返回不同的颜色
                return i switch
                {
                    0 => Brushes.Transparent,
                    > 0 => SolidColorBrush.Parse("#cc2929"),
                    < 0 => SolidColorBrush.Parse("#cca329")
                };
            }
            // 如果value不是字符串，返回默认颜色
            return Brushes.Transparent;
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            // 双向绑定可能需要实现，这里不需要，直接抛出异常或返回null
            throw new NotSupportedException();
        }
    }

    public class EntryTypeToVisibleConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (parameter is not string list || value is not string s) { return false; }
            var items = list.Split(',');
            if (items[0] == "_")
            {
                foreach (var item in items)
                { if (s.Equals(item, StringComparison.CurrentCultureIgnoreCase)) { return false; } }
                return true;
            }
            else
            {
                foreach (var item in items)
                { if (s.Equals(item, StringComparison.CurrentCultureIgnoreCase)) { return true; } }
                return false;
            }
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            // 双向绑定可能需要实现，这里不需要，直接抛出异常或返回null
            throw new NotSupportedException();
        }
    }

    public class WindowStateToPathConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            // 检查value是否为string类型
            if (value is WindowState)
            {
                // 这里根据你的逻辑返回不同的颜色
                return value switch
                {
                    WindowState.Maximized => PathGeometry.Parse("M 0,2 L 8,2 8,9 0,9 Z M 2,0 L 10,0 10,7"),
                    _ => PathGeometry.Parse("M 0,0 L 9,0 9,8 0,8 Z"), // 默认背景
                };
            }
            // 如果value不是字符串，返回默认颜色
            return Brushes.Transparent;
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        { throw new NotSupportedException(); }
    }
}