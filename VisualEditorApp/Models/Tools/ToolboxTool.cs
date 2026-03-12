using CommunityToolkit.Mvvm.ComponentModel;
using Dock.Model.Mvvm.Controls;
using System.Collections.ObjectModel;

namespace VisualEditorApp.Models.Tools
{
    public class ToolboxTool : Tool
    {
    }
    public class PropertiesTool : Tool { }

    public class SettingsTool : Tool { }

    public partial class DocumentOutlineTool : Tool
    {
        
        // أضف هذا السطر ليكون هو المرجع الوحيد في البرنامج كله
        public static DocumentOutlineTool Instance { get; } = new DocumentOutlineTool();

        [ObservableProperty]
        private ObservableCollection<ElementNode> nodes = new();

        public DocumentOutlineTool()
        {
            Id = "DocumentOutline";
            Title = "Document Outline";

            var testNode = new ElementNode { Header = "Test Root" };
            testNode.Children.Add(new ElementNode { Header = "Child Element 1" });
            testNode.Children.Add(new ElementNode { Header = "Child Element 2" });

            Nodes.Add(testNode);
        }

    }
}
