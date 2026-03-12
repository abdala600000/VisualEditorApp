using Avalonia.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using Dock.Model.Controls;
using VisualEditorApp.Models;
using VisualEditorApp.Models.Tools;

namespace VisualEditorApp.ViewModels
{
    public partial class MainWindowViewModel : ObservableObject
    {
        // هذه هي الخاصية التي كانت تسبب الخطأ CS1061
        [ObservableProperty] private DocumentOutlineTool _documentOutline;
        [ObservableProperty] private ToolboxTool _toolbox;
        [ObservableProperty] private PropertiesTool _properties;
        [ObservableProperty] private IRootDock? _layout;

        public MainWindowViewModel()
        {
            // إنشاء الأدوات هنا يضمن وجود نسخة واحدة فقط في البرنامج كله
            DocumentOutline = new DocumentOutlineTool();
            Toolbox = new ToolboxTool { Id = "Toolbox", Title = "Toolbox" };
            Properties = new PropertiesTool { Id = "Properties", Title = "Properties" };
        }
    }
}
