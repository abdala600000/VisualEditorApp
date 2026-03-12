using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.VisualTree;
using System.Linq;

namespace VisualEditorApp;

public partial class WorkspaceView : UserControl
{
    public static WorkspaceView? Instance { get; private set; }
    private Control? _selectedControl;

    public WorkspaceView()
    {
        InitializeComponent();
        Instance = this;

        // ÇÚÊÑÇÖ Çáßáíß áÊÍÏíÏ ÇáÚäÕÑ (Tunneling)
        DesignSurface.AddHandler(InputElement.PointerPressedEvent, DesignSurface_PreviewPointerPressed, RoutingStrategies.Tunnel);
    }

    public void LoadDesign(Control rootControl)
    {
        DesignSurface.Content = rootControl;
        ClearSelection();
    }

    public void ClearWorkspace()
    {
        DesignSurface.Content = null;
        ClearSelection();
    }

    // --- 1. ÇáÊÍÏíÏ æÇáÇÚÊÑÇÖ ---
    private void DesignSurface_PreviewPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (e.Source is Control clickedControl && clickedControl != DesignSurface)
        {
            // ÅĞÇ ÖÛØäÇ Úáì ÇáÜ Window ÇáÃÕáíÉ äİÓåÇ¡ äÊÌÇåáåÇ æäÍÏÏ ãÓÇÍÉ ÇáÚãá
            if (clickedControl is Window || clickedControl.Name == "DesignSurface")
            {
                ClearSelection();
                return;
            }

            SelectControl(clickedControl);

            // ÊİÚíá ÇáÊİÇÚá ãÚ ØÈŞÉ ÇáÜ Adorner áßí äÊãßä ãä ÓÍÈ ÇáãÑÈÚÇÊ
            AdornerCanvas.IsHitTestVisible = true;
            e.Handled = true;
        }
        else
        {
            ClearSelection();
        }
    }

    private void SelectControl(Control control)
    {
        _selectedControl = control;
        UpdateAdornerPosition();
        SelectionAdorner.IsVisible = true;
    }

    private void ClearSelection()
    {
        _selectedControl = null;
        SelectionAdorner.IsVisible = false;
        AdornerCanvas.IsHitTestVisible = false;
    }

    // --- 2. ÊÍÏíË ãæŞÚ ÅØÇÑ ÇáÊÍÏíÏ İæŞ ÇáßäÊÑæá ---
    private void UpdateAdornerPosition()
    {
        if (_selectedControl == null) return;

        // ÇáÍÕæá Úáì ÅÍÏÇËíÇÊ ÇáßäÊÑæá ÇáÍŞíŞí æÊÍÑíß ÇáÅØÇÑ ÇáÃÒÑŞ İæŞå
        var transform = _selectedControl.TransformToVisual(AdornerCanvas);
        if (transform != null)
        {
            var bounds = new Rect(new Point(0, 0), _selectedControl.Bounds.Size);
            var rectInAdorner = bounds.TransformToAABB(transform.Value);

            SelectionAdorner.Width = rectInAdorner.Width;
            SelectionAdorner.Height = rectInAdorner.Height;
            Canvas.SetLeft(SelectionAdorner, rectInAdorner.X);
            Canvas.SetTop(SelectionAdorner, rectInAdorner.Y);
        }
    }

    // --- 3. ãÍÑß ÇáÊßÈíÑ æÇáÊÕÛíÑ (Resizing Engine) ---
    private void Resize_DragDelta(object? sender, VectorEventArgs e)
    {
        if (_selectedControl == null || sender is not Thumb thumb) return;

        double deltaX = e.Vector.X;
        double deltaY = e.Vector.Y;

        // áæ ÇáßäÊÑæá ãáæÔ ÚÑÖ Ãæ Øæá ÕÑíÍ (NaN)¡ äÃÎĞ ÍÌãå ÇáÍÇáí ßÈÏÇíÉ
        double currentWidth = double.IsNaN(_selectedControl.Width) ? _selectedControl.Bounds.Width : _selectedControl.Width;
        double currentHeight = double.IsNaN(_selectedControl.Height) ? _selectedControl.Bounds.Height : _selectedControl.Height;

        double newWidth = currentWidth;
        double newHeight = currentHeight;

        if (thumb.Name == "TopLeft")
        {
            newWidth -= deltaX; newHeight -= deltaY;
        }
        else if (thumb.Name == "TopRight")
        {
            newWidth += deltaX; newHeight -= deltaY;
        }
        else if (thumb.Name == "BottomLeft")
        {
            newWidth -= deltaX; newHeight += deltaY;
        }
        else if (thumb.Name == "BottomRight")
        {
            newWidth += deltaX; newHeight += deltaY;
        }

        // ÊØÈíŞ ÇáãŞÇÓ ÇáÌÏíÏ Úáì ÇáßäÊÑæá ÇáÍŞíŞí (ãÚ ÇáÊÃßÏ Ãäå áÇ íÕÛÑ ÌÏÇğ)
        if (newWidth > 10) _selectedControl.Width = newWidth;
        if (newHeight > 10) _selectedControl.Height = newHeight;

        // ÊÍÏíË ãßÇä ÇáÅØÇÑ ÇáÃÒÑŞ áíØÇÈŞ ÇáÍÌã ÇáÌÏíÏ
        UpdateAdornerPosition();
    }
}

