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

namespace VisualEditor.Designer
{
    public partial class DesignerSurfaceView : UserControl
    {
        private Control? _selectedControl;
        private bool _isDraggingControl = false;
        private Point _dragStartMousePosition;
        private Point _dragStartControlPosition;
        private bool _hasMoved = false;
        private bool _isPreviewMode = false;


        private bool _isSelecting; // هل المستخدم بيشد مربع تحديد؟
        private Point _selectionStartPoint; // نقطة بداية الضغط
        private List<Control> _selectedControls = new List<Control>(); // قائمة العناصر المحددة

        // 📢 الأحداث (Events) اللي هنكلم بيها البرنامج بره
        public event EventHandler? DesignChanged;
        public event EventHandler<Control?>? SelectionChanged;

        // خاصية للحصول على التصميم الحالي
        public Control? RootDesign => DesignSurface.Content as Control;

        public DesignerSurfaceView()
        {
            InitializeComponent();

            // 1. أول ما المساطر تاخد حجم (Layout Updated) نرسمها
            TopRulerContainer.SizeChanged += (s, e) => UpdateRulers();
            LeftRulerContainer.SizeChanged += (s, e) => UpdateRulers();

            // 2. مراقبة الزوم والتحريك (Matrix)
            MyZoomBorder.PropertyChanged += (s, e) =>
            {
                if (e.Property.Name == nameof(MyZoomBorder.Matrix))
                {
                    UpdateRulers();
                }
            };


            // 1. ربط المربع الأزرق مع أي حركة سحب أو زووم
            MyZoomBorder.PropertyChanged += (s, e) =>
            {
                if (e.Property.Name == "ZoomX" || e.Property.Name == "ZoomY" ||
                    e.Property.Name == "OffsetX" || e.Property.Name == "OffsetY")
                {
                    UpdateAdornerPosition();
                }
               
            };
            
            // 2. تحديث المربع في حالة تغيير حجم الشاشة نفسها
            this.LayoutUpdated += (s, e) => UpdateAdornerPosition();
            MyZoomBorder.DoubleTapped += (s, e) =>
            {
                // لما تدوس مرتين يرجع التصميم في نص الشاشة بالظبط
                MyZoomBorder.AutoFit();
            };
            // تفعيل السحب والإفلات
            DragDrop.SetAllowDrop(DesignSurface, true);
            DesignSurface.AddHandler(DragDrop.DragOverEvent, DesignSurface_DragOver);
            DesignSurface.AddHandler(DragDrop.DropEvent, DesignSurface_Drop);

            // اعتراض أحداث الماوس
            DesignSurface.AddHandler(InputElement.PointerPressedEvent, DesignSurface_PreviewPointerPressed, RoutingStrategies.Tunnel);
            DesignSurface.AddHandler(InputElement.PointerMovedEvent, DesignSurface_PreviewPointerMoved, RoutingStrategies.Tunnel);
            DesignSurface.AddHandler(InputElement.PointerReleasedEvent, DesignSurface_PreviewPointerReleased, RoutingStrategies.Tunnel);
            // 1. تفعيل استقبال أحداث الكيبورد
            this.KeyDown += DesignerSurfaceView_KeyDown;
        }

        private void DesignerSurfaceView_KeyDown(object? sender, KeyEventArgs e)
        {
            if (MyZoomBorder == null) return;

            switch (e.Key)
            {
                case Key.R: // إعادة الوضع الافتراضي (Reset)
                    MyZoomBorder.ResetMatrix();
                    break;

                case Key.F: // ملء الشاشة بالتصميم (Fill)
                    MyZoomBorder.Fill();
                    break;

                case Key.U: // توسيط التصميم (Uniform)
                    MyZoomBorder.Uniform();
                    break;

                case Key.T: // التبديل التلقائي للزووم المناسب (AutoFit)
                    MyZoomBorder.AutoFit();
                    break;
            }
        }

        // ==========================================
        // 1. الواجهة الخارجية (API)
        // ==========================================
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

        public void SetPreviewMode(bool isPreview)
        {
            _isPreviewMode = isPreview;
            if (_isPreviewMode) ClearSelection();
        }

        // ==========================================
        // 2. محرك التحديد (Selection & Adorner)
        // ==========================================
        public void SelectControl(Control? control)
        {
            _selectedControl = control;
            if (control != null)
            {
                UpdateAdornerPosition();
                SelectionAdorner.IsVisible = true;
                AdornerCanvas.IsHitTestVisible = true;
            }
            // إبلاغ الشاشة الأساسية لتحديث الشجرة (Outline)
            SelectionChanged?.Invoke(this, control);
        }

        private void ClearSelection()
        {
            _selectedControl = null;
            SelectionAdorner.IsVisible = false;
            AdornerCanvas.IsHitTestVisible = false;
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
                if (current.TemplatedParent is Control parentControl)
                {
                    current = parentControl;
                    continue;
                }
                return current;
            }
            return null;
        }

        // ==========================================
        // 3. محرك الماوس (تحريك الكنترولات)
        // ==========================================
        private void DesignSurface_PreviewPointerPressed(object? sender, PointerPressedEventArgs e)
        {
            if (_isPreviewMode || !e.GetCurrentPoint(DesignSurface).Properties.IsLeftButtonPressed) return;

            if (e.Source is Visual sourceVisual && (sourceVisual == AdornerCanvas || AdornerCanvas.IsVisualAncestorOf(sourceVisual)))
                return;
            var pos = e.GetPosition(DesignSurface);
            var hitResult = DesignSurface.InputHitTest(pos);

            // لو ملمسناش عنصر أو لمسنا الـ DesignSurface نفسه (الخلفية)
            if (hitResult == DesignSurface || hitResult == null)
            {
                _isSelecting = true;
                _selectionStartPoint = pos;
                SelectionBox.IsVisible = true;

                // مسح التحديد القديم
                _selectedControls.Clear();
                HideAdorners();
            }
            Control? clickedControl = GetSelectableControl(e.Source as Control);

            if (clickedControl != null)
            {
                SelectControl(clickedControl);
                e.Handled = true;
                _isDraggingControl = true;
                _hasMoved = false;
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
            
            if (_isDraggingControl && _selectedControls.Count > 0)
            {
                var currentMousePos = e.GetPosition(DesignSurface);
                double deltaX = currentMousePos.X - _dragStartMousePosition.X;
                double deltaY = currentMousePos.Y - _dragStartMousePosition.Y;

                // تطبيق الـ Snap to Grid
                double snapSize = 10.0;
                double snappedDeltaX = Math.Round(deltaX / snapSize) * snapSize;
                double snappedDeltaY = Math.Round(deltaY / snapSize) * snapSize;

                foreach (var control in _selectedControls)
                {
                    // نحتاج لتخزين مكان كل عنصر عند بداية السحب في Dictionary مثلاً
                    // أو نعدل الـ Margin/Canvas.SetLeft بناءً على مكانه الحالي
                    if (control.Parent is Canvas)
                    {
                        double oldX = Canvas.GetLeft(control);
                        double oldY = Canvas.GetTop(control);
                        // تحريك بالنسبة لمكانه الأصلي
                    }
                }
            }
            if (_isDraggingControl && _selectedControl != null)
            {
                if (_isSelecting)
                {
                    var currentPos = e.GetPosition(DesignSurface);

                    // حساب أبعاد المربع (دعم السحب في كل الاتجاهات)
                    double x = Math.Min(_selectionStartPoint.X, currentPos.X);
                    double y = Math.Min(_selectionStartPoint.Y, currentPos.Y);
                    double width = Math.Abs(_selectionStartPoint.X - currentPos.X);
                    double height = Math.Abs(_selectionStartPoint.Y - currentPos.Y);

                    Canvas.SetLeft(SelectionBox, x);
                    Canvas.SetTop(SelectionBox, y);
                    SelectionBox.Width = width;
                    SelectionBox.Height = height;
                }


                var currentMousePos = e.GetPosition(DesignSurface);
                double deltaX = currentMousePos.X - _dragStartMousePosition.X;
                double deltaY = currentMousePos.Y - _dragStartMousePosition.Y;

                if (Math.Abs(deltaX) > 1 || Math.Abs(deltaY) > 1) _hasMoved = true;

                // 🎯 2. ميزة المغناطيس (Snap to Grid)
                // بنخلي العنصر "ينط" كل 10 بيكسل مثلاً عشان يمشي مع خطوط الشبكة بالظبط
                double snapSize = 10.0;
                double rawX = _dragStartControlPosition.X + deltaX;
                double rawY = _dragStartControlPosition.Y + deltaY;

                double snappedX = Math.Round(rawX / snapSize) * snapSize;
                double snappedY = Math.Round(rawY / snapSize) * snapSize;

                // 3. تطبيق الحركة
                if (_selectedControl.Parent is Canvas)
                {
                    Canvas.SetLeft(_selectedControl, snappedX);
                    Canvas.SetTop(_selectedControl, snappedY);
                }
                else
                {
                    _selectedControl.Margin = new Avalonia.Thickness(snappedX, snappedY, 0, 0);
                    _selectedControl.HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Left;
                    _selectedControl.VerticalAlignment = Avalonia.Layout.VerticalAlignment.Top;
                }

                UpdateAdornerPosition();
                e.Handled = true;
            }
        }

        private void DesignSurface_PreviewPointerReleased(object? sender, PointerReleasedEventArgs e)
        {


            if (_isSelecting)
            {
                _isSelecting = false;
                SelectionBox.IsVisible = false;

                var selectionRect = new Rect(Canvas.GetLeft(SelectionBox), Canvas.GetTop(SelectionBox), SelectionBox.Width, SelectionBox.Height);

                // 🎯 الحركة الصح هنا:
                // بنشوف مين هو المحتوى (الـ Page اللي اتحملت) وبنحاول نعاملها كـ Panel (عشان نوصل للـ Children)
                if (DesignSurface.Content is Panel mainPanel)
                {
                    foreach (var child in mainPanel.Children.OfType<Control>())
                    {
                        // تحويل حدود العنصر بالنسبة للـ DesignSurface عشان نعرف نقارنها بالمربع الأزرق
                        var transform = child.TranslatePoint(new Point(0, 0), DesignSurface);
                        if (transform.HasValue)
                        {
                            var childRect = new Rect(transform.Value.X, transform.Value.Y, child.Bounds.Width, child.Bounds.Height);

                            if (selectionRect.Intersects(childRect))
                            {
                                _selectedControls.Add(child);
                            }
                        }
                    }
                }

                // لو في عناصر اتحددت، نظهر الـ Adorner (لو حابب)
                if (_selectedControls.Count > 0)
                {
                    System.Diagnostics.Debug.WriteLine($"✅ تم تحديد {_selectedControls.Count} عناصر");
                    // هنا ممكن تنده دالة لتحديد "البرواز الكبير" للمجموعة
                }
            }
            //if (_isSelecting)
            //{
            //    _isSelecting = false;
            //    SelectionBox.IsVisible = false;

            //    // المستطيل النهائي للتحديد
            //    var selectionRect = new Rect(Canvas.GetLeft(SelectionBox), Canvas.GetTop(SelectionBox), SelectionBox.Width, SelectionBox.Height);

            //    // فحص كل العناصر في الـ Canvas
            //    foreach (var child in DesignSurface.Children.OfType<Control>())
            //    {
            //        // الحصول على حدود العنصر بالنسبة للـ DesignSurface
            //        var bounds = child.Bounds;
            //        if (selectionRect.Intersects(bounds))
            //        {
            //            _selectedControls.Add(child);
            //            // إظهار حدود تمييز لكل عنصر (اختياري)
            //        }
            //    }

            //    System.Diagnostics.Debug.WriteLine($"Selected {_selectedControls.Count} items.");
            //}
            if (_isDraggingControl)
            {
                _isDraggingControl = false;
                e.Handled = true;

                // 👈 السر هنا: لو الكنترول اتحرك، بنبلغ الشاشة الأم إن التصميم اتغير عشان تولد XAML جديد
                if (_hasMoved) DesignChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        // ==========================================
        // 4. محرك التكبير والتصغير (Resizing)
        // ==========================================
        private void Resize_DragDelta(object? sender, VectorEventArgs e)
        {
            if (_selectedControl == null || sender is not Thumb thumb) return;


           

            // 💡 السحر هنا: بنجيب نسبة الزووم الحالية
            double zoomX = MyZoomBorder.ZoomX;
            double zoomY = MyZoomBorder.ZoomY;

            // 💡 بنقسم حركة الماوس على الزووم عشان الحركة تبقى دقيقة
            double deltaX = e.Vector.X / zoomX;
            double deltaY = e.Vector.Y / zoomY;

            // مثال لتغيير الحجم لو سحبنا المربع اللي تحت على اليمين:
            if (thumb.Name == "BottomRight")
            {
                _selectedControl.Width = Math.Max(10, _selectedControl.Bounds.Width + deltaX);
                _selectedControl.Height = Math.Max(10, _selectedControl.Bounds.Height + deltaY);
            }

            

            double currentWidth = double.IsNaN(_selectedControl.Width) ? _selectedControl.Bounds.Width : _selectedControl.Width;
            double currentHeight = double.IsNaN(_selectedControl.Height) ? _selectedControl.Bounds.Height : _selectedControl.Height;

            double newWidth = currentWidth;
            double newHeight = currentHeight;
            double leftOffset = 0, topOffset = 0;

            if (thumb.Name == "TopLeft") { newWidth -= deltaX; newHeight -= deltaY; leftOffset = deltaX; topOffset = deltaY; }
            else if (thumb.Name == "TopRight") { newWidth += deltaX; newHeight -= deltaY; topOffset = deltaY; }
            else if (thumb.Name == "BottomLeft") { newWidth -= deltaX; newHeight += deltaY; leftOffset = deltaX; }
            else if (thumb.Name == "BottomRight") { newWidth += deltaX; newHeight += deltaY; }

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

            UpdateAdornerPosition();

            // 👈 إبلاغ الشاشة الأم بتغير الحجم
            DesignChanged?.Invoke(this, EventArgs.Empty);
        }

        // ==========================================
        // 5. محرك السحب والإفلات (Drag & Drop)
        // ==========================================
        private void DesignSurface_DragOver(object? sender, DragEventArgs e)
        {
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
                    if (DesignSurface.Content == null)
                        DesignSurface.Content = new Canvas { Background = Avalonia.Media.Brushes.Transparent };

                    var newControl = (Control)Activator.CreateInstance(controlType)!;
                    newControl.Width = 100;
                    newControl.Height = 30;
                    if (newControl is ContentControl cc) cc.Content = controlType.Name;
                    else if (newControl is TextBlock tb) tb.Text = controlType.Name;

                    Control? targetContainer = GetValidDropTarget(e.Source as Control);
                    if (targetContainer == null) return;

                    var dropPosition = e.GetPosition(targetContainer);

                    if (targetContainer is Panel targetPanel)
                    {
                        if (targetPanel is Canvas)
                        {
                            Canvas.SetLeft(newControl, dropPosition.X);
                            Canvas.SetTop(newControl, dropPosition.Y);
                        }
                        else
                        {
                            newControl.Margin = new Avalonia.Thickness(0);
                        }
                        targetPanel.Children.Add(newControl);
                    }
                    else if (targetContainer is ContentControl targetContentControl)
                    {
                        targetContentControl.Content = newControl;
                    }

                    SelectControl(newControl);

                    // 👈 إبلاغ الشاشة الأم بنزول كنترول جديد
                    DesignChanged?.Invoke(this, EventArgs.Empty);
                }
            }
        }

        private Control? GetValidDropTarget(Control? hitControl)
        {
            Control? current = hitControl;
            while (current != null && current != DesignSurface)
            {
                if (current is Panel) return current;
                if (current is ContentControl cc && cc.Content == null && current != DesignSurface.Content) return current;
                current = current.Parent as Control;
            }
            return DesignSurface.Content as Control;
        }


        // ضيف الدالة دي جوه كلاس DesignerSurfaceView
        public void SetZoomLevel(string zoomMode)
        {
            if (MyZoomBorder == null) return;

            // 1. لو اختار Fit to Screen، الدوكيومنت بيقول نستخدم دالة Uniform
            if (zoomMode == "Fit to Screen")
            {
                MyZoomBorder.Uniform();
                return;
            }

            // 2. تحديد نسبة الزووم المطلوبة
            double targetZoom = 1.0;
            if (zoomMode == "25%") targetZoom = 0.25;
            else if (zoomMode == "50%") targetZoom = 0.50;
            else if (zoomMode == "100%") targetZoom = 1.0;

            // 3. قراءة الزووم الحالي (زي ما الدوكيومنت قال باستخدام ZoomX)
            double currentZoom = MyZoomBorder.ZoomX;

            if (currentZoom > 0)
            {
                // حساب النسبة اللي هنضرب فيها عشان نوصل للرقم المطلوب
                double relativeZoom = targetZoom / currentZoom;

                // حساب منتصف الشاشة عشان التكبير والتصغير يكون من النص بالظبط
                double centerX = MyZoomBorder.Bounds.Width / 2;
                double centerY = MyZoomBorder.Bounds.Height / 2;

                // 4. استدعاء دالة Zoom كـ Method ونديها الإحداثيات
                MyZoomBorder.Zoom(relativeZoom, centerX, centerY);
            }
        }


        private void UpdateRulers()
        {
            if (TopRulerContainer.Bounds.Width == 0 || LeftRulerContainer.Bounds.Height == 0) return;

            // الحصول على المصفوفة ومعكوسها
            var matrix = MyZoomBorder.Matrix;
            var zoom = matrix.M11;

            // 🎯 الحركة الذكية: بنعرف النقطة (0,0) بتاعة المسطرة بتقابل كام في التصميم
            // بنقسم الـ Offset على الزوم وبنعكس الإشارة
            double startContentX = -matrix.M31 / zoom;
            double startContentY = -matrix.M32 / zoom;

            RenderTopRuler(zoom, startContentX, matrix.M31);
           RenderLeftRuler(zoom, startContentY, matrix.M32);
        }

        private void RenderTopRuler(double zoom, double startValue, double offsetX)
        {
            TopRuler.Children.Clear();

            // تحديد المسافة بين الشرطات (مثلاً كل 100 وحدة)
            double step = 100;
            if (zoom < 0.5) step = 500;
            else if (zoom > 2) step = 50;

            double pixelStep = step * zoom;

            // حساب أول نقطة "مضاعفة للـ step" قبل بداية الشاشة مباشرة
            double firstVisibleValue = Math.Floor(startValue / step) * step;

            // تحويل القيمة الحقيقية لمكان على الشاشة (Pixel Position)
            double firstPixelPos = (firstVisibleValue * zoom) + offsetX;

            for (double x = firstPixelPos; x < TopRulerContainer.Bounds.Width; x += pixelStep)
            {
                // القيمة اللي هتتكتب (0, 100, 200...)
                double val = Math.Round(firstVisibleValue + ((x - firstPixelPos) / pixelStep) * step);

                // رسم الشرطة
                TopRuler.Children.Add(new Line
                {
                    StartPoint = new Point(x, 12),
                    EndPoint = new Point(x, 25),
                    Stroke = Brushes.DimGray,
                    StrokeThickness = 1
                });

                // رسم الرقم
                var txt = new TextBlock
                {
                    Text = val.ToString(),
                    FontSize = 9,
                    Foreground = Brushes.DimGray
                };
                Canvas.SetLeft(txt, x + 3);
                Canvas.SetTop(txt, 0);
                TopRuler.Children.Add(txt);
            }
        }

        private void RenderLeftRuler(double zoom, double startValue, double offsetY)
        {
            LeftRuler.Children.Clear();

            // نفس الخطوات البرمجية بناءً على الزوم
            double step = 100;
            if (zoom < 0.5) step = 500;
            else if (zoom > 2) step = 50;

            double pixelStep = step * zoom;

            // حساب أول نقطة مرئية رأسياً
            double firstVisibleValue = Math.Floor(startValue / step) * step;
            double firstPixelPos = (firstVisibleValue * zoom) + offsetY;

            for (double y = firstPixelPos; y < LeftRulerContainer.Bounds.Height; y += pixelStep)
            {
                double val = Math.Round(firstVisibleValue + ((y - firstPixelPos) / pixelStep) * step);

                // 1. رسم الشرطة العرضية
                LeftRuler.Children.Add(new Line
                {
                    StartPoint = new Point(12, y),
                    EndPoint = new Point(25, y),
                    Stroke = Brushes.DimGray,
                    StrokeThickness = 1
                });

                // 2. رسم الرقم مع تدويره -90 درجة
                var txt = new TextBlock
                {
                    Text = val.ToString(),
                    FontSize = 9,
                    Foreground = Brushes.DimGray,
                    // تدوير النص ليكون موازياً للمسطرة
                    RenderTransform = new RotateTransform(-90),
                    RenderTransformOrigin = new RelativePoint(0, 0, RelativeUnit.Relative)
                };

                Canvas.SetLeft(txt, 2); // ترك مسافة بسيطة من الحافة
                Canvas.SetTop(txt, y + 15); // إزاحة بسيطة لتحسين المظهر
                LeftRuler.Children.Add(txt);
            }
        }
 
        private void HideAdorners()
        {
            // 1. إخفاء المربع الأزرق اللي فيه الـ Thumbs (TopLeft, BottomRight, etc.)
            if (SelectionAdorner != null)
            {
                SelectionAdorner.IsVisible = false;
            }

            // 2. اختياري: لو عامل تمييز (Highlight) للعناصر المختارة، بنشيله هنا
            // مثلاً لو كنت بتغير الـ BorderBrush بتاع العناصر المحددة
            foreach (var control in _selectedControls)
            {
                // نرجع الشكل الطبيعي للعنصر
            }
        }
    }
}
