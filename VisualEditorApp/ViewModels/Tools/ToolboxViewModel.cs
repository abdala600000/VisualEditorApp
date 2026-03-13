using System;
using Dock.Model.Mvvm.Controls;
using VisualEditor.Toolbox.Controls;

namespace VisualEditorApp.ViewModels.Tools
{
    public partial class ToolboxViewModel : Tool
    {
        public ToolboxViewModels Toolbox { get; }

        public ToolboxViewModel()
        {
            Id = "Toolbox";
            Title = "Toolbox";
            Toolbox = new ToolboxViewModels();
        }
    }
}
