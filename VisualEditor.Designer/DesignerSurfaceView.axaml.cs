using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.PanAndZoom;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
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

        // 📢 الأحداث (Events) اللي هنكلم بيها البرنامج بره
        public event EventHandler? DesignChanged;
        public event EventHandler<Control?>? SelectionChanged;

        // خاصية للحصول على التصميم الحالي
        public Control? RootDesign => DesignSurface.Content as Control;

        public DesignerSurfaceView()
        {
            InitializeComponent();
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
            if (_isDraggingControl && _selectedControl != null)
            {
                var currentMousePos = e.GetPosition(DesignSurface);
                double deltaX = currentMousePos.X - _dragStartMousePosition.X;
                double deltaY = currentMousePos.Y - _dragStartMousePosition.Y;

                if (Math.Abs(deltaX) > 1 || Math.Abs(deltaY) > 1) _hasMoved = true;

                if (_selectedControl.Parent is Canvas)
                {
                    Canvas.SetLeft(_selectedControl, _dragStartControlPosition.X + deltaX);
                    Canvas.SetTop(_selectedControl, _dragStartControlPosition.Y + deltaY);
                }
                else
                {
                    _selectedControl.Margin = new Avalonia.Thickness(_dragStartControlPosition.X + deltaX, _dragStartControlPosition.Y + deltaY, 0, 0);
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

            double deltaX = e.Vector.X;
            double deltaY = e.Vector.Y;

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
    }
}