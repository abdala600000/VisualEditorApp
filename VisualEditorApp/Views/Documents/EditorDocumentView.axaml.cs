using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using VisualEditorApp.ViewModels.Documents;
using VisualEditor.CodeEditor;

namespace VisualEditorApp.Views.Documents
{
    public partial class EditorDocumentView : UserControl
    {
        public EditorDocumentView()
        {
            AvaloniaXamlLoader.Load(this);
            DataContextChanged += (s, e) =>
            {
                if (DataContext is EditorDocumentViewModel vm)
                {
                    var smartEditor = this.FindControl<SmartEditorView>("SmartEditor");
                    if (smartEditor != null)
                    {
                        smartEditor.SetXamlText(vm.Text);
                        smartEditor.SetHighlighting(vm.Extension);
                    }
                }
            };
        }
    }
}
