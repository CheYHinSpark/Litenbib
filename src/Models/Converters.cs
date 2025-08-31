using Avalonia;
using Avalonia.Data.Converters;
using Avalonia.Media;
using System;
using System.Globalization;

namespace Litenbib.Models
{
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
                    //"Article" => SolidColorBrush.Parse("#3d6666"),
                    //"Book" => SolidColorBrush.Parse("#3d5266"),
                    //"Booklet" => SolidColorBrush.Parse("#3d3d66"),
                    //"Conference" => SolidColorBrush.Parse("#523d66"),
                    //"InBook" => SolidColorBrush.Parse("#663d66"),
                    //"InCollection" => SolidColorBrush.Parse("#663d52"),
                    //"InProceedings" => SolidColorBrush.Parse("#663d3d"),
                    //"Manual" => SolidColorBrush.Parse("#66523d"),
                    //"MastersThesis" => SolidColorBrush.Parse("#66663d"),
                    //"Misc" => SolidColorBrush.Parse("#52663d"),
                    //"PhdThesis" => SolidColorBrush.Parse("#3d663d"),
                    //"Proceedings" => SolidColorBrush.Parse("#3d6652"),
                    //"TechReport" => SolidColorBrush.Parse("#525252"),
                    //"Unpublished" => SolidColorBrush.Parse("#3d3d3d"),
                    //_ => Brushes.Transparent, // 默认背景
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

    public class BibTypeConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            // 检查value是否为string类型
            if (value is string text)
            {
                // 这里根据你的逻辑返回不同的颜色
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
                    _ => null, // 默认背景
                };
            }
            // 如果value不是字符串，返回默认颜色
            return Brushes.Transparent;
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            return value;
        }
    }
}