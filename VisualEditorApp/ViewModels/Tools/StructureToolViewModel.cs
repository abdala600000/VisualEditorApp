using System;
using CommunityToolkit.Mvvm.ComponentModel;
using Dock.Model.Mvvm.Controls;
using VisualEditor.Toolbox.Outline;

namespace VisualEditorApp.ViewModels.Tools
{
    public partial class StructureToolViewModel : Tool
    {
        [ObservableProperty] private string _summary = "No structure information available.";

       
        public StructureToolViewModel()
        {
            Id = "Structure";
            Title = "Structure";
        }
    }
}
