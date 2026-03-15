using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives; // „Â„ ··Ê’Ê· ≈·Ï Thumb
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.VisualTree;
using System;
namespace VisualEditor.Core;


public partial class DesignerItem : UserControl
{
    public static readonly StyledProperty<double> RotationAngleProperty =
        AvaloniaProperty.Register<DesignerItem, double>(nameof(RotationAngle));

    public double RotationAngle
    {
        get => GetValue(RotationAngleProperty);
        set => SetValue(RotationAngleProperty, value);
    }

    private bool _isSelected;

    public bool IsSelected
    {
        get => _isSelected;
        set
        {
            _isSelected = value;
            AdornerLayer.IsVisible = value;
        }
    }

    public DesignerItem()
    {
        InitializeComponent();

        RenderTransformOrigin = new RelativePoint(new Point(0.5, 0.5), RelativeUnit.Relative);

        var rotateTransform = new RotateTransform();
        rotateTransform.Bind(RotateTransform.AngleProperty, this.GetObservable(RotationAngleProperty));
        RenderTransform = rotateTransform;
    }

    public void SetContent(Control content)
    {
        ShapeContainer.Content = content;
    }

    private void Resize_DragDelta(object? sender, VectorEventArgs e)
    {
        if (sender is Thumb thumb)
        {
            double deltaX = e.Vector.X;
            double deltaY = e.Vector.Y;

            double newWidth = this.Width;
            double newHeight = this.Height;
            double left = Canvas.GetLeft(this);
            double top = Canvas.GetTop(this);

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

    // --- √Õœ«À «· œÊÌ— «·ÃœÌœ… «· Ì Õ·  «·„‘ﬂ·… ---

    private bool _isRotating = false;

    private void RotationHandle_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        _isRotating = true;
        e.Handled = true; // „‰⁄ «‰ ﬁ«· «·ÕœÀ ··√”›· ·ﬂÌ ·« Ì Õ—ﬂ «·⁄‰’— »œ·« „‰ «·œÊ—«‰
    }

    private void RotationHandle_PointerMoved(object? sender, PointerEventArgs e)
    {
        if (!_isRotating) return;

        Canvas? canvas = this.FindAncestorOfType<Canvas>();
        if (canvas == null) return;

        // «·Õ’Ê· ⁄·Ï „Êﬁ⁄ «·„«Ê” «·œﬁÌﬁ
        Point currentPointOnCanvas = e.GetCurrentPoint(canvas).Position;

        double left = Canvas.GetLeft(this);
        double top = Canvas.GetTop(this);
        Point center = new Point(left + (this.Width / 2.0), top + (this.Height / 2.0));

        double offsetX = currentPointOnCanvas.X - center.X;
        double offsetY = currentPointOnCanvas.Y - center.Y;

        double angleInDegrees = Math.Atan2(offsetY, offsetX) * (180.0 / Math.PI);

        RotationAngle = angleInDegrees + 90;
    }

    private void RotationHandle_PointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        _isRotating = false;
        e.Handled = true;
    }
}
