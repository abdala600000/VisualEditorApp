using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.PanAndZoom;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Shapes;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.VisualTree;
using System;
using System.Collections.Generic;
using System.Linq;

namespace VisualEditor.Designer
{
    public partial class DesignerSurfaceView : UserControl
    {
        #region 1. الأرصدة والحقول (Fields)

        // الكنترولات
        private Control? _selectedControl;
        private List<Control> _selectedControls = new List<Control>();

        // حالات السحب والحركة
        private bool _isDraggingControl = false;
        private bool _isSelecting = false;
        private bool _hasMoved = false;
        private bool _isPreviewMode = false;

        // النقاط والإحداثيات
        private Point _dragStartMousePosition;
        private Point _dragStartControlPosition;
        private Point _selectionStartPoint;

        // الأحداث الخارجية
        public event EventHandler? DesignChanged;
        public event EventHandler<Control?>? SelectionChanged;

        // خصائص الوصول
        public Control? RootDesign => DesignSurface.Content as Control;

        #endregion

        #region 2. البداية والتعريف (Constructor)

        public DesignerSurfaceView()
        {
            InitializeComponent();
            SetupDesignerEvents();
        }

        private void SetupDesignerEvents()
        {
            // مراقبة الحجم والزووم لتحديث المساطر والـ Adorner
            TopRulerContainer.SizeChanged += (s, e) => UpdateRulers();
            LeftRulerContainer.SizeChanged += (s, e) => UpdateRulers();

            MyZoomBorder.PropertyChanged += (s, e) =>
            {
                var p = e.Property.Name;
                if (p == nameof(MyZoomBorder.Matrix) || p == "ZoomX" || p == "ZoomY" || p == "OffsetX" || p == "OffsetY")
                {
                    UpdateRulers();
                    UpdateAdornerPosition();
                }
            };

            // أحداث الماوس والكيبورد
            MyZoomBorder.DoubleTapped += (s, e) => MyZoomBorder.AutoFit();
            this.LayoutUpdated += (s, e) => UpdateAdornerPosition();
            this.KeyDown += DesignerSurfaceView_KeyDown;

            // السحب والإفلات (Drag & Drop من الـ Toolbox)
            DragDrop.SetAllowDrop(DesignSurface, true);
            DesignSurface.AddHandler(DragDrop.DragOverEvent, DesignSurface_DragOver);
            DesignSurface.AddHandler(DragDrop.DropEvent, DesignSurface_Drop);

            // اعتراض أحداث الماوس (Tunneling) لضمان التحكم قبل الكنترولات الفرعية
            DesignSurface.AddHandler(PointerPressedEvent, DesignSurface_PreviewPointerPressed, RoutingStrategies.Tunnel);
            DesignSurface.AddHandler(PointerMovedEvent, DesignSurface_PreviewPointerMoved, RoutingStrategies.Tunnel);
            DesignSurface.AddHandler(PointerReleasedEvent, DesignSurface_PreviewPointerReleased, RoutingStrategies.Tunnel);
        }

        #endregion

        #region 3. التحكم في التحديد (Selection Logic)

        public void SelectControl(Control? control)
        {
            _selectedControl = control;
            if (control != null)
            {
                UpdateAdornerPosition();
                SelectionAdorner.IsVisible = true;
                AdornerCanvas.IsHitTestVisible = true;
            }
            SelectionChanged?.Invoke(this, control);
        }

        private void ClearSelection()
        {
            _selectedControl = null;
            _selectedControls.Clear();
            SelectionAdorner.IsVisible = false;
            AdornerCanvas.IsHitTestVisible = false;
            SelectionBox.IsVisible = false;
            SelectionChanged?.Invoke(this, null);
        }

        private void UpdateAdornerPosition()
        {
            if (_selectedControl == null) return;
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

        private Control? GetSelectableControl(Control? element)
        {
            Control? current = element;
            while (current != null)
            {
                if (current == DesignSurface || current == DesignSurface.Content) return null;
                if (current.TemplatedParent is Control parentControl) { current = parentControl; continue; }
                return current;
            }
            return null;
        }

        #endregion

        #region 4. التعامل مع الماوس (Dragging & Multi-Selection)

        private void DesignSurface_PreviewPointerPressed(object? sender, PointerPressedEventArgs e)
        {
            if (_isPreviewMode || !e.GetCurrentPoint(DesignSurface).Properties.IsLeftButtonPressed) return;

            // تجاهل الضغط لو كان على الـ Adorner (عشان ندوّر أو نكبر)
            if (e.Source is Visual v && (v == AdornerCanvas || AdornerCanvas.IsVisualAncestorOf(v))) return;

            var pos = e.GetPosition(DesignSurface);
            var hitResult = DesignSurface.InputHitTest(pos);
            Control? clickedControl = GetSelectableControl(e.Source as Control);

            if (clickedControl != null)
            {
                // حالة سحب كنترول
                SelectControl(clickedControl);
                _isDraggingControl = true;
                _hasMoved = false;
                _dragStartMousePosition = pos;

                _dragStartControlPosition = clickedControl.Parent is Canvas
                    ? new Point(Canvas.GetLeft(clickedControl).DefaultIfNaN(), Canvas.GetTop(clickedControl).DefaultIfNaN())
                    : new Point(clickedControl.Margin.Left, clickedControl.Margin.Top);

                e.Handled = true;
            }
            else
            {
                // حالة الضغط على الخلفية (بداية مربع التحديد)
                _isSelecting = true;
                _selectionStartPoint = pos;
                SelectionBox.IsVisible = true;
                ClearSelection();
            }
        }

        private void DesignSurface_PreviewPointerMoved(object? sender, PointerEventArgs e)
        {
            var currentPos = e.GetPosition(DesignSurface);

            if (_isSelecting)
            {
                // تحديث مربع التحديد الأزرق
                double x = Math.Min(_selectionStartPoint.X, currentPos.X);
                double y = Math.Min(_selectionStartPoint.Y, currentPos.Y);
                SelectionBox.Width = Math.Abs(_selectionStartPoint.X - currentPos.X);
                SelectionBox.Height = Math.Abs(_selectionStartPoint.Y - currentPos.Y);
                Canvas.SetLeft(SelectionBox, x);
                Canvas.SetTop(SelectionBox, y);
            }
            else if (_isDraggingControl && _selectedControl != null)
            {
                // منطق السحب مع الـ Snap to Grid
                double deltaX = currentPos.X - _dragStartMousePosition.X;
                double deltaY = currentPos.Y - _dragStartMousePosition.Y;
                if (Math.Abs(deltaX) > 1 || Math.Abs(deltaY) > 1) _hasMoved = true;

                double snapSize = 10.0;
                double snappedX = Math.Round((_dragStartControlPosition.X + deltaX) / snapSize) * snapSize;
                double snappedY = Math.Round((_dragStartControlPosition.Y + deltaY) / snapSize) * snapSize;

                if (_selectedControl.Parent is Canvas)
                {
                    Canvas.SetLeft(_selectedControl, snappedX);
                    Canvas.SetTop(_selectedControl, snappedY);
                }
                else
                {
                    _selectedControl.Margin = new Thickness(snappedX, snappedY, 0, 0);
                    _selectedControl.HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Left;
                    _selectedControl.VerticalAlignment = Avalonia.Layout.VerticalAlignment.Top;
                }
                UpdateAdornerPosition();
            }
        }

        private void DesignSurface_PreviewPointerReleased(object? sender, PointerReleasedEventArgs e)
        {
            if (_isSelecting)
            {
                ProcessMultiSelection();
                _isSelecting = false;
                SelectionBox.IsVisible = false;
            }

            if (_isDraggingControl)
            {
                _isDraggingControl = false;
                if (_hasMoved) DesignChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        private void ProcessMultiSelection()
        {
            var selectionRect = new Rect(Canvas.GetLeft(SelectionBox), Canvas.GetTop(SelectionBox), SelectionBox.Width, SelectionBox.Height);
            _selectedControls.Clear();

            if (DesignSurface.Content is Panel mainPanel)
            {
                foreach (var child in mainPanel.Children.OfType<Control>())
                {
                    var pos = child.TranslatePoint(new Point(0, 0), DesignSurface);
                    if (pos.HasValue)
                    {
                        var childRect = new Rect(pos.Value.X, pos.Value.Y, child.Bounds.Width, child.Bounds.Height);
                        if (selectionRect.Intersects(childRect)) _selectedControls.Add(child);
                    }
                }
            }
        }

        #endregion

        #region 5. المساطر والزووم (Rulers & Zoom)

        private void UpdateRulers()
        {
            if (TopRulerContainer.Bounds.Width == 0 || LeftRulerContainer.Bounds.Height == 0) return;
            var matrix = MyZoomBorder.Matrix;
            var zoom = matrix.M11;
            RenderTopRuler(zoom, -matrix.M31 / zoom, matrix.M31);
            RenderLeftRuler(zoom, -matrix.M32 / zoom, matrix.M32);
        }

        private void RenderTopRuler(double zoom, double startValue, double offsetX)
        {
            TopRuler.Children.Clear();
            double step = zoom < 0.5 ? 500 : (zoom > 2 ? 50 : 100);
            double pixelStep = step * zoom;
            double firstVal = Math.Floor(startValue / step) * step;
            double firstPixel = (firstVal * zoom) + offsetX;

            for (double x = firstPixel; x < TopRulerContainer.Bounds.Width; x += pixelStep)
            {
                double val = Math.Round(firstVal + ((x - firstPixel) / pixelStep) * step);
                TopRuler.Children.Add(new Line { StartPoint = new Point(x, 12), EndPoint = new Point(x, 25), Stroke = Brushes.DimGray, StrokeThickness = 1 });
                var txt = new TextBlock { Text = val.ToString(), FontSize = 9, Foreground = Brushes.DimGray };
                Canvas.SetLeft(txt, x + 3); TopRuler.Children.Add(txt);
            }
        }

        private void RenderLeftRuler(double zoom, double startValue, double offsetY)
        {
            LeftRuler.Children.Clear();
            double step = zoom < 0.5 ? 500 : (zoom > 2 ? 50 : 100);
            double pixelStep = step * zoom;
            double firstVal = Math.Floor(startValue / step) * step;
            double firstPixel = (firstVal * zoom) + offsetY;

            for (double y = firstPixel; y < LeftRulerContainer.Bounds.Height; y += pixelStep)
            {
                double val = Math.Round(firstVal + ((y - firstPixel) / pixelStep) * step);
                LeftRuler.Children.Add(new Line { StartPoint = new Point(12, y), EndPoint = new Point(25, y), Stroke = Brushes.DimGray, StrokeThickness = 1 });
                var txt = new TextBlock { Text = val.ToString(), FontSize = 9, Foreground = Brushes.DimGray, RenderTransform = new RotateTransform(-90) };
                Canvas.SetTop(txt, y + 15); LeftRuler.Children.Add(txt);
            }
        }

        #endregion

        #region 6. الأوامر العامة (API & Drop)

        public void LoadDesign(Control rootControl) { DesignSurface.Content = rootControl; ClearSelection(); }

        private void DesignerSurfaceView_KeyDown(object? sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.R: MyZoomBorder.ResetMatrix(); break;
                case Key.F: MyZoomBorder.Fill(); break;
                case Key.T: MyZoomBorder.AutoFit(); break;
            }
        }

        private void DesignSurface_Drop(object? sender, DragEventArgs e)
        {
            if (e.Data.Contains("ControlType"))
            {
                string typeName = e.Data.Get("ControlType")?.ToString() ?? "";
                Type? t = Type.GetType(typeName);
                if (t != null && typeof(Control).IsAssignableFrom(t))
                {
                    var newCtrl = (Control)Activator.CreateInstance(t)!;
                    newCtrl.Width = 100; newCtrl.Height = 30;

                    if (DesignSurface.Content is Panel p)
                    {
                        var pos = e.GetPosition(p);
                        Canvas.SetLeft(newCtrl, pos.X); Canvas.SetTop(newCtrl, pos.Y);
                        p.Children.Add(newCtrl);
                        SelectControl(newCtrl);
                        DesignChanged?.Invoke(this, EventArgs.Empty);
                    }
                }
            }
        }

        private void DesignSurface_DragOver(object? sender, DragEventArgs e) => e.DragEffects = DragDropEffects.Copy;

        #endregion

        #region 7. محرك التحجيم (Resizing Logic)

        private void Resize_DragDelta(object? sender, VectorEventArgs e)
        {
            // 1. التأكد إن في عنصر مختار وإن اللي بينادي هو "Thumb" (مربع التحكم)
            if (_selectedControl == null || sender is not Thumb thumb) return;

            // 2. 🎯 السحر هنا: بنجيب نسبة الزووم الحالية من الـ ZoomBorder
            // بنقسم حركة الماوس على الزووم عشان الحركة تبقى "طلقة" ودقيقة مهما كبرت أو صغرت
            double zoomX = MyZoomBorder.ZoomX;
            double zoomY = MyZoomBorder.ZoomY;

            double deltaX = e.Vector.X / zoomX;
            double deltaY = e.Vector.Y / zoomY;

            // 3. قراءة الأبعاد الحالية بدقة
            double currentWidth = _selectedControl.Width.DefaultIfNaN(_selectedControl.Bounds.Width);
            double currentHeight = _selectedControl.Height.DefaultIfNaN(_selectedControl.Bounds.Height);

            double newWidth = currentWidth;
            double newHeight = currentHeight;
            double leftOffset = 0, topOffset = 0;

            // 4. تحديد أي اتجاه بيتم سحبه وتعديل الأبعاد والمكان بناءً عليه
            switch (thumb.Name)
            {
                case "TopLeft":
                    newWidth -= deltaX; newHeight -= deltaY; leftOffset = deltaX; topOffset = deltaY; break;
                case "TopRight":
                    newWidth += deltaX; newHeight -= deltaY; topOffset = deltaY; break;
                case "BottomLeft":
                    newWidth -= deltaX; newHeight += deltaY; leftOffset = deltaX; break;
                case "BottomRight":
                    newWidth += deltaX; newHeight += deltaY; break;
            }

            // 5. تطبيق التعديلات مع احترام "الحد الأدنى" للحجم (10 بكسل)
            if (newWidth > 10)
            {
                _selectedControl.Width = newWidth;
                if (leftOffset != 0) ApplyPositionOffset(_selectedControl, leftOffset, 0);
            }

            if (newHeight > 10)
            {
                _selectedControl.Height = newHeight;
                if (topOffset != 0) ApplyPositionOffset(_selectedControl, 0, topOffset);
            }

            // 6. تحديث برواز التحديد الأزرق فوراً
            UpdateAdornerPosition();

            // 7. إبلاغ السيستم إن التصميم اتغير (عشان الـ Undo/Redo أو حفظ الـ XAML)
            DesignChanged?.Invoke(this, EventArgs.Empty);
        }

        // دالة مساعدة لتحريك العنصر أثناء التحجيم (لتجنب تكرار الكود)
        private void ApplyPositionOffset(Control ctrl, double x, double y)
        {
            if (ctrl.Parent is Canvas)
            {
                if (x != 0) Canvas.SetLeft(ctrl, Canvas.GetLeft(ctrl).DefaultIfNaN() + x);
                if (y != 0) Canvas.SetTop(ctrl, Canvas.GetTop(ctrl).DefaultIfNaN() + y);
            }
            else
            {
                ctrl.Margin = new Thickness(ctrl.Margin.Left + x, ctrl.Margin.Top + y, 0, 0);
            }
        }

        #endregion

        #region 8. مخرجات الزووم والملاحة (Zoom & Navigation)

        public void SetZoomLevel(string zoomMode)
        {
            if (MyZoomBorder == null) return;

            // 1. حالة "Fit to Screen" (توسيط وتكبير ملائم)
            if (zoomMode == "Fit to Screen")
            {
                MyZoomBorder.Uniform();
                UpdateRulers(); // تحديث المساطر فوراً بعد الزووم
                return;
            }

            // 2. 🎯 تحويل النص لنسبة مئوية رقمية (استخدام switch expression أنظف)
            double targetZoom = zoomMode switch
            {
                "25%" => 0.25,
                "50%" => 0.50,
                "150%" => 1.50,
                "200%" => 2.00,
                "400%" => 4.00,
                _ => 1.0  // الافتراضي 100%
            };

            // 3. حساب نسبة الزووم النسبية (Relative Zoom)
            // القانون: النسبة المطلوبة / النسبة الحالية
            double currentZoom = MyZoomBorder.ZoomX;

            if (currentZoom > 0)
            {
                double relativeZoom = targetZoom / currentZoom;

                // 4. تحديد نقطة التكبير (المنتصف)
                // عشان الزووم يحصل من نص الشاشة بالظبط مش من الطرف
                double centerX = MyZoomBorder.Bounds.Width / 2;
                double centerY = MyZoomBorder.Bounds.Height / 2;

                // 5. تنفيذ الزووم
                MyZoomBorder.Zoom(relativeZoom, centerX, centerY);

                // تحديث المساطر والـ Adorner عشان يلاحقوا الزووم الجديد
                UpdateRulers();
                UpdateAdornerPosition();
            }
        }

        #endregion
    }

    // دالة مساعدة للتعامل مع القيم غير المعرفة
    public static class DoubleExtensions { public static double DefaultIfNaN(this double val, double def = 0) => double.IsNaN(val) ? def : val; }
}