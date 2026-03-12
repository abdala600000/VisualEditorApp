using Avalonia.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using Dock.Model.Mvvm.Controls;
using System.Collections.ObjectModel;
using System.Linq;

namespace VisualEditorApp.Models.Tools
{
    public partial class ToolboxTool : Tool
    {
        [ObservableProperty]
        private ObservableCollection<ToolboxItem> _items = new();

        public ToolboxTool()
        {
            Id = "Toolbox";
            Title = "Toolbox";

            LoadAllAvaloniaControls();
        }

        private void LoadAllAvaloniaControls()
        {
            // 1. تحديد الـ Assembly اللي جواه الكنترولات بتاعة أفلونيا
            var controlAssembly = typeof(Control).Assembly;

            // 2. البحث عن كل الكلاسات اللي بتورث من Control ومش Abstract
            var allControls = controlAssembly.GetTypes()
                .Where(t => t.IsPublic && !t.IsAbstract && typeof(Control).IsAssignableFrom(t))
                .OrderBy(t => t.Name)
                .ToList();

            var tempList = new ObservableCollection<ToolboxItem>();

            foreach (var type in allControls)
            {
                tempList.Add(new ToolboxItem
                {
                    Name = type.Name,
                    ControlType = type,
                    // لو حبيت ممكن تحط أيقونة افتراضية لكل الكنترولات أو تعمل سويتش لبعضهم
                    IconPath = "M4,6H20V16H4V6M20,4H4C2.9,4 2,4.9 2,6V16C2,17.1 2.9,18 4,18H20C21.1,18 22,17.1 22,16V6C22,4.9 21.1,4 20,4M18,14H6V12H18V14Z"
                });
            }

            Items = tempList;
        }
    }
    public class PropertiesTool : Tool { 
    
    
    
    }

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

            
        }

    }
}
