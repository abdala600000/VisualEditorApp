using Avalonia.Controls;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;

namespace VisualEditorApp.Models
{
    public class ElementNode
    {
        public string Header { get; set; } = ""; // اسم الكنترول (مثل Button)
        public Control? RelatedControl { get; set; } // المرجع للكنترول الحقيقي
        public ObservableCollection<ElementNode> Children { get; set; } = new();
    }
}
