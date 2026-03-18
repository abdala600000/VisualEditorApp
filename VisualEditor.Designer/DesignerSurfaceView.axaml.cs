using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.PanAndZoom;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Shapes;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;
using System;
using System.Collections.Generic;
using System.Linq;
using VisualEditor.Designer.Services;
using VisualEditor.Core.Messages;
using VisualEditor.Core.Models;

namespace VisualEditor.Designer
{
    public partial class DesignerSurfaceView : UserControl
    {
        #region 1. الأرصدة والحقول (Fields)

        // الكنترولات
        private Control? _selectedControl;
        private List<Control> _selectedControls = new List<Control>();
        // قاموس بيسجل: كل عنصر كان فين بالظبط قبل ما نبدأ نشد بالماوس
        private Dictionary<Control, Point> _groupStartPositions = new Dictionary<Control, Point>();

        // حالات السحب والحركة
        private bool _isDraggingControl = false;
        private bool _isSelecting = false;
        private bool _hasMoved = false;
        private bool _isPreviewMode = false;

        // النقاط والإحداثيات
        private Point _dragStartMousePosition;
        private Point _dragStartControlPosition;
        private Point _selectionStartPoint;
        private Point _currentPointerPosition;
        private Point _lastAdornerMousePos;

        public event EventHandler? DesignChanged;
        public event EventHandler<Control?>? SelectionChanged;
        public event EventHandler<(Control Element, Control Parent)>? ElementAdded;
        public event EventHandler<Control>? ElementRemoved;
        public Control? SelectedControl => _selectedControl;

        // خصائص الوصول
        public Control? RootDesign => DesignSurface.Content as Control;

        private Control? _internalClipboard; // الحافظة الداخلية للبرنامج

        // 🎯 خطوط المحاذاة الذكية
        private Line _snapLineX = new Line { Stroke = Brushes.DeepSkyBlue, StrokeThickness = 1, StrokeDashArray = new Avalonia.Collections.AvaloniaList<double> { 4, 4 }, IsVisible = false };
        private Line _snapLineY = new Line { Stroke = Brushes.DeepSkyBlue, StrokeThickness = 1, StrokeDashArray = new Avalonia.Collections.AvaloniaList<double> { 4, 4 }, IsVisible = false };
        #endregion

        #region 2. البداية والتعريف (Constructor)

        public DesignerSurfaceView()
        {
            InitializeComponent();
            SetupDesignerEvents();
            CreateContextMenu(); // 👈 ضيف السطر ده هنا


            // إضافة خطوط المحاذاة لطبقة الرسم العلوية
            AdornerCanvas.Children.Add(_snapLineX);
            AdornerCanvas.Children.Add(_snapLineY);
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

            // السحب والإفلات (Drag & Drop) على DropLayer بدلاً من DesignSurface
            // DropLayer طبقة شفافة فوق المحتوى تضمن استقبال Drop حتى لو المحتوى فارغ
            DragDrop.SetAllowDrop(DropLayer, true);
            DropLayer.AddHandler(DragDrop.DragOverEvent, DesignSurface_DragOver);
            DropLayer.AddHandler(DragDrop.DropEvent, DesignSurface_Drop);

            // اعتراض أحداث الماوس (Tunneling) على DropLayer لضمان التحكم قبل الكنترولات الفرعية
            DropLayer.AddHandler(PointerPressedEvent, DesignSurface_PreviewPointerPressed, RoutingStrategies.Tunnel);
            DropLayer.AddHandler(PointerMovedEvent, DesignSurface_PreviewPointerMoved, RoutingStrategies.Tunnel);
            DropLayer.AddHandler(PointerReleasedEvent, DesignSurface_PreviewPointerReleased, RoutingStrategies.Tunnel);

            // نبقي DesignSurface يستقبل Drop أيضاً كـ fallback
            DragDrop.SetAllowDrop(DesignSurface, true);
            DesignSurface.AddHandler(DragDrop.DragOverEvent, DesignSurface_DragOver);
            DesignSurface.AddHandler(DragDrop.DropEvent, DesignSurface_Drop);
        }

        #endregion

        #region 3. التحكم في التحديد (Selection Logic)

        public void SelectControl(Control? control, bool isAdditive = false)
        {
            if (control == null)
            {
                ClearSelection();
                return;
            }

            if (!isAdditive)
            {
                _selectedControls.Clear();
            }

            if (!_selectedControls.Contains(control))
            {
                _selectedControls.Add(control);
            }

            _selectedControl = control;

            UpdateAdornerPosition();
            SelectionAdorner.IsVisible = true;
            AdornerCanvas.IsHitTestVisible = true;

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
            if (_selectedControls.Count == 0)
            {
                SelectionAdorner.IsVisible = false;
                AdornerCanvas.IsHitTestVisible = false;
                return;
            }

            // إخفاء مقبض الدوران لو فيه أكتر من عنصر
            var rotateHandle = SelectionAdorner.FindControl<Control>("RotateHandle");
            if (rotateHandle != null) rotateHandle.IsVisible = _selectedControls.Count == 1;

            if (_selectedControls.Count == 1)
            {
                // 🎯 حالة العنصر الواحد: تدوير ومطابقة دقيقة (بديلة لـ DesignerItem)
                var ctrl = _selectedControls[0];

                // نحسب الـ transform بدون RenderTransform عشان نحصل على الموضع الأصلي للعنصر
                var savedTransform = ctrl.RenderTransform;
                ctrl.RenderTransform = null;
                var transform = ctrl.TransformToVisual(AdornerCanvas);
                ctrl.RenderTransform = savedTransform;

                if (transform.HasValue)
                {
                    double w = ctrl.Bounds.Width;
                    double h = ctrl.Bounds.Height;

                    // مركز العنصر في الـ AdornerCanvas (بدون دوران)
                    var centerGlobal = new Point(w / 2, h / 2).Transform(transform.Value);

                    SelectionAdorner.Width = w;
                    SelectionAdorner.Height = h;

                    // وضع الإطار الأزرق بحيث مركزه = مركز العنصر
                    Canvas.SetLeft(SelectionAdorner, centerGlobal.X - (w / 2));
                    Canvas.SetTop(SelectionAdorner, centerGlobal.Y - (h / 2));

                    // تطبيق نفس زاوية الدوران على الـ Adorner
                    if (savedTransform is RotateTransform rt)
                    {
                        SelectionAdorner.RenderTransform = new RotateTransform(rt.Angle);
                    }
                    else if (savedTransform is TransformGroup tg)
                    {
                        var rotate = tg.Children.OfType<RotateTransform>().FirstOrDefault();
                        SelectionAdorner.RenderTransform = rotate != null ? new RotateTransform(rotate.Angle) : null;
                    }
                    else
                    {
                        SelectionAdorner.RenderTransform = null;
                    }

                    SelectionAdorner.RenderTransformOrigin = new RelativePoint(0.5, 0.5, RelativeUnit.Relative);
                }
            }
            else
            {
                // 🎯 حالة التحديد المتعدد: صندوق AABB يحيط بكل العناصر
                SelectionAdorner.RenderTransform = null;
                Rect? groupBounds = null;
                foreach (var ctrl in _selectedControls)
                {
                    var transform = ctrl.TransformToVisual(AdornerCanvas);
                    if (transform.HasValue)
                    {
                        var bounds = new Rect(new Point(0, 0), ctrl.Bounds.Size);
                        var rectInAdorner = bounds.TransformToAABB(transform.Value);
                        if (groupBounds == null) groupBounds = rectInAdorner;
                        else groupBounds = groupBounds.Value.Union(rectInAdorner);
                    }
                }

                if (groupBounds != null)
                {
                    var finalBounds = groupBounds.Value;
                    SelectionAdorner.Width = finalBounds.Width;
                    SelectionAdorner.Height = finalBounds.Height;
                    Canvas.SetLeft(SelectionAdorner, finalBounds.X);
                    Canvas.SetTop(SelectionAdorner, finalBounds.Y);
                }
            }

            SelectionAdorner.IsVisible = true;
            AdornerCanvas.IsHitTestVisible = true;
        }

        private Control? GetSelectableControl(Control? element)
        {
            Control? current = element;
            while (current != null)
            {
                if (current == DesignSurface) return null;
                // تجاهل DropLayer - هو مجرد طبقة شفافة
                if (current == DropLayer) { current = current.Parent as Control; continue; }

                // 🎯 لو العنصر هو الـ Content الأساسي (Root)، نرجعه عادي عشان يظهر في الـ Properties
                if (current == DesignSurface.Content) return current;

                if (current.TemplatedParent is Control parentControl) { current = parentControl; continue; }
                return current;
            }
            return null;
        }

        #endregion

        #region 4. التعامل مع الماوس (Dragging & Multi-Selection)

        private void DesignSurface_PreviewPointerPressed(object? sender, PointerPressedEventArgs e)
        {
            // 🎯 ضمان الحصول على التركيز (Focus) عشان زر Delete يشتغل
            MyZoomBorder.Focus();

            if (e.GetCurrentPoint(DesignSurface).Properties.IsMiddleButtonPressed) return;

            var props = e.GetCurrentPoint(DesignSurface).Properties;

            // 🎯 إذا جاء الحدث من DropLayer، نعمل HitTest على DesignSurface لإيجاد الكنترول الفعلي
            Control? sourceControl = e.Source as Control;
            if (sourceControl == DropLayer || sourceControl?.Name == "DropLayer")
            {
                var posOnSurface = e.GetPosition(DesignSurface);
                sourceControl = DesignSurface.InputHitTest(posOnSurface) as Control ?? DesignSurface;
            }

            // لو ضغط كليك يمين
            if (props.IsRightButtonPressed)
            {
                Control? clicked = GetSelectableControl(sourceControl);
                if (clicked != null) SelectControl(clicked); // حدد العنصر الأول
                return; // سيب الـ ContextMenu يفتح لوحده
            }

            if (_isPreviewMode || !e.GetCurrentPoint(DesignSurface).Properties.IsLeftButtonPressed) return;

            // تجاهل الضغط لو كان على نقاط التحجيم (Adorner)
            if (e.Source is Visual v && (v == AdornerCanvas || AdornerCanvas.IsVisualAncestorOf(v))) return;

            var pos = e.GetPosition(DesignSurface);
            Control? clickedControl = GetSelectableControl(sourceControl);

            // التحقق من ضغط زر الـ Ctrl للتحديد المتعدد
            bool isCtrlPressed = e.KeyModifiers.HasFlag(KeyModifiers.Control);

            if (clickedControl != null)
            {
                // 1. حالة الضغط على كنترول (زرار، تكست، إلخ)
                if (isCtrlPressed)
                {
                    // إضافة أو إزالة من التحديد الحالي
                    if (_selectedControls.Contains(clickedControl))
                        _selectedControls.Remove(clickedControl);
                    else
                        _selectedControls.Add(clickedControl);

                    SelectControl(clickedControl, true);
                }
                else
                {
                    if (!_selectedControls.Contains(clickedControl))
                    {
                        _selectedControls.Clear();
                        _selectedControls.Add(clickedControl);
                        SelectControl(clickedControl);
                    }
                }

                _isDraggingControl = true;
                _hasMoved = false;
                _dragStartMousePosition = pos;

                _groupStartPositions.Clear();
                foreach (var ctrl in _selectedControls)
                {
                    _groupStartPositions[ctrl] = ctrl.Parent is Canvas
                        ? new Point(Canvas.GetLeft(ctrl).DefaultIfNaN(), Canvas.GetTop(ctrl).DefaultIfNaN())
                        : new Point(ctrl.Margin.Left, ctrl.Margin.Top);
                }

                e.Handled = true;
            }
            else
            {
                // المستخدم ضغط على الخلفية الفاضية - ابدأ Rubber Band Selection
                ClearSelection();
                _isSelecting = true;
                _selectionStartPoint = e.GetPosition(AdornerCanvas); // بمساحة AdornerCanvas
                Canvas.SetLeft(SelectionBox, _selectionStartPoint.X);
                Canvas.SetTop(SelectionBox, _selectionStartPoint.Y);
                SelectionBox.Width = 0;
                SelectionBox.Height = 0;
                SelectionBox.IsVisible = true;
                e.Pointer.Capture(DropLayer);
            }
        }

        private void DesignSurface_PreviewPointerMoved(object? sender, PointerEventArgs e)
        {
            // لو في دوران أو إمالة جارية، نتجاهل حركة الماوس على السطح
            if (_isRotating || _isSkewing) return;

            var currentPos = e.GetPosition(DesignSurface); // الإحداثيات بالنسبة للمصمم (Zoomed)
            _currentPointerPosition = currentPos;

            // 🎯 تحديث موضع الماوس بالنسبة للـ AdornerCanvas (Unzoomed) للعمليات الرياضية
            var adPos = e.GetPosition(AdornerCanvas);
            _lastAdornerMousePos = adPos;

            if (_isSelecting)
            {
                // تحديث مربع التحديد بمساحة AdornerCanvas (غير متأثرة بالزووم)
                UpdateSelectionBox(e.GetPosition(AdornerCanvas));
            }
            else if (_isDraggingControl && _selectedControls.Count > 0)
            {
                double deltaX = currentPos.X - _dragStartMousePosition.X;
                double deltaY = currentPos.Y - _dragStartMousePosition.Y;

                if (Math.Abs(deltaX) > 1 || Math.Abs(deltaY) > 1) _hasMoved = true;

                double snapSize = 10.0;

                // 🎯 تحريك كل عنصر في المجموعة بناءً على مكانه الأصلي + فرق حركة الماوس
                foreach (var ctrl in _selectedControls)
                {
                    if (!_groupStartPositions.ContainsKey(ctrl)) continue;

                    Point startPos = _groupStartPositions[ctrl];
                    double snappedX = Math.Round((startPos.X + deltaX) / snapSize) * snapSize;
                    double snappedY = Math.Round((startPos.Y + deltaY) / snapSize) * snapSize;

                    if (ctrl.Parent is Canvas)
                    {
                        Canvas.SetLeft(ctrl, snappedX);
                        Canvas.SetTop(ctrl, snappedY);
                    }
                    else
                    {
                        ctrl.Margin = new Thickness(snappedX, snappedY, 0, 0);
                    }
                }

                // تحديث الـ Adorner لو فيه عنصر واحد بس هو الـ Main
                UpdateAdornerPosition();
            }
        }

        private void DesignSurface_PreviewPointerReleased(object? sender, PointerReleasedEventArgs e)
        {
            // لو في دوران أو إمالة جارية، نتركها لمعالجاتها
            if (_isRotating || _isSkewing) return;

            if (_isSelecting)
            {
                ProcessMultiSelection();
                _isSelecting = false;
                SelectionBox.IsVisible = false;
                e.Pointer.Capture(null);

                // لو ما اتحدد حاجة، حدد الـ Root
                if (_selectedControls.Count == 0 && DesignSurface.Content is Control root)
                    SelectControl(root);
                else if (_selectedControls.Count > 0)
                    UpdateAdornerPosition();
            }

            if (_isDraggingControl)
            {
                _isDraggingControl = false;
                if (_hasMoved)
                {
                    // 🎯 أخذ "لقطة" للأماكن الجديدة
                    var oldPositions = new Dictionary<Control, Point>(_groupStartPositions);
                    var newPositions = new Dictionary<Control, Point>();

                    foreach (var ctrl in _selectedControls)
                    {
                        newPositions[ctrl] = ctrl.Parent is Canvas
                            ? new Point(Canvas.GetLeft(ctrl).DefaultIfNaN(), Canvas.GetTop(ctrl).DefaultIfNaN())
                            : new Point(ctrl.Margin.Left, ctrl.Margin.Top);
                    }

                    // 🎯 تسجيل العملية
                    HistoryService.Instance.RegisterChange(
                        undo: () => RestorePositions(oldPositions),
                        redo: () => RestorePositions(newPositions)
                    );

                    DesignChanged?.Invoke(this, EventArgs.Empty);
                }
                if (_hasMoved) DesignChanged?.Invoke(this, EventArgs.Empty);
            }
        }
        // دالة مساعدة لتطبيق الأماكن (عشان الـ Undo/Redo يستخدموها)
        private void RestorePositions(Dictionary<Control, Point> positions)
        {
            foreach (var kvp in positions)
            {
                var ctrl = kvp.Key;
                var pos = kvp.Value;

                if (ctrl.Parent is Canvas)
                {
                    Canvas.SetLeft(ctrl, pos.X);
                    Canvas.SetTop(ctrl, pos.Y);
                }
                else
                {
                    ctrl.Margin = new Thickness(pos.X, pos.Y, 0, 0);
                }
            }
            UpdateAdornerPosition();
            DesignChanged?.Invoke(this, EventArgs.Empty);
        }
        private void ProcessMultiSelection()
        {
            // selectionRect بمساحة AdornerCanvas (حيث يعيش SelectionBox)
            var selectionRect = new Rect(
                Canvas.GetLeft(SelectionBox),
                Canvas.GetTop(SelectionBox),
                SelectionBox.Width,
                SelectionBox.Height);

            if (selectionRect.Width < 2 && selectionRect.Height < 2) return; // مجرد كليك بدون سحب

            _selectedControls.Clear();

            // نجمع كل الكنترولات من شجرة الـ DesignSurface
            var allControls = new List<Control>();
            CollectAllControls(DesignSurface.Content as Control, allControls);

            foreach (var ctrl in allControls)
            {
                // نحول موضع الكنترول إلى مساحة AdornerCanvas للمقارنة الصحيحة
                var transform = ctrl.TransformToVisual(AdornerCanvas);
                if (!transform.HasValue) continue;

                var ctrlBounds = new Rect(new Point(0, 0), ctrl.Bounds.Size);
                var ctrlInAdorner = ctrlBounds.TransformToAABB(transform.Value);

                if (selectionRect.Intersects(ctrlInAdorner))
                    _selectedControls.Add(ctrl);
            }

            if (_selectedControls.Count > 0)
                _selectedControl = _selectedControls[0];

            // إبلاغ الشاشة الأم بتغيير التحديد
            SelectionChanged?.Invoke(this, _selectedControl);
        }

        // يجمع كل الكنترولات القابلة للتحديد من الشجرة (بدون الـ Root نفسه)
        private void CollectAllControls(Control? root, List<Control> result)
        {
            if (root == null || root == DesignSurface.Content) 
            {
                // ندخل للأبناء مباشرة
                if (root is Panel p)
                    foreach (var child in p.Children.OfType<Control>())
                        CollectAllControls(child, result);
                return;
            }

            result.Add(root);

            if (root is Panel panel)
                foreach (var child in panel.Children.OfType<Control>())
                    CollectAllControls(child, result);
        }

        private void UpdateSelectionBox(Point currentPos)
        {
            if (SelectionBox == null) return;

            double x = Math.Min(_selectionStartPoint.X, currentPos.X);
            double y = Math.Min(_selectionStartPoint.Y, currentPos.Y);
            double width  = Math.Abs(_selectionStartPoint.X - currentPos.X);
            double height = Math.Abs(_selectionStartPoint.Y - currentPos.Y);

            Canvas.SetLeft(SelectionBox, x);
            Canvas.SetTop(SelectionBox, y);
            SelectionBox.Width  = width;
            SelectionBox.Height = height;
            SelectionBox.IsVisible = true;
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

        public void LoadDesign(Control rootControl)
        {
            DesignSurface.Content = rootControl;
            ClearSelection();

            // ضبط حجم طبقة المحتوى بناءً على حجم الـ root
            Dispatcher.UIThread.Post(() =>
            {
                double w = double.IsNaN(rootControl.Width)  ? rootControl.Bounds.Width  : rootControl.Width;
                double h = double.IsNaN(rootControl.Height) ? rootControl.Bounds.Height : rootControl.Height;

                // قيم افتراضية لو لم يُحدد حجم
                if (w <= 0) w = 800;
                if (h <= 0) h = 600;

                DesignSurfaceWrapper.Width  = w;
                DesignSurfaceWrapper.Height = h;

                // توسيط طبقة المحتوى في منتصف الـ Canvas الكبير
                double canvasW = 4000;
                double canvasH = 3000;
                Canvas.SetLeft(DesignSurfaceWrapper, (canvasW - w) / 2);
                Canvas.SetTop(DesignSurfaceWrapper,  (canvasH - h) / 2);

                UpdateAdornerPosition();
            }, Avalonia.Threading.DispatcherPriority.Loaded);
        }
        private void DesignerSurfaceView_KeyDown(object? sender, KeyEventArgs e)
        {
            if (e.Key == Key.Delete || e.Key == Key.Back)
            {
                // مسح كل العناصر المحددة من الواجهة
                foreach (var ctrl in _selectedControls.ToList())
                {
                    if (ctrl.Parent is Panel p)
                    {
                        p.Children.Remove(ctrl);
                        ElementRemoved?.Invoke(this, ctrl);
                    }
                    else if (ctrl.Parent is ContentControl cc)
                    {
                        cc.Content = null;
                        ElementRemoved?.Invoke(this, ctrl);
                    }
                }
                ClearSelection();
            }
            if (e.KeyModifiers == KeyModifiers.Control)
            {
                if (e.Key == Key.C) CopySelected();
                if (e.Key == Key.V) PasteSelected();
                if (e.Key == Key.D) DuplicateSelected();

                // 🎯 التراجع والإعادة
                if (e.Key == Key.Z) HistoryService.Instance.Undo();
                if (e.Key == Key.Y) HistoryService.Instance.Redo();
            }
            switch (e.Key)
            {
                case Key.R: MyZoomBorder.ResetMatrix(); break;
                case Key.F: MyZoomBorder.Fill(); break;
                case Key.T: MyZoomBorder.AutoFit(); break;

            }
        }



        public void SetPreviewMode(bool isPreview)
        {
            _isPreviewMode = isPreview;

            // أول ما ندخل وضع المشاهدة، بنخفي المربع الأزرق ونلغي أي تحديد
            if (_isPreviewMode)
            {
                ClearSelection();
            }
        }
        #endregion

        #region 7. محرك التحجيم (Resizing Logic)

        // نقطة البداية للتوسيع (بمساحة العنصر)
        private Size _resizeStartSize;
        private Point _resizeStartPos;

        private void Resize_DragDelta(object? sender, VectorEventArgs e)
        {
            if (_selectedControl == null || sender is not Thumb thumb) return;

            double zoom = MyZoomBorder.ZoomX;

            // تحويل حركة الماوس إلى مساحة العنصر (مع مراعاة الدوران والزووم)
            // نهج WpfDesigner: نعكس الـ RenderTransform على متجه الحركة
            double dx = e.Vector.X / zoom;
            double dy = e.Vector.Y / zoom;

            double angle = (_selectedControl.RenderTransform is RotateTransform rt2)
                ? rt2.Angle * Math.PI / 180.0 : 0;

            if (angle != 0)
            {
                double cos = Math.Cos(-angle);
                double sin = Math.Sin(-angle);
                double rx = dx * cos - dy * sin;
                double ry = dx * sin + dy * cos;
                dx = rx; dy = ry;
            }

            double currentW = _selectedControl.Width.DefaultIfNaN(_selectedControl.Bounds.Width);
            double currentH = _selectedControl.Height.DefaultIfNaN(_selectedControl.Bounds.Height);

            double newW = currentW, newH = currentH;
            double offX = 0, offY = 0;

            switch (thumb.Name)
            {
                case "TopLeft": newW -= dx; newH -= dy; offX = dx; offY = dy; break;
                case "TopRight": newW += dx; newH -= dy; offY = dy; break;
                case "BottomLeft": newW -= dx; newH += dy; offX = dx; break;
                case "BottomRight": newW += dx; newH += dy; break;
            }

            if (newW > 10)
            {
                _selectedControl.Width = newW;
                // نحرك الموضع فقط إذا لم يكن هناك دوران (مثل WpfDesigner)
                if (offX != 0 && angle == 0)
                    ApplyPositionOffset(_selectedControl, offX, 0);
            }
            if (newH > 10)
            {
                _selectedControl.Height = newH;
                if (offY != 0 && angle == 0)
                    ApplyPositionOffset(_selectedControl, 0, offY);
            }

            UpdateAdornerPosition();
        }

        private void Resize_DragCompleted(object? sender, VectorEventArgs e)
        {
            if (_selectedControl == null) return;
            UpdateAdornerPosition();
            DesignChanged?.Invoke(this, EventArgs.Empty);
        }

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

        #region 8. مخرجات الدوران (Rotation Logic) - نهج WpfDesigner

        private bool _isRotating = false;
        private double _rotateInitialAngle;   // الزاوية عند بدء السحب
        private Vector _rotateStartVector;    // المتجه من المركز إلى نقطة البداية
        private Point _rotateCenterInParent; // مركز العنصر في مساحة الـ parent

        private void RotateHandle_PointerPressed(object? sender, PointerPressedEventArgs e)
        {
            if (_selectedControl == null) return;
            if (!e.GetCurrentPoint(AdornerCanvas).Properties.IsLeftButtonPressed) return;

            // ── نهج WpfDesigner: نحسب المركز والمتجه الابتدائي ──
            double w = _selectedControl.Bounds.Width;
            double h = _selectedControl.Bounds.Height;

            // مركز العنصر في مساحة الـ parent (Canvas أو Panel)
            if (_selectedControl.Parent is Canvas parentCanvas)
            {
                double left = Canvas.GetLeft(_selectedControl).DefaultIfNaN();
                double top = Canvas.GetTop(_selectedControl).DefaultIfNaN();
                _rotateCenterInParent = new Point(left + w / 2, top + h / 2);
            }
            else
            {
                _rotateCenterInParent = new Point(
                    _selectedControl.Margin.Left + w / 2,
                    _selectedControl.Margin.Top + h / 2);
            }

            // الزاوية الحالية
            _rotateInitialAngle = (_selectedControl.RenderTransform is RotateTransform rt)
                ? rt.Angle : 0;

            // نقطة البداية في مساحة الـ AdornerCanvas
            var startPoint = e.GetPosition(AdornerCanvas);
            // نحول مركز العنصر إلى مساحة الـ AdornerCanvas
            var parentTransform = (_selectedControl.Parent as Control)?.TransformToVisual(AdornerCanvas);
            Point centerInAdorner = parentTransform.HasValue
                ? _rotateCenterInParent.Transform(parentTransform.Value)
                : startPoint;

            _rotateStartVector = new Vector(startPoint.X - centerInAdorner.X,
                                            startPoint.Y - centerInAdorner.Y);

            _isRotating = true;
            e.Pointer.Capture(AdornerCanvas);
            e.Handled = true;
        }

        private void RotateHandle_PointerMoved(object? sender, PointerEventArgs e)
        {
            if (_isSkewing) { SkewHandle_PointerMoved(sender, e); return; }
            if (!_isRotating || _selectedControl == null) return;

            // مركز العنصر في مساحة الـ AdornerCanvas
            var parentTransform = (_selectedControl.Parent as Control)?.TransformToVisual(AdornerCanvas);
            double w = _selectedControl.Bounds.Width;
            double h = _selectedControl.Bounds.Height;

            Point centerInAdorner;
            if (parentTransform.HasValue)
                centerInAdorner = _rotateCenterInParent.Transform(parentTransform.Value);
            else
                centerInAdorner = e.GetPosition(AdornerCanvas); // fallback

            var currentPoint = e.GetPosition(AdornerCanvas);
            var currentVector = new Vector(currentPoint.X - centerInAdorner.X,
                                           currentPoint.Y - centerInAdorner.Y);

            // ── نهج WpfDesigner: AngleBetween بين المتجه الابتدائي والحالي ──
            double angleBetween = AngleBetween(_rotateStartVector, currentVector);
            double destAngle = _rotateInitialAngle + angleBetween;

            // Snapping: كل 15 درجة بدون Ctrl، حر مع Ctrl (عكس WpfDesigner لأن Shift أشيع)
            bool isFree = e.KeyModifiers.HasFlag(KeyModifiers.Shift);
            if (!isFree)
                destAngle = Math.Round(destAngle / 15.0) * 15.0;

            _selectedControl.RenderTransformOrigin = new RelativePoint(0.5, 0.5, RelativeUnit.Relative);

            // نحافظ على الـ Skew الحالي عند تغيير الدوران
            var currentSkew = GetCurrentSkewTransform();
            if (currentSkew.AngleX == 0 && currentSkew.AngleY == 0)
            {
                _selectedControl.RenderTransform = new RotateTransform(destAngle);
            }
            else
            {
                var group = new TransformGroup();
                group.Children.Add(new SkewTransform(currentSkew.AngleX, currentSkew.AngleY));
                group.Children.Add(new RotateTransform(destAngle));
                _selectedControl.RenderTransform = group;
            }

            UpdateAdornerPosition();
            e.Handled = true;
        }

        private void RotateHandle_PointerReleased(object? sender, PointerReleasedEventArgs e)
        {
            if (_isSkewing) { SkewHandle_PointerReleased(sender, e); return; }
            if (!_isRotating) return;
            _isRotating = false;
            e.Pointer.Capture(null);
            e.Handled = true;

            if (_selectedControl != null)
            {
                UpdateAdornerPosition();
                DesignChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        /// <summary>
        /// يحسب الزاوية بين متجهين (مثل WpfDesigner's Vector.AngleBetween)
        /// </summary>
        private static double AngleBetween(Vector v1, Vector v2)
        {
            double sin = v1.X * v2.Y - v2.X * v1.Y;
            double cos = v1.X * v2.X + v1.Y * v2.Y;
            return Math.Atan2(sin, cos) * (180.0 / Math.PI);
        }

        #endregion

        #region 8b. مقابض الإمالة (Skew Logic)

        private bool _isSkewing = false;
        private bool _isSkewX;           // true = SkewX، false = SkewY
        private Point _skewStartPoint;
        private double _skewInitialAngleX;
        private double _skewInitialAngleY;

        private void SkewHandle_PointerPressed(object? sender, PointerPressedEventArgs e)
        {
            if (_selectedControl == null) return;
            if (!e.GetCurrentPoint(AdornerCanvas).Properties.IsLeftButtonPressed) return;

            _isSkewX = (sender as Avalonia.Controls.Shapes.Ellipse)?.Name == "SkewXHandle";
            _skewStartPoint = e.GetPosition(AdornerCanvas);

            // احفظ قيم الـ Skew الحالية
            var current = GetCurrentSkewTransform();
            _skewInitialAngleX = current.AngleX;
            _skewInitialAngleY = current.AngleY;

            _isSkewing = true;
            e.Pointer.Capture(AdornerCanvas);
            e.Handled = true;
        }

        private void SkewHandle_PointerMoved(object? sender, PointerEventArgs e)
        {
            if (!_isSkewing || _selectedControl == null) return;

            var currentPoint = e.GetPosition(AdornerCanvas);
            double delta = _isSkewX
                ? (currentPoint.X - _skewStartPoint.X) * 0.3   // SkewX: حركة أفقية
                : (currentPoint.Y - _skewStartPoint.Y) * 0.3;  // SkewY: حركة رأسية

            double newAngleX = _isSkewX ? _skewInitialAngleX + delta : _skewInitialAngleX;
            double newAngleY = _isSkewX ? _skewInitialAngleY : _skewInitialAngleY + delta;

            // Snapping كل 5 درجات بدون Shift
            if (!e.KeyModifiers.HasFlag(KeyModifiers.Shift))
            {
                newAngleX = Math.Round(newAngleX / 5.0) * 5.0;
                newAngleY = Math.Round(newAngleY / 5.0) * 5.0;
            }

            // تطبيق TransformGroup يجمع Skew + Rotate الحالي
            ApplySkewTransform(_selectedControl, newAngleX, newAngleY);
            UpdateAdornerPosition();
            e.Handled = true;
        }

        private void SkewHandle_PointerReleased(object? sender, PointerReleasedEventArgs e)
        {
            if (!_isSkewing) return;
            _isSkewing = false;
            e.Pointer.Capture(null);
            e.Handled = true;
            DesignChanged?.Invoke(this, EventArgs.Empty);
        }

        private (double AngleX, double AngleY) GetCurrentSkewTransform()
        {
            if (_selectedControl?.RenderTransform is TransformGroup tg)
            {
                var skew = tg.Children.OfType<SkewTransform>().FirstOrDefault();
                if (skew != null) return (skew.AngleX, skew.AngleY);
            }
            else if (_selectedControl?.RenderTransform is SkewTransform st)
            {
                return (st.AngleX, st.AngleY);
            }
            return (0, 0);
        }

        private void ApplySkewTransform(Control ctrl, double angleX, double angleY)
        {
            // نحافظ على الـ RotateTransform الموجودة إن وجدت
            double rotateAngle = 0;
            if (ctrl.RenderTransform is RotateTransform rt) rotateAngle = rt.Angle;
            else if (ctrl.RenderTransform is TransformGroup tg2)
            {
                var rt2 = tg2.Children.OfType<RotateTransform>().FirstOrDefault();
                if (rt2 != null) rotateAngle = rt2.Angle;
            }

            ctrl.RenderTransformOrigin = new RelativePoint(0.5, 0.5, RelativeUnit.Relative);

            if (angleX == 0 && angleY == 0 && rotateAngle == 0)
            {
                ctrl.RenderTransform = null;
                return;
            }

            if (rotateAngle == 0)
            {
                ctrl.RenderTransform = new SkewTransform(angleX, angleY);
            }
            else
            {
                var group = new TransformGroup();
                group.Children.Add(new SkewTransform(angleX, angleY));
                group.Children.Add(new RotateTransform(rotateAngle));
                ctrl.RenderTransform = group;
            }
        }

        #endregion

        #region 9. مخرجات الزووم والملاحة (Zoom & Navigation)

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

        #region 9. محرك السحب والإفلات (Drag & Drop Logic)

        private void DesignSurface_DragOver(object? sender, DragEventArgs e)
        {
            // التأكد إن البيانات المسحوبة هي نوع كنترول (ControlType)
            e.DragEffects = e.Data.Contains("ControlType") ? DragDropEffects.Copy : DragDropEffects.None;
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
                    // 1. لو السطح فاضي خالص، بننشئ Canvas كحاوية أساسية
                    if (DesignSurface.Content == null)
                        DesignSurface.Content = new Canvas { Background = Brushes.Transparent };

                    // 2. إنشاء نسخة جديدة من الكنترول (Reflection)
                    Control? newControl = null;
                    try
                    {
                        newControl = (Control)Activator.CreateInstance(controlType)!;
                    }
                    catch (Exception ex)
                    {
                        MessageBus.Send(SystemDiagnosticMessage.Create(DiagnosticSeverity.Error, "DESIGN001", $"Failed to instantiate {typeName}: {ex.Message}"));
                        return;
                    }

                    newControl.Width = 100;
                    newControl.Height = 30;

                    // ضبط النص الافتراضي للكنترول بناءً على نوعه
                    if (newControl is ContentControl cc) cc.Content = controlType.Name;
                    else if (newControl is TextBlock tb) tb.Text = controlType.Name;

                    // 🎯 تسمية تلقائية فريدة لضمان عمل الـ Sync
                    string baseName = controlType.Name;
                    int counter = 1;
                    while (FindControlByName(DesignSurface.Content as Control, $"{baseName}_{counter}") != null) counter++;
                    newControl.Name = $"{baseName}_{counter}";

                    // 3. 🎯 البحث عن حاوية صالحة
                    // لو الـ Drop جاء من DropLayer، نعمل HitTest على DesignSurface لنجد الكنترول الفعلي تحته
                    var dropPosOnSurface = e.GetPosition(DesignSurface);
                    Control? hitControl = null;
                    if (e.Source is Border b && b.Name == "DropLayer")
                    {
                        // HitTest على DesignSurface لإيجاد الكنترول الفعلي
                        hitControl = DesignSurface.InputHitTest(dropPosOnSurface) as Control;
                    }
                    else
                    {
                        hitControl = e.Source as Control;
                    }

                    Control? targetContainer = GetValidDropTarget(hitControl);
                    if (targetContainer == null) return;

                    // 🎯 ضمان وجود اسم للحاوية لنجاح عملية المزامنة مع الـ XAML
                    // نستثني الـ Root والـ Simulated Shells لأن الـ XAML Patcher بيفهم إن الأب الجذري كود فارغ
                    if (string.IsNullOrEmpty(targetContainer.Name) &&
                        targetContainer != DesignSurface.Content &&
                        targetContainer.Name != "SimulatedContentArea" &&
                        targetContainer.Name != "SimulatedWindowFrame")
                    {
                        try
                        {
                            string baseParentName = targetContainer.GetType().Name;
                            int pCounter = 1;
                            while (FindControlByName(DesignSurface.Content as Control, $"{baseParentName}_{pCounter}") != null) pCounter++;
                            targetContainer.Name = $"{baseParentName}_{pCounter}";
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"Warning: Could not set auto-name for container: {ex.Message}");
                        }
                    }

                    var dropPosition = e.GetPosition(targetContainer);

                    // 4. إضافة الكنترول للحاوية المناسبة
                    if (targetContainer is Panel targetPanel)
                    {
                        if (targetPanel is Canvas)
                        {
                            Canvas.SetLeft(newControl, dropPosition.X);
                            Canvas.SetTop(newControl, dropPosition.Y);
                        }
                        else
                        {
                            newControl.Margin = new Thickness(0);
                        }
                        targetPanel.Children.Add(newControl);
                    }
                    else if (targetContainer is ContentControl targetContentControl)
                    {
                        // 🎯 إذا كانت الحاوية تقبل عنصراً واحداً فقط، نقوم باستبدال المحتوى القديم
                        targetContentControl.Content = newControl;
                    }
                    else if (targetContainer is Decorator decoratorTarget)
                    {
                        // 🎯 نفس الشيء للـ Decorator (مثل Border)
                        decoratorTarget.Child = newControl;
                    }

                    // 5. تحديد الكنترول الجديد فوراً عشان المربع الأزرق يظهر عليه
                    SelectControl(newControl);

                    // 6. إبلاغ الشاشة الأم بإضافة عنصر جديد
                    ElementAdded?.Invoke(this, (newControl, targetContainer));
                }
            }
        }

        /// <summary>
        /// دالة ذكية للبحث في شجرة العناصر عن أول "حاوية" (Panel أو ContentControl) صالحة لاستقبال العنصر الجديد
        /// </summary>
        private Control? GetValidDropTarget(Control? hitControl)
        {
            if (DesignSurface.Content == null) return null;

            Control? current = hitControl;
            while (current != null && current != DesignSurface)
            {
                // 🎯 تجاوز الإطارات الوهمية للنافذة والذهاب مباشرة للمحتوى الحقيقي
                if (current.Name == "SimulatedWindowFrame" || current.Name == "SimulatedContentArea" || current.Name == "SimulatedTitleBar")
                {
                    var contentArea = FindControlByName(DesignSurface.Content as Control, "SimulatedContentArea") as Border;
                    if (contentArea != null)
                    {
                        if (contentArea.Child is Control actualContent)
                            return actualContent;
                        else
                            return contentArea;
                    }
                }

                // 🎯 أي Panel (مثل Grid, StackPanel, Canvas) هو هدف صالح دائماً
                if (current is Panel) return current;

                // 🎯 الـ ContentControl (مثل Button, ScrollViewer) والـ Decorator (مثل Border)
                // أهداف صالحة حتى لو كان فيها محتوى (سنقوم باستبداله في دالة الـ Drop)
                if (current is ContentControl && current != DesignSurface.Content) return current;
                if (current is Decorator && current != DesignSurface.Content) return current;

                current = current.Parent as Control;
            }
            return DesignSurface.Content as Control;
        }

        private Control? FindControlByName(Control? root, string name)
        {
            if (root == null) return null;
            if (root.Name == name) return root;

            if (root is Panel p)
            {
                foreach (var child in p.Children.OfType<Control>())
                {
                    var found = FindControlByName(child, name);
                    if (found != null) return found;
                }
            }
            else if (root is ContentControl cc && cc.Content is Control childContent)
            {
                return FindControlByName(childContent, name);
            }
            return null;
        }

        #endregion

        #region 10. القائمة المختصرة (Context Menu Logic)

        private void CreateContextMenu()
        {
            // إنشاء القائمة
            var menu = new ContextMenu();

            // 1. Bring to Front (إحضار للمقدمة)
            var bringToFront = new MenuItem { Header = "Bring to Front", Icon = "🔼" };
            bringToFront.Click += (s, e) => ChangeZOrder(true);

            // 2. Send to Back (إرسال للخلف)
            var sendToBack = new MenuItem { Header = "Send to Back", Icon = "🔽" };
            sendToBack.Click += (s, e) => ChangeZOrder(false);

            // 3. Duplicate (تكرار)
            var duplicate = new MenuItem { Header = "Duplicate", Icon = "👥", InputGesture = new KeyGesture(Key.D, (KeyModifiers)RawInputModifiers.Control) };
            duplicate.Click += (s, e) => DuplicateSelected();

            // 4. Delete (مسح)
            var delete = new MenuItem { Header = "Delete", Icon = "🗑️", InputGesture = new KeyGesture(Key.Delete) };
            delete.Click += (s, e) => DeleteSelected();

            var copy = new MenuItem { Header = "Copy", InputGesture = new KeyGesture(Key.C, (KeyModifiers)RawInputModifiers.Control) };
            copy.Click += (s, e) => CopySelected();

            var paste = new MenuItem { Header = "Paste", InputGesture = new KeyGesture(Key.V, (KeyModifiers)RawInputModifiers.Control) };
            paste.Click += (s, e) => PasteSelected();

            menu.ItemsSource = new List<MenuItem> { bringToFront, sendToBack, new MenuItem { Header = "-" }, duplicate, delete, copy, paste };

            // ربط المنيو بالسطح التصميمي
            DesignSurface.ContextMenu = menu;
        }

        private void ChangeZOrder(bool toFront)
        {
            if (_selectedControl == null || _selectedControl.Parent is not Panel parent) return;

            // في Avalonia، الترتيب في قائمة Children هو اللي بيحدد مين فوق مين
            parent.Children.Remove(_selectedControl);
            if (toFront)
                parent.Children.Add(_selectedControl); // إضافته في الآخر يخليه فوق الكل
            else
                parent.Children.Insert(0, _selectedControl); // إضافته في الأول يخليه تحت الكل

            DesignChanged?.Invoke(this, EventArgs.Empty);
        }

        private void DuplicateSelected()
        {
            var targets = _selectedControls.Count > 0 ? _selectedControls.ToList() : (_selectedControl != null ? new List<Control> { _selectedControl } : new List<Control>());
            if (targets.Count == 0 || targets[0].Parent is not Panel parent) return;

            try
            {
                var newSelection = new List<Control>();
                foreach (var target in targets)
                {
                    // 1. إنشاء نسخة جديدة
                    var clone = CloneControl(target);

                    // 2. تحديد مكان النسخة الجديدة (إزاحة بسيطة 20 بيكسل)
                    double newX = Canvas.GetLeft(target).DefaultIfNaN() + 20;
                    double newY = Canvas.GetTop(target).DefaultIfNaN() + 20;

                    Canvas.SetLeft(clone, newX);
                    Canvas.SetTop(clone, newY);

                    // 3. إضافة النسخة للمصمم
                    parent.Children.Add(clone);
                    newSelection.Add(clone);
                }

                // 4. تحديد العناصر الجديدة كمجموعة
                _selectedControls.Clear();
                foreach (var ctrl in newSelection) SelectControl(ctrl, true);

                DesignChanged?.Invoke(this, EventArgs.Empty);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Clone error: {ex.Message}");
                MessageBus.Send(SystemDiagnosticMessage.Create(DiagnosticSeverity.Error, "DESIGN002", $"Clone error: {ex.Message}"));
            }
        }

        private void DeleteSelected()
        {
            if (_selectedControl == null) return;

            var targets = _selectedControls.Count > 0 ? _selectedControls.ToList() : new List<Control> { _selectedControl };
            var parent = targets.First().Parent as Panel; // نحتفظ بالـ Parent

            if (parent == null) return;

            // 1. تنفيذ المسح الفعلي
            foreach (var ctrl in targets) parent.Children.Remove(ctrl);
            ClearSelection();
            DesignChanged?.Invoke(this, EventArgs.Empty);

            // 2. 🎯 تسجيل العملية في الـ History
            HistoryService.Instance.RegisterChange(
                undo: () =>
                {
                    // التراجع: نرجع العناصر للـ Parent ونحددهم
                    foreach (var ctrl in targets) parent.Children.Add(ctrl);
                    foreach (var ctrl in targets) SelectControl(ctrl);
                    DesignChanged?.Invoke(this, EventArgs.Empty);
                },
                redo: () =>
                {
                    // الإعادة: نمسحهم تاني ونلغي التحديد
                    foreach (var ctrl in targets) parent.Children.Remove(ctrl);
                    ClearSelection();
                    DesignChanged?.Invoke(this, EventArgs.Empty);
                }
            );
        }

        #endregion

        #region 11. عمليات النسخ واللصق (Clipboard Operations)

        // 1. 📄 نَسخ (Copy)
        private void CopySelected()
        {
            if (_selectedControl == null) return;

            // إحنا هنا بنعمل نسخة من الكنترول في الميموري
            _internalClipboard = CloneControl(_selectedControl);
            System.Diagnostics.Debug.WriteLine($"Copied: {_selectedControl.GetType().Name}");
        }

        // 2. 📋 لَصق (Paste)
        private void PasteSelected()
        {
            if (_internalClipboard == null || DesignSurface.Content is not Panel parent) return;

            // بناخد نسخة جديدة من اللي في الحافظة عشان نقدر نكرر اللصق كذا مرة
            var newControl = CloneControl(_internalClipboard);

            // إزاحة بسيطة عشان اللصق ميبقاش فوق النسخة القديمة بالظبط
            double lastX = Canvas.GetLeft(_internalClipboard).DefaultIfNaN();
            double lastY = Canvas.GetTop(_internalClipboard).DefaultIfNaN();

            Canvas.SetLeft(newControl, lastX + 20);
            Canvas.SetTop(newControl, lastY + 20);

            parent.Children.Add(newControl);
            SelectControl(newControl); // حدد العنصر الجديد فوراً

            DesignChanged?.Invoke(this, EventArgs.Empty);
        }

        //// 3. 👥 تكرار (Duplicate) - هي عبارة عن Copy ثم Paste فوراً
        //private void DuplicateSelected()
        //{
        //    if (_selectedControl == null) return;
        //    CopySelected();
        //    PasteSelected();
        //}

        /// <summary>
        /// دالة سحرية لعمل نسخة طبق الأصل من الكنترول (Deep Clone)
        /// </summary>
        private Control CloneControl(Control source)
        {
            var type = source.GetType();
            var clone = (Control)Activator.CreateInstance(type)!;

            // نسخ الخصائص الأساسية
            clone.Width = source.Width;
            clone.Height = source.Height;
            // clone.Background = source.Background;
            clone.Opacity = source.Opacity;

            // لو الكنترول ليه محتوى (زي الـ Button أو TextBlock)
            if (clone is ContentControl cc && source is ContentControl sourceCC)
                cc.Content = sourceCC.Content;
            else if (clone is TextBlock tb && source is TextBlock sourceTB)
                tb.Text = sourceTB.Text;

            return clone;
        }

        #endregion
    }

    // دالة مساعدة للتعامل مع القيم غير المعرفة
    public static class DoubleExtensions { public static double DefaultIfNaN(this double val, double def = 0) => double.IsNaN(val) ? def : val; }
}