using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
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

    public class ObservableRangeCollection<T> : ObservableCollection<T>
    {
        // 默认构造函数
        public ObservableRangeCollection() : base() { }

        // 新增的构造函数，用于从现有列表初始化
        public ObservableRangeCollection(IEnumerable<T> list) : base(list) { }

        public void AddRange(IEnumerable<T> items)
        {
            ArgumentNullException.ThrowIfNull(items);

            var startIndex = Count;

            foreach (var item in items)
            { Items.Add(item); }

            OnCollectionChanged(new NotifyCollectionChangedEventArgs(
                NotifyCollectionChangedAction.Add, items.ToList(), startIndex)); // 必须ToList()才能正确运行
        }

        public void InsertRange(IEnumerable<(int, T)> index_items)
        {
            ArgumentNullException.ThrowIfNull(index_items);

            foreach (var item in index_items)
            {
                Items.Insert(item.Item1, item.Item2);
                OnCollectionChanged(new NotifyCollectionChangedEventArgs(
                    NotifyCollectionChangedAction.Add, item.Item2, item.Item1));
            }
        }

        public void RemoveRange(IEnumerable<T> items)
        {
            ArgumentNullException.ThrowIfNull(items);

            foreach (var item in items)
            { Items.Remove(item); }

            OnCollectionChanged(new NotifyCollectionChangedEventArgs(
                NotifyCollectionChangedAction.Remove, items.ToList(), 0)); // Avalonia 会自己更新索引
        }
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
