using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using VisualEditorApp.ViewModels;

namespace VisualEditorApp;
public partial class SolutionExplorerView : UserControl
{
     
    public SolutionExplorerView()
    {
        InitializeComponent();

         
    }
    private void SolutionTree_DoubleTapped(object? sender, Avalonia.Input.TappedEventArgs e)
    {
        if (SolutionTree.SelectedItem is SolutionItem item && item.IconType != "Folder")
        {
            // ‰‰œÂ ⁄·Ï «·‹ Command «··Ì ›Ì «·‹ ViewModel
            ((SolutionExplorerTool)DataContext!).OpenFileCommand.Execute(item);
        }
    }
}