using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using VisualEditorApp.Models;

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
                // ÇÓÊÏÚÇÁ ÇבדÇ‗Ñז בÊÍÏםÏ ÇבÚהÕÑ Ýם דהØÞÉ ÇבÚדב
                // ÇÓÊÎÏדהÇ Instance בÓוזבÉ ÇבזÕזב Èםה ÇבהזÇÝÐ ÇבדהÝÕבÉ
                WorkspaceView.Instance?.SelectControl(selectedNode.RelatedControl);
            }
        }
    }
}