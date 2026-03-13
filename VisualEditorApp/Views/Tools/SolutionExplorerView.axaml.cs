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

        private void OnItemDoubleTapped(object? sender, Avalonia.Input.TappedEventArgs e)
        {
            if (DataContext is SolutionExplorerViewModel viewModel && viewModel.SelectedItem is { Path: { } path } clickedItem)
            {
                if (clickedItem.Kind == SolutionItemKind.Document ||  clickedItem.Kind == Models.SolutionItemKind.Project)
                {
                    viewModel.OpenDocument(path);
                }
            }
        }
    }
}
