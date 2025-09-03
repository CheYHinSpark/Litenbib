using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
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
        private DateTime lastTime = DateTime.Now;
        private readonly LinkedList<IUndoableAction> _undoList = new ();
        private readonly LinkedList<IUndoableAction> _redoList = new ();

        // 检查是否可以撤销
        public bool CanUndo => _undoList.Count != 0;
        // 检查是否可以重做
        public bool CanRedo => _redoList.Count != 0;

        // 添加一个新操作
        public void AddAction(IUndoableAction action)
        {
            // 检查新动作是否与最后的一样
            if (action is EntryChangeAction entrychange 
                && _undoList.Last != null 
                && _undoList.Last.Value is EntryChangeAction lastchange)
            {
                if (EntryChangeAction.IsSameField(lastchange, entrychange) 
                    && (DateTime.Now - lastTime).TotalSeconds < 0.5)
                {
                    lastchange.NewValue = entrychange.NewValue;
                    lastTime.Add((DateTime.Now - lastTime) / 2);
                    return;
                }
            }
            _undoList.AddLast(action);
            if (_undoList.Count > 200)
            { _undoList.RemoveFirst(); }
            _redoList.Clear(); // 添加新操作时，重做历史被清空
            lastTime = DateTime.Now;
            Debug.WriteLine(lastTime);
        }

        // 执行撤销
        public void Undo()
        {
            if (CanUndo && _undoList.Last != null)
            {
                var action = _undoList.Last.Value;
                _undoList.RemoveLast();
                action.Undo();
                _redoList.AddLast(action);
            }
        }

        // 执行重做
        public void Redo()
        {
            if (CanRedo && _redoList.Last != null)
            {
                var action = _redoList.Last.Value;
                _redoList.RemoveLast();
                action.Redo();
                _undoList.AddLast(action);
            }
        }
    }

    public class EntryChangeAction(BibtexEntry entry, string propertyName, string? oldValue, string? newValue) : IUndoableAction
    {
        private readonly BibtexEntry _entry = entry; // 更改的行对象
        private readonly string _propertyName = propertyName; // 更改的属性名
        private readonly string? _oldValue = oldValue;   // 更改前的值
        private string? _newValue = newValue;   // 更改后的值
        public string? NewValue { get => _newValue; set => _newValue = value; }

        public override void Undo()
        { _entry.SetValueSilent(_propertyName, _oldValue); }

        public override void Redo()
        { _entry.SetValueSilent(_propertyName, _newValue); }

        public static bool IsSameField(EntryChangeAction oldAction, EntryChangeAction newAction)
        {
            if (oldAction._entry == newAction._entry 
                && oldAction._propertyName != "Type")
            {
                return oldAction._propertyName == newAction._propertyName 
                    && oldAction._oldValue != newAction._newValue;
            }
            return false;
        }
    }
}
