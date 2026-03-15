using Avalonia;
using Avalonia.Controls;
using Avalonia.LogicalTree;
using Avalonia.Markup.Xaml;
using System.Xml.Linq;
using VisualEditor.Core.Messages;
using VisualEditor.Toolbox.Prop;
 

namespace VisualEditor.Toolbox.Outline;

public partial class DocumentOutlineView : UserControl
{
    public DocumentOutlineView()
    {
        InitializeComponent();
        DataContext = DocumentOutlineViewModel.Instance;


    }



    private void TreeView_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (e.AddedItems.Count > 0 && e.AddedItems[0] is ElementNode selectedNode)
        {
            if (selectedNode.RelatedControl != null)
            {
                MessageBus.Send(new ControlSelectedMessage(selectedNode.RelatedControl, "Outline"));

            }
        }
    }
}