using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using Dock.Model.Mvvm.Controls;
using VisualEditorApp.Models;

namespace VisualEditorApp.ViewModels.Tools
{
    public partial class ProblemsToolViewModel : Tool
    {
        public ProblemsToolViewModel()
        {
            Id = "Problems";
            Title = "Problems";
            Diagnostics = new ObservableCollection<ProblemItemViewModel>();
        }

        public ObservableCollection<ProblemItemViewModel> Diagnostics { get; }

        public void UpdateDiagnostics(System.Collections.Generic.IEnumerable<ProblemItemViewModel> items)
        {
            Diagnostics.Clear();
            foreach (var item in items)
            {
                Diagnostics.Add(item);
            }
        }

        public void Clear()
        {
            Diagnostics.Clear();
        }
    }
}
