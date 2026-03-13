using Avalonia.Controls;
using System.Collections.ObjectModel;

namespace VisualEditor.Toolbox.Outline
{
    public class ElementNode
    {
        public string Header { get; set; } = ""; // اسم الكنترول (مثل Button)
        public Control? RelatedControl { get; set; } // المرجع للكنترول الحقيقي
        public ObservableCollection<ElementNode> Children { get; set; } = new();
    }
}
