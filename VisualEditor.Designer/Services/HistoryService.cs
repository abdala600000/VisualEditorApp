using System;
using System.Collections.Generic;

namespace VisualEditor.Designer.Services
{
    public class HistoryService
    {
        public static HistoryService Instance { get; } = new HistoryService();

        private const int MaxHistory = 50;

        private readonly LinkedList<(Action UndoAction, Action RedoAction)> _undoStack = new();
        private readonly LinkedList<(Action UndoAction, Action RedoAction)> _redoStack = new();

        private HistoryService() { }

        public void RegisterChange(Action undo, Action redo)
        {
            _undoStack.AddLast((undo, redo));
            // حذف الأقدم إذا تجاوز الحد الأقصى
            if (_undoStack.Count > MaxHistory)
                _undoStack.RemoveFirst();
            _redoStack.Clear();
        }

        public void Undo()
        {
            if (_undoStack.Count > 0)
            {
                var action = _undoStack.Last!.Value;
                _undoStack.RemoveLast();
                action.UndoAction();
                _redoStack.AddLast(action);
                // حذف الأقدم من Redo أيضاً إذا تجاوز الحد
                if (_redoStack.Count > MaxHistory)
                    _redoStack.RemoveFirst();
            }
        }

        public void Redo()
        {
            if (_redoStack.Count > 0)
            {
                var action = _redoStack.Last!.Value;
                _redoStack.RemoveLast();
                action.RedoAction();
                _undoStack.AddLast(action);
                if (_undoStack.Count > MaxHistory)
                    _undoStack.RemoveFirst();
            }
        }

        public void Clear()
        {
            _undoStack.Clear();
            _redoStack.Clear();
        }
    }
}
