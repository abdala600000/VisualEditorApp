using Avalonia.Controls;
using Avalonia.Controls.Primitives; // ãåã ááæÕæá Åáì Thumb
using Avalonia.Input;
namespace VisualEditorApp;

public partial class DesignerItem : UserControl
{
    public DesignerItem()
    {
        InitializeComponent();
    }
    // ÏÇáÉ ÌÏíÏÉ áÇÓÊÞÈÇá ÇáÔßá ææÖÚå Ýí ÇáæÚÇÁ ÇáÏÇÎáí
    public void SetContent(Control content)
    {
        ShapeContainer.Content = content;
    }
    // åÐå ÇáÏÇáÉ ÊÚãá ÚäÏ ÓÍÈ Ãí äÞØÉ ãä äÞÇØ ÇáÒæÇíÇ
    private void Resize_DragDelta(object? sender, VectorEventArgs e)
    {
        if (sender is Thumb thumb)
        {
            // ãÞÏÇÑ ÍÑßÉ ÇáãÇæÓ
            double deltaX = e.Vector.X;
            double deltaY = e.Vector.Y;

            // ÇáÃÈÚÇÏ æÇáãæÞÚ ÇáÍÇáí ááÚäÕÑ
            double newWidth = this.Width;
            double newHeight = this.Height;
            double left = Canvas.GetLeft(this);
            double top = Canvas.GetTop(this);

            // ÍÓÇÈ ÇáÃÈÚÇÏ ÇáÌÏíÏÉ ÈäÇÁð Úáì ÇáäÞØÉ ÇáãÓÍæÈÉ
            if (thumb.Name == "TopLeft")
            {
                newWidth -= deltaX;
                newHeight -= deltaY;
                left += deltaX;
                top += deltaY;
            }
            else if (thumb.Name == "TopRight")
            {
                newWidth += deltaX;
                newHeight -= deltaY;
                top += deltaY;
            }
            else if (thumb.Name == "BottomLeft")
            {
                newWidth -= deltaX;
                newHeight += deltaY;
                left += deltaX;
            }
            else if (thumb.Name == "BottomRight")
            {
                newWidth += deltaX;
                newHeight += deltaY;
            }

            // ÊØÈíÞ ÇáÃÈÚÇÏ ÇáÌÏíÏÉ ãÚ æÖÚ ÍÏ ÃÏäì ááÍÌã (20 ÈßÓá ãËáÇð) ÍÊì áÇ íÎÊÝí ÇáÚäÕÑ
            if (newWidth > 20)
            {
                this.Width = newWidth;
                if (thumb.Name == "TopLeft" || thumb.Name == "BottomLeft")
                    Canvas.SetLeft(this, left);
            }

            if (newHeight > 20)
            {
                this.Height = newHeight;
                if (thumb.Name == "TopLeft" || thumb.Name == "TopRight")
                    Canvas.SetTop(this, top);
            }
        }
    }
}