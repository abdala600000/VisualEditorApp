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

      //  WorkspaceView.Instance?.AddElement(designerItem, 50, 50);
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

       // WorkspaceView.Instance?.AddElement(designerItem, 100, 100);
    }


    private void AddControlButton_Click(object? sender, RoutedEventArgs e)
    {
        // ≈‰‘«¡ “— ÕﬁÌﬁÌ
        var btn = new Button
        {
            Content = "Sample Button",
            // ‰Ã⁄· «·“— Ì „œœ ·Ì„·√ „”«Õ… «·€·«› (DesignerItem) »«·ﬂ«„· ⁄‰œ «· ﬂ»Ì— Ê«· ’€Ì—
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Stretch,
            VerticalAlignment = Avalonia.Layout.VerticalAlignment.Stretch
        };

        // ≈‰‘«¡ «·€·«› Ê ÕœÌœ √»⁄«œÂ «·«› —«÷Ì…
        var designerItem = new DesignerItem
        {
            Width = 120,
            Height = 40
        };

        // Ê÷⁄ «·“— œ«Œ· «·€·«›
        designerItem.SetContent(btn);

        // ≈—”«·Â ≈·Ï „”«Õ… «·⁄„·
      //  WorkspaceView.Instance?.AddElement(designerItem, 150, 150);
    }
}