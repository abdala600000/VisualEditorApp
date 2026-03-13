using Avalonia;
using Avalonia.Controls;
using Avalonia.LogicalTree;
using Avalonia.Markup.Xaml;
using CommunityToolkit.Mvvm.Messaging;
using System.Xml.Linq;
using VisualEditor.Core.Messages;
using VisualEditor.Toolbox.Prop;
 

namespace VisualEditor.Toolbox.Outline;

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
                WeakReferenceMessenger.Default.Send(new ControlSelectedMessage(selectedNode.RelatedControl, "Outline"));

            }
        }
    }
}