using Avalonia.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using Dock.Model.Controls;
using Dock.Model.Mvvm.Controls;
using System.Linq;
using VisualEditorApp.Models;
using VisualEditorApp.Models.Tools;
using static VisualEditorApp.ViewModels.SolutionExplorerTool;

namespace VisualEditorApp.ViewModels
{
    public partial class MainWindowViewModel : ObservableObject
    {
        // هذه هي الخاصية التي كانت تسبب الخطأ CS1061
        [ObservableProperty] private DocumentOutlineTool _documentOutline;
        [ObservableProperty] private ToolboxTool _toolbox;
        [ObservableProperty] private PropertiesTool _properties;
        [ObservableProperty] private SolutionExplorerTool _solutionExplorer;
        [ObservableProperty] private IRootDock? _layout;

        public MainWindowViewModel()
        {
            // إنشاء الأدوات هنا يضمن وجود نسخة واحدة فقط في البرنامج كله
            DocumentOutline = new DocumentOutlineTool();
            Toolbox = new ToolboxTool { Id = "Toolbox", Title = "Toolbox" };
            Properties = new PropertiesTool { Id = "Properties", Title = "Properties" };
            SolutionExplorer = new SolutionExplorerTool();
        }


        
    }


}
