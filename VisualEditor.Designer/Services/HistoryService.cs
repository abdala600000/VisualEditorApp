using System;
using System.Collections.Generic;
using System.Text;

namespace VisualEditor.Designer.Services
{
    public class HistoryService
    {
        public static HistoryService Instance { get; } = new HistoryService();

        // 🎯 المكدس الأول: للحركات اللي نقدر نتراجع عنها (Undo)
        private readonly Stack<(Action UndoAction, Action RedoAction)> _undoStack = new();

        // 🎯 المكدس الثاني: للحركات اللي تراجعنا عنها وعاوزين نرجعها تاني (Redo)
        private readonly Stack<(Action UndoAction, Action RedoAction)> _redoStack = new();

        private HistoryService() { }

        // تسجيل حركة جديدة
        public void RegisterChange(Action undo, Action redo)
        {
            _undoStack.Push((undo, redo));
            _redoStack.Clear(); // أول ما تعمل حركة جديدة، بنمسح الـ Redo القديم
        }

        public void Undo()
        {
            if (_undoStack.Count > 0)
            {
                var action = _undoStack.Pop();
                action.UndoAction(); // 🚀 نفذ التراجع
                _redoStack.Push(action);
            }
        }

        public void Redo()
        {
            if (_redoStack.Count > 0)
            {
                var action = _redoStack.Pop();
                action.RedoAction(); // 🚀 نفذ الإعادة
                _undoStack.Push(action);
            }
        }

        public void Clear()
        {
            _undoStack.Clear();
            _redoStack.Clear();
        }
    }
}
