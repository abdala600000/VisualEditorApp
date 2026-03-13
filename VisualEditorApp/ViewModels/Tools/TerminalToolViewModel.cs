using CommunityToolkit.Mvvm.ComponentModel;
using Dock.Model.Mvvm.Controls;

namespace VisualEditorApp.ViewModels.Tools
{
    public partial class TerminalToolViewModel : Tool
    {
        [ObservableProperty] private string _output = string.Empty;

        public TerminalToolViewModel()
        {
            Id = "Terminal";
            Title = "Terminal";
        }
    }
}
