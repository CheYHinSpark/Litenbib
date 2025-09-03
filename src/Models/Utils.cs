using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Litenbib.Models
{
    public class PropertyChangedEventArgsEx(string? propertyName, object? oldValue, object? newValue) : PropertyChangedEventArgs(propertyName)
    {
        public object? OldValue { get; } = oldValue;
        public object? NewValue { get; } = newValue;
    }

    internal class BibFile
    {
        public static string Read(string path)
        {
            return File.ReadAllText(path);
        }

        public static void Write(string path, string content)
        {

        }
    }
}
