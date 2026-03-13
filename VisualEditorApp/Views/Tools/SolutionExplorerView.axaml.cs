using Avalonia.Controls;
using Avalonia.Interactivity;
using VisualEditorApp.Models;
using VisualEditorApp.ViewModels.Tools;

namespace VisualEditorApp.Views.Tools
{
    public partial class SolutionExplorerView : UserControl
    {
        public SolutionExplorerView()
        {
            InitializeComponent();
        }

        private void OnTreeViewDoubleTapped(object? sender, RoutedEventArgs e)
        {
            if (DataContext is SolutionExplorerViewModel viewModel && viewModel.SelectedItem is { IsLeaf: true, Path: { } path })
            {
                viewModel.OpenDocument(path);
            }
        }
    }
}
