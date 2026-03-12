using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using Avalonia.VisualTree;
using System.Linq;

namespace VisualEditorApp;

public partial class WorkspaceView : UserControl
{
    // ÃÖİ åĞÇ ÇáÓØÑ áÅäÔÇÁ äŞØÉ æÕæá ãÈÇÔÑÉ áãÓÇÍÉ ÇáÚãá
    public static WorkspaceView? Instance { get; private set; }
    // ÇáÍİÇÙ Úáì ÇáãÊÛíÑÇÊ ÇáÎÇÕÉ ÈÚãáíÉ ÇáÓÍÈ
    private bool _isDragging = false;
    private Point _startPoint;
    private Control? _draggedElement = null;

    public WorkspaceView()
    {
        InitializeComponent();
        Instance = this; // ÃÖİ åĞÇ ÇáÓØÑ áÊÚííä ÇáãÑÌÚ ÚäÏ ÊÔÛíá ÇáæÇÌåÉ
    }

    private void Canvas_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        var canvas = sender as Canvas;
        if (canvas == null) return;

        var point = e.GetCurrentPoint(canvas);
        var visualSource = e.Source as Visual;

        // åäÇ äÈÍË Úä DesignerItem İí ÇáÚäÇÕÑ ÇáÊí ÊÍÊ ÇáãÇæÓ
        // ÅĞÇ ÖÛØäÇ Úáì ÇáãÑÈÚ ÇáÏÇÎáí¡ ÓíÕÚÏ ÇáßæÏ ááÃÚáì ÍÊì íÌÏ DesignerItem ÇáÍÇæí áå
        var designerItem = visualSource?.GetVisualAncestors().OfType<DesignerItem>().FirstOrDefault()
                           ?? visualSource as DesignerItem;

        if (designerItem != null)
        {
            _isDragging = true;
            _draggedElement = designerItem; // ÇáÂä äÍä äÓÍÈ ÇáÛáÇİ ÈÇáßÇãá
            _startPoint = point.Position;
            e.Handled = true;
        }
    }

    private void Canvas_PointerMoved(object? sender, PointerEventArgs e)
    {
        if (!_isDragging || _draggedElement == null) return;

        var canvas = sender as Canvas;
        if (canvas == null) return;

        var currentPoint = e.GetCurrentPoint(canvas);

        // ÍÓÇÈ ãÓÇİÉ ÇáÊÍÑíß æÊÍÏíË ÅÍÏÇËíÇÊ ÇáÚäÕÑ
        var offsetX = currentPoint.Position.X - _startPoint.X;
        var offsetY = currentPoint.Position.Y - _startPoint.Y;

        var currentLeft = Canvas.GetLeft(_draggedElement);
        var currentTop = Canvas.GetTop(_draggedElement);

        Canvas.SetLeft(_draggedElement, currentLeft + offsetX);
        Canvas.SetTop(_draggedElement, currentTop + offsetY);

        _startPoint = currentPoint.Position;
    }

    private void Canvas_PointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        // ÅäåÇÁ ÇáÓÍÈ
        _isDragging = false;
        _draggedElement = null;
    }

    // ÏÇáÉ ÌÏíÏÉ ÓäÓÊÎÏãåÇ áÇÍŞÇğ áÑÈØ ÇáÃÒÑÇÑ ÇáãæÌæÏÉ İí äÇİĞÉ ÇáÃÏæÇÊ ÈãÓÇÍÉ ÇáÚãá åĞå
    public void AddElement(Control element, double left, double top)
    {
        Canvas.SetLeft(element, left);
        Canvas.SetTop(element, top);
        DesignerCanvas.Children.Add(element);
    }
}
