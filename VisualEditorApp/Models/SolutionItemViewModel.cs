using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace VisualEditorApp.Models
{
    public partial class SolutionItemViewModel : ObservableObject
    {
        [ObservableProperty] private bool _isExpanded;
        [ObservableProperty] private bool _isSelected;

        public SolutionItemViewModel(SolutionItemKind kind, string name, string? path)
        {
            Kind = kind;
            Name = name;
            Path = path;
            Children = new ObservableCollection<SolutionItemViewModel>();
        }

        public SolutionItemKind Kind { get; }

        public string Name { get; }

        public string? Path { get; }

        public ObservableCollection<SolutionItemViewModel> Children { get; }

        public bool IsLeaf => Kind == SolutionItemKind.Document;
    }
}
