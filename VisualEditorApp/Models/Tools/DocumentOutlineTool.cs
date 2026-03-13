using Dock.Model.Mvvm.Controls;
using VisualEditor.Toolbox.Outline;

namespace VisualEditorApp.Models.Tools
{
    public class DocumentOutlineTool : Tool
    {
        // أضف هذا السطر ليكون هو المرجع الوحيد في البرنامج كله
        public static DocumentOutlineTool Instance { get; } = new DocumentOutlineTool();


    }
}
