using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using VisualEditorApp.Models;
using VisualEditorApp.Models.Tools;
using VisualEditorApp.ViewModels;

namespace VisualEditorApp;

public partial class DocumentOutlineView : UserControl
{
    public DocumentOutlineView()
    {
        InitializeComponent();
    }
    private void TreeView_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (e.AddedItems.Count > 0 && e.AddedItems[0] is ElementNode selectedNode)
        {
            if (selectedNode.RelatedControl != null)
            {
                // 1. ÊÍÏíÏ ÇáÚäÕÑ İí ÇáÏíÒÇíäÑ (ÅÙåÇÑ ÇáãÑÈÚÇÊ ÇáÒÑŞÇÁ)
                WorkspaceView.Instance?.SelectControl(selectedNode.RelatedControl);
                // 2. ÇÓÊÏÚÇÁ ÇáßæÏ ÈÊÇÚß ÃäÊ (PropertiesView) æÊãÑíÑ ÇáÚäÕÑ áå
                PropertiesView.Instance?.SetSelectedElement(selectedNode.RelatedControl);

            }
        }
    }
}