using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.LogicalTree;
using Avalonia.Markup.Xaml;
using Avalonia.VisualTree;
using AvaloniaEdit.Highlighting;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml;
using VisualEditorApp.Models;
using VisualEditorApp.Models.Tools;
using VisualEditorApp.ViewModels;
 
namespace VisualEditorApp;

public partial class WorkspaceView : UserControl
{
    public static WorkspaceView? Instance { get; private set; }
    private Control? _selectedControl;
    private bool _isDraggingControl = false;
    private Point _dragStartMousePosition;
    private Point _dragStartControlPosition;

    // تعريف النواة الجديدة كمتغير على مستوى اللوحة
    private readonly VisualEditorApp.Core.XmlToAvaloniaBuilder _xmlBuilder = new();
    private bool _hasMoved = false; // المتغير الجديد عشان نراقب الحركة
   
    public WorkspaceView()
    {
        InitializeComponent();
        Instance = this;

        // --- تفعيل تلوين الكود (XML/XAML Highlighting) ---
        XamlEditor.SyntaxHighlighting = HighlightingManager.Instance.GetDefinition("XML");

        // تفعيل السحب والإفلات من الـ Toolbox
        DragDrop.SetAllowDrop(DesignerContainer, true);
        DesignerContainer.AddHandler(DragDrop.DragOverEvent, DesignSurface_DragOver);
        DesignerContainer.AddHandler(DragDrop.DropEvent, DesignSurface_Drop);

        // اعتراض أحداث الماوس للتحكم في الكنترولات داخل اللوحة (Tunneling)
        DesignSurface.AddHandler(InputElement.PointerPressedEvent, DesignSurface_PreviewPointerPressed, RoutingStrategies.Tunnel);
        DesignSurface.AddHandler(InputElement.PointerMovedEvent, DesignSurface_PreviewPointerMoved, RoutingStrategies.Tunnel);
        DesignSurface.AddHandler(InputElement.PointerReleasedEvent, DesignSurface_PreviewPointerReleased, RoutingStrategies.Tunnel);
    }
    // --- 4. محرك السحب والإفلات (Drag & Drop Engine) ---

    private void DesignSurface_DragOver(object? sender, DragEventArgs e)
    {
        // التأكد إن الماوس شايل داتا فيها "ControlType"
        if (e.Data.Contains("ControlType"))
        {
            e.DragEffects = DragDropEffects.Copy; // تغيير شكل الماوس لـ Copy
        }
        else
        {
            e.DragEffects = DragDropEffects.None; // 🚫
        }

        // السطر ده هو اللي بيمسح علامة الـ 🚫 ويأكد العملية
        e.Handled = true;
    }

    private void DesignSurface_Drop(object? sender, DragEventArgs e)
    {
        if (e.Data.Contains("ControlType"))
        {
            string typeName = e.Data.Get("ControlType")?.ToString() ?? "";
            Type? controlType = Type.GetType(typeName);

            if (controlType != null && typeof(Control).IsAssignableFrom(controlType))
            {
                // 1. التأكد من وجود لوحة أساسية (Root Canvas) لو الشاشة بيضاء تماماً
                if (DesignSurface.Content == null)
                {
                    DesignSurface.Content = new Canvas { Background = Avalonia.Media.Brushes.Transparent };
                }

                // 2. بناء الكنترول الجديد
                var newControl = (Control)Activator.CreateInstance(controlType)!;
                newControl.Width = 100;
                newControl.Height = 30;
                if (newControl is ContentControl cc) cc.Content = controlType.Name;
                else if (newControl is TextBlock tb) tb.Text = controlType.Name;

                // 3. البحث عن الحاوية (Container) اللي الماوس واقف فوقها
                Control? targetContainer = GetValidDropTarget(e.Source as Control);
                if (targetContainer == null) return;

                // 4. تحديد الإحداثيات بالنسبة للحاوية الهدف (مش الشاشة كلها)
                var dropPosition = e.GetPosition(targetContainer);

                // 5. زرع الكنترول في الحاوية الصحيحة
                if (targetContainer is Panel targetPanel)
                {
                    // لو الحاوية Canvas (نقدر نحدد مكان حر X و Y)
                    if (targetPanel is Canvas)
                    {
                        Canvas.SetLeft(newControl, dropPosition.X);
                        Canvas.SetTop(newControl, dropPosition.Y);
                    }
                    else
                    {
                        // لو StackPanel أو Grid، المكان بيتحدد أوتوماتيك، بس ممكن نظبط الـ Margin
                        newControl.Margin = new Avalonia.Thickness(0);
                    }
                    targetPanel.Children.Add(newControl);
                }
                else if (targetContainer is ContentControl targetContentControl)
                {
                    // لو الحاوية بتقبل عنصر واحد (زي Border) وكانت فاضية
                    targetContentControl.Content = newControl;
                }

                // 6. تحديد الكنترول وتحديث شجرة العناصر
                SelectControl(newControl);
                if (DesignSurface.Content is Control rootControl)
                {
                    UpdateOutline(rootControl);
                }

                // السطر الجديد
                UpdateXamlEditor();
            }
        }
    }
    private Control? GetValidDropTarget(Control? hitControl)
    {
        Control? current = hitControl;

        // نفضل نطلع لفوق في الشجرة لحد ما نلاقي حاوية مناسبة
        while (current != null && current != DesignSurface)
        {
            // هل العنصر الحالي عبارة عن Panel بيشيل عناصر كتير؟
            if (current is Panel)
            {
                return current;
            }
            // أو هل هو حاوية لعنصر واحد (زي Border) ومحتواه فاضي؟
            if (current is ContentControl cc && cc.Content == null && current != DesignSurface.Content)
            {
                return current;
            }

            current = current.Parent as Control;
        }

        // لو ملقيناش حاجة (الماوس واقف في الفراغ)، نرجع اللوحة الأساسية
        return DesignSurface.Content as Control;
    }
    private void InjectControlIntoSurface(Control newControl, Point position)
    {
        // لو اللوحة فاضية تماماً، نعمل Canvas أساسي يلم الليلة دي كلها
        if (DesignSurface.Content == null)
        {
            var rootCanvas = new Canvas { Background = Avalonia.Media.Brushes.Transparent };
            DesignSurface.Content = rootCanvas;
        }

        // لو المحتوى الحالي عبارة عن Panel (زي Canvas أو Grid)
        if (DesignSurface.Content is Panel rootPanel)
        {
            // ضبط مكان الكنترول بناءً على الماوس
            if (rootPanel is Canvas)
            {
                Canvas.SetLeft(newControl, position.X);
                Canvas.SetTop(newControl, position.Y);
            }
            else
            {
                // لو كان Grid مثلاً
                newControl.Margin = new Thickness(position.X, position.Y, 0, 0);
                newControl.HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Left;
                newControl.VerticalAlignment = Avalonia.Layout.VerticalAlignment.Top;
            }

            // إضافة الكنترول للشاشة
            rootPanel.Children.Add(newControl);

            // تحديد الكنترول أوتوماتيكياً (عشان الإطار الأزرق يظهر عليه)
            SelectControl(newControl);

            // تحديث شجرة العناصر (Document Outline)
            UpdateOutline((Control)DesignSurface.Content);
        }
    }
    public void LoadDesign(Control rootControl)
    {
        DesignSurface.Content = rootControl;
        ClearSelection();

        // استدعاء تحديث الشجرة فور تحميل التصميم
        UpdateOutline(rootControl);
    }

    public void ClearWorkspace()
    {
        DesignSurface.Content = null;
        ClearSelection();
    }


    private void DesignSurface_PreviewPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (_isPreviewMode || !e.GetCurrentPoint(DesignSurface).Properties.IsLeftButtonPressed) return;

        if (e.Source is Visual sourceVisual && (sourceVisual == AdornerCanvas || AdornerCanvas.IsVisualAncestorOf(sourceVisual)))
            return;

        Control? clickedControl = GetSelectableControl(e.Source as Control);

        if (clickedControl != null)
        {
            SelectControl(clickedControl);
            e.Handled = true;

            _isDraggingControl = true;
            _hasMoved = false; // تصفير الحركة مع كل ضغطة جديدة
            _dragStartMousePosition = e.GetPosition(DesignSurface);

            if (clickedControl.Parent is Canvas)
            {
                double left = Canvas.GetLeft(clickedControl);
                double top = Canvas.GetTop(clickedControl);
                _dragStartControlPosition = new Point(double.IsNaN(left) ? 0 : left, double.IsNaN(top) ? 0 : top);
            }
            else
            {
                _dragStartControlPosition = new Point(clickedControl.Margin.Left, clickedControl.Margin.Top);
            }
        }
        else
        {
            ClearSelection();
        }
    }

    private void DesignSurface_PreviewPointerMoved(object? sender, PointerEventArgs e)
    {
        if (_isDraggingControl && _selectedControl != null)
        {
            var currentMousePos = e.GetPosition(DesignSurface);
            double deltaX = currentMousePos.X - _dragStartMousePosition.X;
            double deltaY = currentMousePos.Y - _dragStartMousePosition.Y;

            // التأكد إن الماوس اتحرك فعلاً مش مجرد "رعشة"
            if (System.Math.Abs(deltaX) > 1 || System.Math.Abs(deltaY) > 1)
            {
                _hasMoved = true; // كده إحنا اتأكدنا إن حصل سحب
            }

            if (_selectedControl.Parent is Canvas)
            {
                Canvas.SetLeft(_selectedControl, _dragStartControlPosition.X + deltaX);
                Canvas.SetTop(_selectedControl, _dragStartControlPosition.Y + deltaY);
            }
            else
            {
                _selectedControl.Margin = new Avalonia.Thickness(
                    _dragStartControlPosition.X + deltaX,
                    _dragStartControlPosition.Y + deltaY, 0, 0);
                _selectedControl.HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Left;
                _selectedControl.VerticalAlignment = Avalonia.Layout.VerticalAlignment.Top;
            }

            UpdateAdornerPosition();
            e.Handled = true;
        }
    }

    private void DesignSurface_PreviewPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (_isDraggingControl)
        {
            _isDraggingControl = false;
            e.Handled = true;

            // هنا السر: هنكتب الكود بس لو الكنترول اتحرك فعلياً
            if (_hasMoved)
            {
                UpdateXamlEditor();
            }
        }
    }
    // دالة مساعدة لمعرفة الكنترول المباشر للـ Canvas
    private Control? GetTopLevelControl(Control element)
    {
        Control? current = element;
        while (current != null)
        {
            if (current.Parent is Canvas && current.Parent == DesignSurface.Content)
                return current;
            current = current.Parent as Control;
        }
        return null;
    }
   
    private void SyncCodeEditorToControl(Control control)
    {
        if (string.IsNullOrEmpty(XamlEditor.Text)) return;

        string typeName = control.GetType().Name;
        string xaml = XamlEditor.Text;

        // نبحث عن أول ظهور لاسم الكنترول في الكود
        // ملاحظة: في المشاريع الضخمة نستخدم نظام إحداثيات أدق، ولكن هذا يفي بالغرض حالياً
        int index = xaml.IndexOf("<" + typeName);

        if (index != -1)
        {
            // تحريك المؤشر لمكان الكود
            XamlEditor.CaretOffset = index;
            // جعل المحرر ينزل آلياً لمكان السطر
            XamlEditor.ScrollToLine(XamlEditor.Document.GetLineByOffset(index).LineNumber);
        }
    }

    // دالة لاستقبال النص من الخارج (MainWindow)
    public void SetXamlContent(string xml)
    {
        _isInternalUpdate = true;
        XamlEditor.Text = xml;
        _isInternalUpdate = false;
        RefreshDesigner(xml);
    }
   

    // --- 2. تحديث موقع إطار التحديد فوق الكنترول ---
    private void UpdateAdornerPosition()
    {
        if (_selectedControl == null) return;

        // الحصول على إحداثيات الكنترول الحقيقي وتحريك الإطار الأزرق فوقه
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

    // --- 3. محرك التكبير والتصغير (Resizing Engine) ---
    private void Resize_DragDelta(object? sender, VectorEventArgs e)
    {
        if (_selectedControl == null || sender is not Thumb thumb) return;

        double deltaX = e.Vector.X;
        double deltaY = e.Vector.Y;

        // الحصول على المقاس الحالي
        double currentWidth = double.IsNaN(_selectedControl.Width) ? _selectedControl.Bounds.Width : _selectedControl.Width;
        double currentHeight = double.IsNaN(_selectedControl.Height) ? _selectedControl.Bounds.Height : _selectedControl.Height;

        double newWidth = currentWidth;
        double newHeight = currentHeight;

        // متغيرات لضبط المكان (عشان لو كبرنا من فوق أو شمال)
        double leftOffset = 0;
        double topOffset = 0;

        // تحديد اتجاه السحب
        if (thumb.Name == "TopLeft")
        {
            newWidth -= deltaX; newHeight -= deltaY;
            leftOffset = deltaX; topOffset = deltaY;
        }
        else if (thumb.Name == "TopRight")
        {
            newWidth += deltaX; newHeight -= deltaY;
            topOffset = deltaY;
        }
        else if (thumb.Name == "BottomLeft")
        {
            newWidth -= deltaX; newHeight += deltaY;
            leftOffset = deltaX;
        }
        else if (thumb.Name == "BottomRight")
        {
            newWidth += deltaX; newHeight += deltaY;
        }

        // 1. تطبيق العرض الجديد وتعديل الإحداثي السيني (X)
        if (newWidth > 10)
        {
            _selectedControl.Width = newWidth;
            if (leftOffset != 0)
            {
                if (_selectedControl.Parent is Canvas)
                    Canvas.SetLeft(_selectedControl, (double.IsNaN(Canvas.GetLeft(_selectedControl)) ? 0 : Canvas.GetLeft(_selectedControl)) + leftOffset);
                else
                    _selectedControl.Margin = new Avalonia.Thickness(_selectedControl.Margin.Left + leftOffset, _selectedControl.Margin.Top, 0, 0);
            }
        }

        // 2. تطبيق الطول الجديد وتعديل الإحداثي الصادي (Y)
        if (newHeight > 10)
        {
            _selectedControl.Height = newHeight;
            if (topOffset != 0)
            {
                if (_selectedControl.Parent is Canvas)
                    Canvas.SetTop(_selectedControl, (double.IsNaN(Canvas.GetTop(_selectedControl)) ? 0 : Canvas.GetTop(_selectedControl)) + topOffset);
                else
                    _selectedControl.Margin = new Avalonia.Thickness(_selectedControl.Margin.Left, _selectedControl.Margin.Top + topOffset, 0, 0);
            }
        }

        // 3. تحديث الإطار الأزرق عشان يمشي مع الكنترول بالمللي
        UpdateAdornerPosition();

        UpdateXamlEditor();
    }
    private bool _isPreviewMode = false;

    public void SetPreviewMode(bool isPreview)
    {
        _isPreviewMode = isPreview;
        if (_isPreviewMode)
        {
            ClearSelection(); // إخفاء المربعات الزرقاء فوراً
        }
    }
    private bool _isInternalUpdate = false;

    // دالة لتحديث النص عند فتح ملف من MainWindow
    public void SetXamlText(string xml)
    {
        _isInternalUpdate = true;
        XamlEditor.Text = xml;
        _isInternalUpdate = false;

        // تحديث التصميم فوراً
        RefreshDesigner(xml);
    }

    // حدث عند كتابة أي شيء في المحرر السفلي
    private void XamlEditor_TextChanged(object? sender, EventArgs e)
    {
        if (_isInternalUpdate) return;

        // تحديث التصميم "Live" أثناء الكتابة
        RefreshDesigner(XamlEditor.Text);
    }
    // --- مصفاة تنظيف الـ XAML (XAML Sanitizer) ---
  private string SanitizeXaml(string originalXaml)
{
    string clean = originalXaml;

    // 1. مسح الـ x:Class
    clean = Regex.Replace(clean, @"\s+x:Class=""[^""]*""", "");

    // 2. تحويل CompiledBinding
    clean = Regex.Replace(clean, @"\{CompiledBinding\b", "{Binding");

    // 3. مسح الأحداث (Events)
    clean = Regex.Replace(clean, @"\s+[A-Za-z]*(?:Click|Pressed|Released|Enter|Leave|Move|Wheel|Down|Up|Changed|Loaded|Unloaded|Opened|Closed|Tapped|TextInput|Focus|Checked|Unchecked)=""[^""]*""", "");

    // ======== الإضافة الجديدة: حماية اللوحة من الصور المفقودة ========
    // الفلتر ده بيمسح خاصية Source="" لو كانت مسار محلي أو avares:// 
    // وبيسيبها لو كانت رابط من النت (http أو https) عشان لو حبيت تعرض صورة من النت في التصميم
    clean = Regex.Replace(clean, @"\s+Source=""(?!(http|https)://)[^""]*""", "");
    
    // (اختياري) حماية إضافية لخصائص الصور التانية زي الفراشي (ImageBrush)
    clean = Regex.Replace(clean, @"<ImageBrush\s+ImageSource=""(?!(http|https)://)[^""]*""", "<ImageBrush ");

    return clean;
}
    private void RefreshDesigner(string xaml)
    {
        if (string.IsNullOrWhiteSpace(xaml))
        {
            ClearWorkspace();
            return;
        }

        try
        {
            // 1. تنظيف الكود (اختياري بس مهم لو جايب كود من بره)
            string cleanXml = SanitizeXaml(xaml);

            // 2. تحويل النص إلى كنترول حقيقي
            var parsedControl = AvaloniaRuntimeXamlLoader.Parse<object>(cleanXml);

            if (parsedControl != null)
            {
                // 3. خدعة احترافية: 
                // المحرك بتاعنا بيولد كود محاط بـ <UserControl>
                // إحنا محتاجين ناخد المحتوى اللي جواه (زي الـ Canvas) عشان أحداث الماوس تفضل شغالة صح


                if (parsedControl is Control rootControl)
                {
                    Control elementToLoad = rootControl;

                    if (rootControl is Window window && window.Content is Control windowContent)
                    {
                        window.Content = null;
                        DesignSurface.Content = windowContent;
                    }
                    else
                    {
                        DesignSurface.Content = parsedControl;
                    }
                }


 
                // 4. مسح التحديد القديم (عشان الكنترولات القديمة اتمسحت من الذاكرة)
                ClearSelection();

                // 5. تحديث شجرة العناصر (Document Outline)
                if (DesignSurface.Content is Control rootControl1)
                {
                    UpdateOutline(rootControl1);
                }
            }
        }
        catch
        {
            // 🚫 السكوت من ذهب هنا:
            // هنتجاهل الأخطاء لأن المستخدم ممكن يكون بيكتب <Butt ولسه مكملش الكلمة
            // فمش منطقي نطلع له Error مع كل حرف بيكتبه.
        }
    }

    private Control? GetSelectableControl(Control? element)
    {
        Control? current = element;

        while (current != null)
        {
            // 1. لو وصلنا للأرضية (اللوحة الفاضية)، نخرج من غير تحديد
            if (current == DesignSurface || current == DesignSurface.Content)
                return null;

            // 2. لو العنصر ده جزء من تصميم داخلي (زي TextBlock جوه Button)
            // نختار الأب الحقيقي بتاعه (الـ Button)
            if (current.TemplatedParent is Control parentControl)
            {
                current = parentControl;
                continue;
            }

            // 3. أول كنترول حقيقي يقابلنا نرجعه فوراً
            return current;
        }
        return null;
    }


    public void SelectControl(Control control)
    {
        _selectedControl = control;
        UpdateAdornerPosition();
        SelectionAdorner.IsVisible = true;

        // السطر ده بيرجع الروح للمربعات عشان تحس بالماوس
        AdornerCanvas.IsHitTestVisible = true;

        SyncCodeEditorToControl(control);
    }

    private void ClearSelection()
    {
        _selectedControl = null;
        SelectionAdorner.IsVisible = false;
        AdornerCanvas.IsHitTestVisible = false; // نقفلها عشان الماوس ميخبطش في الهوا
    }

    private void UpdateOutline(Control root)
    {
        var rootNode = BuildNode(root);

        // تحديث النسخة الوحيدة (التي يعرضها الـ Dock حالياً)
        // نستخدم Clear و Add بدلاً من New لضمان أن الـ TreeView تشعر بالتغيير
        DocumentOutlineTool.Instance.Nodes.Clear();
        DocumentOutlineTool.Instance.Nodes.Add(rootNode);
    }
    private ElementNode BuildNode(Control control)
    {
        var node = new ElementNode { Header = control.GetType().Name, RelatedControl = control };

        // استخدام الـ LogicalChildren للحصول على العناصر المتداخلة (مثل Border جوه StackPanel)
        foreach (var child in control.GetLogicalChildren())
        {
            if (child is Control childControl)
            {
                node.Children.Add(BuildNode(childControl));
            }
        }
        return node;
    }


    // --- محرك توليد XAML (XAML Generator) ---
    private void UpdateXamlEditor()
    {
        if (DesignSurface.Content is not Control rootControl) return;

        var sb = new System.Text.StringBuilder();

        // كتابة ترويسة الملف (Header)
        sb.AppendLine("<UserControl xmlns=\"https://github.com/avaloniaui\"");
        sb.AppendLine("             xmlns:x=\"http://schemas.microsoft.com/winfx/2006/xaml\">");

        // البدء في ترجمة الكنترول الأساسي وكل اللي جواه
        BuildControlXaml(rootControl, sb, 1);

        sb.AppendLine("</UserControl>");

        // إرسال الكود النهائي للمحرر السفلي (مع إيقاف التحديث العكسي عشان ما يحصلش لوب)
        _isInternalUpdate = true;
        XamlEditor.Text = sb.ToString();
        _isInternalUpdate = false;
    }

    private void BuildControlXaml(Control control, System.Text.StringBuilder sb, int indentLevel)
    {
        string indent = new string(' ', indentLevel * 4);
        string typeName = control.GetType().Name;

        sb.Append($"{indent}<{typeName}");

        // --- 1. الخصائص الأساسية ---
        if (!double.IsNaN(control.Width)) sb.Append($" Width=\"{(int)control.Width}\"");
        if (!double.IsNaN(control.Height)) sb.Append($" Height=\"{(int)control.Height}\"");

        if (control.Margin != default)
            sb.Append($" Margin=\"{control.Margin.Left},{control.Margin.Top},{control.Margin.Right},{control.Margin.Bottom}\"");

        if (control.Parent is Canvas)
        {
            double left = Canvas.GetLeft(control);
            double top = Canvas.GetTop(control);
            if (!double.IsNaN(left)) sb.Append($" Canvas.Left=\"{(int)left}\"");
            if (!double.IsNaN(top)) sb.Append($" Canvas.Top=\"{(int)top}\"");
        }

        // إضافة بعض الخصائص للـ Border
        if (control is Border border && border.CornerRadius != default)
        {
            sb.Append($" CornerRadius=\"{border.CornerRadius.TopLeft}\"");
        }

        bool hasChildren = false;

        // --- 2. معالجة الأبناء (Children) ---
        // أ. لو كنترول بيشيل لستة (Grid, StackPanel)
        if (control is Panel panel && panel.Children.Count > 0)
        {
            sb.AppendLine(">");
            foreach (var child in panel.Children)
            {
                if (child is Control childCtrl && childCtrl.Name != "SelectionAdorner")
                    BuildControlXaml(childCtrl, sb, indentLevel + 1);
            }
            hasChildren = true;
        }
        // ب. لو كنترول بيشيل محتوى واحد (Button)
        else if (control is ContentControl cc && cc.Content != null)
        {
            sb.AppendLine(">");
            if (cc.Content is Control childCtrl)
            {
                BuildControlXaml(childCtrl, sb, indentLevel + 1);
            }
            else
            {
                sb.AppendLine($"{indent}    {cc.Content}");
            }
            hasChildren = true;
        }
        // ج. حل مشكلة الـ Border في Avalonia 11
        else if (control is Border border1 && border1.Child != null)
        {
            sb.AppendLine(">");
            if (border1.Child is Control childCtrl && childCtrl.Name != "SelectionAdorner")
            {
                BuildControlXaml(childCtrl, sb, indentLevel + 1);
            }
            hasChildren = true;
        }
        // د. لو النص العادي
        else if (control is TextBlock tb && !string.IsNullOrEmpty(tb.Text))
        {
            sb.AppendLine(">");
            sb.AppendLine($"{indent}    {tb.Text}");
            hasChildren = true;
        }

        // --- 3. إغلاق التاج ---
        if (hasChildren)
        {
            sb.AppendLine($"{indent}</{typeName}>");
        }
        else
        {
            sb.AppendLine(" />");
        }
    }


    private void UpdateControlInXaml(Control control)
{
    if (string.IsNullOrEmpty(XamlEditor.Text) || control == null || control == DesignSurface.Content) return;

    string xaml = XamlEditor.Text;
    string typeName = control.GetType().Name;

    // 1. تحديد القيم الجديدة
    string newWidth = double.IsNaN(control.Width) ? "" : ((int)control.Width).ToString();
    string newHeight = double.IsNaN(control.Height) ? "" : ((int)control.Height).ToString();
    
    // 2. البحث عن تاج الكنترول في النص
    // (ده تطبيق مبسط لفكرة الـ DesignItem، في المشاريع الضخمة بنستخدم XML Parser)
    var regex = new System.Text.RegularExpressions.Regex($@"<{typeName}[^>]*>");
    var match = regex.Match(xaml);

    if (match.Success)
    {
        string originalTag = match.Value;
        string updatedTag = originalTag;

        // 3. تحديث العرض (Width)
        if (!string.IsNullOrEmpty(newWidth))
            updatedTag = UpdateOrAddAttribute(updatedTag, "Width", newWidth);

        // 4. تحديث الطول (Height)
        if (!string.IsNullOrEmpty(newHeight))
            updatedTag = UpdateOrAddAttribute(updatedTag, "Height", newHeight);

        // 5. تحديث المكان (لو جوه Canvas)
        if (control.Parent is Canvas)
        {
            double left = Canvas.GetLeft(control);
            double top = Canvas.GetTop(control);
            if (!double.IsNaN(left)) updatedTag = UpdateOrAddAttribute(updatedTag, "Canvas.Left", ((int)left).ToString());
            if (!double.IsNaN(top)) updatedTag = UpdateOrAddAttribute(updatedTag, "Canvas.Top", ((int)top).ToString());
        }
        // تحديث المكان (لو جوه StackPanel أو Grid)
        else if (control.Margin != default)
        {
            string marginStr = $"{(int)control.Margin.Left},{(int)control.Margin.Top},0,0";
            updatedTag = UpdateOrAddAttribute(updatedTag, "Margin", marginStr);
        }

        // 6. استبدال التاج القديم بالجديد في المحرر
        if (originalTag != updatedTag)
        {
            _isInternalUpdate = true;
            XamlEditor.Text = xaml.Substring(0, match.Index) + updatedTag + xaml.Substring(match.Index + match.Length);
            _isInternalUpdate = false;
        }
    }
}

// دالة مساعدة لتحديث أو إضافة خاصية جوه التاج
private string UpdateOrAddAttribute(string tag, string attributeName, string newValue)
{
    var regex = new System.Text.RegularExpressions.Regex($@"{attributeName}=""[^""]*""");
    if (regex.IsMatch(tag))
    {
        // لو الخاصية موجودة، نعدلها
        return regex.Replace(tag, $"{attributeName}=\"{newValue}\"");
    }
    else
    {
        // لو مش موجودة، نضيفها قبل قفلة التاج (> أو />)
        if (tag.EndsWith("/>"))
            return tag.Insert(tag.Length - 2, $" {attributeName}=\"{newValue}\"");
        else
            return tag.Insert(tag.Length - 1, $" {attributeName}=\"{newValue}\" ");
    }
}
}

