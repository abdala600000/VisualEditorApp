using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Media;

namespace VisualEditorApp;

public partial class ToolboxView : UserControl
{
    public ToolboxView()
    {
        InitializeComponent();
    }

    private void AddRectButton_Click(object? sender, RoutedEventArgs e)
    {
        var rect = new Rectangle
        {
            Fill = Brushes.SteelBlue,
            Stroke = Brushes.Black,
            StrokeThickness = 1
        };

        var designerItem = new DesignerItem
        {
            Width = 100,
            Height = 100 //  ÕœÌœ «·√»⁄«œ «·√”«”Ì… ··€·«›
        };

        // ‰” Œœ„ «·œ«·… «·ÃœÌœ… ·Ê÷⁄ «·„—»⁄ œ«Œ· «·€·«› »œÊ‰ „”Õ «·‰ﬁ«ÿ
        designerItem.SetContent(rect);

        WorkspaceView.Instance?.AddElement(designerItem, 50, 50);
    }

    private void AddCircleButton_Click(object? sender, RoutedEventArgs e)
    {
        var ellipse = new Ellipse
        {
            Fill = Brushes.Tomato,
            Stroke = Brushes.Black,
            StrokeThickness = 1
        };

        var designerItem = new DesignerItem
        {
            Width = 100,
            Height = 100
        };

        designerItem.SetContent(ellipse);

        WorkspaceView.Instance?.AddElement(designerItem, 100, 100);
    }
}