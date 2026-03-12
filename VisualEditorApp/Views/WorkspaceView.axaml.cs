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
    // ãÊÛíÑ áÍİÙ ÇáÚäÕÑ ÇáãÍÏÏ ÍÇáíÇğ
    private DesignerItem? _selectedItem = null;

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

        var designerItem = visualSource?.GetVisualAncestors().OfType<DesignerItem>().FirstOrDefault()
                           ?? visualSource as DesignerItem;

        if (designerItem != null)
        {
            // ÅĞÇ ÖÛØäÇ Úáì ÚäÕÑ ÌÏíÏ ÛíÑ ÇáãÍÏÏ ÍÇáíÇğ¡ Şã ÈÅáÛÇÁ ÊÍÏíÏ ÇáŞÏíã
            if (_selectedItem != null && _selectedItem != designerItem)
            {
                _selectedItem.IsSelected = false;
            }

            // ÊÍÏíÏ ÇáÚäÕÑ ÇáĞí ÖÛØäÇ Úáíå
            _selectedItem = designerItem;
            _selectedItem.IsSelected = true;
            // ÃÖİ åĞÇ ÇáÓØÑ áÅÑÓÇá ÇáÚäÕÑ Åáì áæÍÉ ÇáÎÕÇÆÕ
            PropertiesView.Instance?.SetSelectedElement(_selectedItem);
            _isDragging = true;
            _draggedElement = designerItem;
            _startPoint = point.Position;
            e.Handled = true;
        }
        else
        {
            // ÅĞÇ ÖÛØäÇ Úáì ãÓÇÍÉ İÇÑÛÉ İí ÇáÜ Canvas¡ Şã ÈÅáÛÇÁ ÇáÊÍÏíÏ
            if (_selectedItem != null)
            {
                _selectedItem.IsSelected = false;
                _selectedItem = null;
                // ÃÖİ åĞÇ ÇáÓØÑ áÊİÑíÛ áæÍÉ ÇáÎÕÇÆÕ
                PropertiesView.Instance?.SetSelectedElement(null);
            }
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


    // ÏÇáÉ áãÓÍ ßá ÇáÚäÇÕÑ ãä ãÓÇÍÉ ÇáÚãá
    public void ClearWorkspace()
    {
        DesignerCanvas.Children.Clear();
        _selectedItem = null;
    }

    // ÏÇáÉ ãÎÕÕÉ áÇÓÊŞÈÇá ÇáÚäÇÕÑ ÇáãÍááÉ ãä ãáİ ÇáÜ XAML æÊÛáíİåÇ
    public void AddWrappedElement(Control element, double left, double top, double width, double height)
    {
        var designerItem = new DesignerItem
        {
            // ÅĞÇ áã íßä ÇáÚäÕÑ íãÊáß ÚÑÖÇğ Ãæ ØæáÇğ İí Çáãáİ¡ äÖÚ ŞíãÇğ ÇİÊÑÇÖíÉ
            Width = double.IsNaN(width) ? 100 : width,
            Height = double.IsNaN(height) ? 40 : height
        };

        designerItem.SetContent(element);

        // ÊÍÏíÏ ÇáãæŞÚ¡ æÅĞÇ áã íßä áå ãæŞÚ äÖÚå İí ÇáÅÍÏÇËíÇÊ (50, 50)
        Canvas.SetLeft(designerItem, double.IsNaN(left) ? 50 : left);
        Canvas.SetTop(designerItem, double.IsNaN(top) ? 50 : top);

        DesignerCanvas.Children.Add(designerItem);
    }
}
