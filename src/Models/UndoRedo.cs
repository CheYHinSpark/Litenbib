using Avalonia.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Litenbib.Models
{
    public abstract class IUndoableAction
    {
        // 撤销操作
        public abstract void Undo();
        // 重做操作
        public abstract void Redo();
    }

    public class UndoRedoManager
    {
        private int savedStep = 0;
        public bool Edited { get => _undoList.Count != savedStep; set => savedStep = _undoList.Count; }
        private DateTime lastTime = DateTime.Now;
        private readonly LinkedList<IUndoableAction> _undoList = new();
        private readonly LinkedList<IUndoableAction> _redoList = new();
        /// <summary> 上个修改的Box </summary>
        public TextBox? LastEditedBox { get; set; }
        /// <summary> 当前修改的Box </summary>
        public TextBox? NewEditedBox { get; set; }

        // 检查是否可以撤销
        public bool CanUndo => _undoList.Count != 0;
        // 检查是否可以重做
        public bool CanRedo => _redoList.Count != 0;

        // 添加一个新操作
        public void AddAction(IUndoableAction action)
        {
            // 检查新编辑动作是否与最后的一样
            if (action is EntryChangeAction entrychange
                && _undoList.Last != null
                && _undoList.Last.Value is EntryChangeAction lastchange)
            {
                if (EntryChangeAction.IsSameField(lastchange, entrychange)
                    && (DateTime.Now - lastTime).TotalSeconds < 0.2)
                {
                    lastchange.NewValue = entrychange.NewValue;
                    lastchange.NewIndex = entrychange.NewIndex;
                    lastTime = DateTime.Now;
                    return;
                }
            }
            _undoList.AddLast(action);
            if (_undoList.Count > 100) // 最大限制100条
            {
                _undoList.RemoveFirst();
                --savedStep;
            }
            _redoList.Clear(); // 添加新操作时，重做历史被清空
            lastTime = DateTime.Now;
            LastEditedBox = NewEditedBox;
            if (savedStep >= _undoList.Count)
            { savedStep = -1; }
        }

        // 执行撤销
        public async void Undo()
        {
            if (CanUndo && _undoList.Last != null)
            {
                var action = _undoList.Last.Value;
                _undoList.RemoveLast();
                action.Undo();
                _redoList.AddLast(action);
                if (action is EntryChangeAction entrychange
                    && LastEditedBox != null
                    && LastEditedBox == NewEditedBox)
                {
                    await Task.Delay(1);
                    LastEditedBox.SelectionStart = entrychange.OldIndex;
                    LastEditedBox.SelectionEnd = entrychange.OldIndex;
                }
            }
        }

        // 执行重做
        public async void Redo()
        {
            if (CanRedo && _redoList.Last != null)
            {
                var action = _redoList.Last.Value;
                _redoList.RemoveLast();
                action.Redo();
                _undoList.AddLast(action);
                if (action is EntryChangeAction entrychange
                    && LastEditedBox != null
                    && LastEditedBox == NewEditedBox)
                {
                    await Task.Delay(1);
                    LastEditedBox.SelectionStart = entrychange.NewIndex;
                    LastEditedBox.SelectionEnd = entrychange.NewIndex;
                }
            }
        }
    }

    public class EntryChangeAction(BibtexEntry entry, string propertyName,
        string? oldValue, string? newValue, int oldIndex = 0, int newIndex = 0) : IUndoableAction
    {
        private readonly BibtexEntry _entry = entry; // 更改的行对象
        private readonly string _propertyName = propertyName; // 更改的属性名
        private readonly string? _oldValue = oldValue;   // 更改前的值
        public string? NewValue = newValue; // 更改后的值
        public int OldIndex = oldIndex;
        public int NewIndex = newIndex;

        public override void Undo()
        { _entry.SetValueSilent(_propertyName, _oldValue); }

        public override void Redo()
        { _entry.SetValueSilent(_propertyName, NewValue); }

        public static bool IsSameField(EntryChangeAction oldAction, EntryChangeAction newAction)
        {
            if (oldAction._entry == newAction._entry
                && oldAction._propertyName != "Type")
            {
                return oldAction._propertyName == newAction._propertyName
                    && oldAction._oldValue != newAction.NewValue;
            }
            return false;
        }
    }

    public class AddEntriesAction(ObservableRangeCollection<BibtexEntry> holder,
        List<(int, BibtexEntry)> index_entries) : IUndoableAction
    {
        private readonly ObservableRangeCollection<BibtexEntry> _holder = holder;
        private readonly List<(int, BibtexEntry)> _index_entries = index_entries;

        public override void Undo()
        { _holder.RemoveRange(_index_entries.Select(t => t.Item2)); }

        public override void Redo()
        { _holder.InsertRange(_index_entries); }
    }

    public class DeleteEntriesAction(ObservableRangeCollection<BibtexEntry> holder,
        List<(int, BibtexEntry)> index_entries) : IUndoableAction
    {
        private readonly ObservableRangeCollection<BibtexEntry> _holder = holder;
        private readonly List<(int, BibtexEntry)> _index_entries = index_entries;

        public override void Undo()
        { _holder.InsertRange(_index_entries); }

        public override void Redo()
        { _holder.RemoveRange(_index_entries.Select(t => t.Item2)); }
    }

    public class ReplaceEntriesAction(ObservableRangeCollection<BibtexEntry> holder,
        List<(int, BibtexEntry)> old_index_entries, List<(int, BibtexEntry)> new_index_entries) : IUndoableAction
    {
        private readonly AddEntriesAction _addAction = new(holder, new_index_entries);
        private readonly DeleteEntriesAction _deleteAction = new(holder, old_index_entries);
        public override void Undo()
        {
            _addAction.Undo();
            _deleteAction.Undo();
        }
        public override void Redo()
        {
            _deleteAction.Redo();
            _addAction.Redo();
        }
    }
}
