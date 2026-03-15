using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Threading;
using System;
using System.Xml;
using VisualEditor.CodeEditor;
using VisualEditor.Core;
using VisualEditor.Core.Messages;
using VisualEditor.Designer;
using VisualEditorApp.ViewModels;
using VisualEditorApp.ViewModels.Documents;
using VisualEditorApp.ViewModels.Tools;
using System.Linq;

namespace VisualEditorApp.Views.Documents
{
    public partial class WorkspaceView : UserControl
    {
        private bool _isUpdating = false;

        public static WorkspaceView? Instance { get; private set; }
        private EventHandler<(System.Reflection.PropertyInfo Prop, object? Value, Avalonia.Controls.Control Target)>? _propertySub;

        public WorkspaceView()
        {
            InitializeComponent();
            Instance = this;

            InitializeControls();

            DataContextChanged += (s, e) =>
            {
                if (DataContext is WorkspaceViewModel vm)
                {
                    SetXamlText(vm.Text, vm.FilePath);
                }
            };

            MessageBus.ControlSelected += (message) =>
            {
                if (message.SenderName == "Outline" && message.SelectedControl != null)
                {
                    Dispatcher.UIThread.InvokeAsync(() =>
                    {
                        MyDesignSurface?.SelectControl(message.SelectedControl);
                    });
                }
            };
            // 👂 الاشتراك في إشارة الـ Build
            MessageBus.ProjectBuilt += (m) =>
            {
                // لما الإشارة تيجي، بنطلب من المصمم يعيد الرسم
                Dispatcher.UIThread.InvokeAsync(() =>
                {
                    LiveDesignerCompiler.Refresh(); // تحميل الـ DLLs الجديدة
                    TriggerRender(); // نده دالة الرسم بتاعتك تانى
                });
            };
        }
        private void TriggerRender()
        {
            // الكود اللي بياخد الـ Text الحالي ويبعته للـ RenderLiveXaml
            // this.ResultControl = LiveDesignerCompiler.RenderLiveXaml(this.Text, this.FilePath);
        }
        
        // ربط نافذة الخصائص بالمصمم
        private void HookPropertiesWindow()
        {
            MessageBus.PropertyChanged += (message) =>
            {
                // Patch property values instantly
                if (MyCodeEditor != null && message.Target != null && message.Property != null)
                {
                    string propName = message.Property.Name;
                    string newVal = message.Value?.ToString() ?? "";
                    string currentXml = MyCodeEditor.Text;

                    string newXml = XamlDOMPatcher.PatchProperty(currentXml, message.Target, propName, newVal);
                    if (currentXml != newXml)
                    {
                        MyCodeEditor.SetXamlText(newXml);
                    }
                }
            };
        }

        private void InitializeControls()
        {
            HookPropertiesWindow();



            if (MyCodeEditor != null)
            {
                MyCodeEditor.XamlTextChanged += MyCodeEditor_XamlTextChanged;
            }

            if (MyDesignSurface != null)
            {
                MyDesignSurface.DesignChanged += MyDesignSurface_DesignChanged;
                MyDesignSurface.SelectionChanged += MyDesignSurface_SelectionChanged;
            }
        }

        private void BtnViewMode_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            var mainGrid = this.FindControl<Grid>("MainGrid");
            var btnDesign = this.FindControl<Button>("BtnDesign");
            var btnSplit = this.FindControl<Button>("BtnSplit");
            var btnXaml = this.FindControl<Button>("BtnXaml");
            var designSurface = this.FindControl<Avalonia.Controls.Control>("MyDesignSurface");
            var codeEditor = this.FindControl<Avalonia.Controls.Control>("MyCodeEditor");

            if (sender is Button clickedBtn && clickedBtn.Tag is string mode && mainGrid != null)
            {
                // تصفير الألوان
                if (btnDesign != null) { btnDesign.Background = Avalonia.Media.Brushes.Transparent; btnDesign.FontWeight = Avalonia.Media.FontWeight.Normal; }
                if (btnSplit != null) { btnSplit.Background = Avalonia.Media.Brushes.Transparent; btnSplit.FontWeight = Avalonia.Media.FontWeight.Normal; }
                if (btnXaml != null) { btnXaml.Background = Avalonia.Media.Brushes.Transparent; btnXaml.FontWeight = Avalonia.Media.FontWeight.Normal; }

                clickedBtn.Background = Avalonia.Media.Brush.Parse("#3E3E42");
                clickedBtn.FontWeight = Avalonia.Media.FontWeight.Bold;

                // الأبعاد: شريط الأدوات (Auto)، المصمم (*)، السحب (5)، المحرر (*)
                if (mode == "Design")
                {
                    if (designSurface != null) designSurface.IsVisible = true;
                    if (codeEditor != null) codeEditor.IsVisible = false;
                    mainGrid.RowDefinitions = RowDefinitions.Parse("Auto, *, 0, 0");
                }
                else if (mode == "Split")
                {
                    if (designSurface != null) designSurface.IsVisible = true;
                    if (codeEditor != null) codeEditor.IsVisible = true;
                    mainGrid.RowDefinitions = RowDefinitions.Parse("Auto, *, 5, *");
                }
                else if (mode == "XAML")
                {
                    if (designSurface != null) designSurface.IsVisible = false;
                    if (codeEditor != null) codeEditor.IsVisible = true;
                    mainGrid.RowDefinitions = RowDefinitions.Parse("Auto, 0, 0, *");
                }
            }
        }

        private void MyDesignSurface_DesignChanged(object? sender, EventArgs e)
        {
            if (_isUpdating || MyDesignSurface == null || MyCodeEditor == null) return;
            _isUpdating = true;

            try
            {
                var designer = MyDesignSurface;
                if (designer != null)
                {
                    // get selected controls, typically just 1 in DesignerSurfaceView
                    var field = typeof(VisualEditor.Designer.DesignerSurfaceView).GetField("_selectedControl", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    if (field?.GetValue(designer) is Control targetControl)
                    {
                        string xml = MyCodeEditor.Text;
                        
                        // Patch position and size
                        if (targetControl.Parent is Canvas)
                        {
                            xml = XamlDOMPatcher.PatchProperty(xml, targetControl, "Canvas.Left", Canvas.GetLeft(targetControl).ToString());
                            xml = XamlDOMPatcher.PatchProperty(xml, targetControl, "Canvas.Top", Canvas.GetTop(targetControl).ToString());
                        }
                        else
                        {
                            xml = XamlDOMPatcher.PatchProperty(xml, targetControl, "Margin", $"{targetControl.Margin.Left},{targetControl.Margin.Top},0,0");
                        }
                        
                        xml = XamlDOMPatcher.PatchProperty(xml, targetControl, "Width", targetControl.Width.ToString());
                        xml = XamlDOMPatcher.PatchProperty(xml, targetControl, "Height", targetControl.Height.ToString());
                        
                        if (xml != MyCodeEditor.Text)
                        {
                            MyCodeEditor.SetXamlText(xml);
                        }
                    }
                }
            }
            finally
            {
                _isUpdating = false;
            }
        }

        private void MyCodeEditor_XamlTextChanged(object? sender, string newXamlText)
        {


            if (_isUpdating || MyDesignSurface == null) return;
            _isUpdating = true;

            try
            {
               //  SetXamlText(newXamlText, "");
              
            }
            catch
            {
            }
            finally
            {
                _isUpdating = false;
            }
        }

        private void MyDesignSurface_SelectionChanged(object? sender, Control? selectedControl)
        {
            if (selectedControl != null)
            {
                MessageBus.Send(new ControlSelectedMessage(selectedControl, "Properties"));
            }
        }

        // 👈 ضفنا filePath هنا
        public void SetXamlText(string xml, string filePath)
        {
            if (MyCodeEditor == null || MyDesignSurface == null) return;

            MyCodeEditor.SetXamlText(xml);

            try
            {
                // 👈 دلوقتي بنبعت النص والمسار مع بعض للمحرك
                var newControl = LiveDesignerCompiler.RenderLiveXaml(xml, filePath);

                if (newControl != null)
                {
                    // 2. استخراج الـ Design DataContext (نحفظه على جنب الأول)
                    object designContext = null;
                    if (newControl is Avalonia.Controls.Control originalControl)
                    {
                        designContext = Avalonia.Controls.Design.GetDataContext(originalControl);
                    }

                    Avalonia.Controls.Control finalElementToDisplay = null;

                    // 3. كود استخراج وتغليف العنصر
                    if (newControl is Avalonia.Controls.Window window)
                    {
                        // لو اللي جاي Window، بنفصل المحتوى بتاعه ونحطه جوه Border
                        var windowContent = window.Content as Avalonia.Controls.Control;
                        window.Content = null;

                        var fakeWindow = new Avalonia.Controls.Border
                        {
                            Background = window.Background ?? Avalonia.Media.Brushes.White,
                            Width = double.IsNaN(window.Width) ? 800 : window.Width,
                            Height = double.IsNaN(window.Height) ? 450 : window.Height,
                            Child = windowContent,
                            BoxShadow = Avalonia.Media.BoxShadows.Parse("0 5 15 0 #40000000")
                        };

                        finalElementToDisplay = fakeWindow;
                    }
                    else if (newControl is Avalonia.Controls.Control ctrl)
                    {
                        // 🎯 السحر هنا: بنحاول نسحب لون الخلفية بناءً على نوع الكنترول الحقيقي
                        Avalonia.Media.IBrush bgBrush = Avalonia.Media.Brushes.White; // اللون الافتراضي

                        if (ctrl is TemplatedControl templatedCtrl)
                        {
                            bgBrush = templatedCtrl.Background ?? bgBrush;
                        }
                        else if (ctrl is Avalonia.Controls.Panel panel)
                        {
                            bgBrush = panel.Background ?? bgBrush;
                        }

                        // 🎯 تغليف الكنترول جوه البوردر
                        var wrapperBorder = new Avalonia.Controls.Border
                        {
                            Background = bgBrush,
                            // بناخد عرض وطول الكنترول، لو مش متحددين بنديله حجم افتراضي 800x450
                            Width = double.IsNaN(ctrl.Width) ? 800 : ctrl.Width,
                            Height = double.IsNaN(ctrl.Height) ? 450 : ctrl.Height,
                            Child = ctrl, // الكنترول نفسه بيتحط جوه البوردر
                            BoxShadow = Avalonia.Media.BoxShadows.Parse("0 5 15 0 #40000000")
                        };

                        finalElementToDisplay = wrapperBorder;
                    }

                    // 🎯 4. السحر هنا: نطبق الـ DataContext على العنصر النهائي اللي هيظهر للمستخدم
                    if (designContext != null && finalElementToDisplay != null)
                    {
                        finalElementToDisplay.DataContext = designContext;

                        // التأكد إن المحتوى الداخلي كمان أخد الـ DataContext
                        if (finalElementToDisplay is Avalonia.Controls.Border b && b.Child != null)
                        {
                            b.Child.DataContext = designContext;
                        }
                    }

                    if (finalElementToDisplay != null)
                    {
                        MyDesignSurface.LoadDesign(finalElementToDisplay);
                        MessageBus.Send(new DesignTreeUpdatedMessage(finalElementToDisplay));
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error Initial Rendering: {ex.Message}");
            }
        }

        private void ZoomComboBox_SelectionChanged(object? sender, SelectionChangedEventArgs e)
        {


            if (ZoomComboBox?.SelectedItem is ComboBoxItem item && item.Content != null)
            {
                string? zoomText = item.Content.ToString();
                if (zoomText != null)
                {
                    MyDesignSurface?.SetZoomLevel(zoomText);
                }
            }
        }

        private void OnPreviewButton_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            if (sender is Avalonia.Controls.Primitives.ToggleButton toggleBtn)
            {
                // بنعرف الزرار مضغوط ولا لأ
                bool isPreviewOn = toggleBtn.IsChecked == true;

                // 🎯 بنبعت الحالة للمصمم
                if (MyDesignSurface != null)
                {
                    MyDesignSurface.SetPreviewMode(isPreviewOn);

                    // تغيير شكل الزرار عشان اليوزر يحس بالتغيير
                    toggleBtn.Background = isPreviewOn ? Avalonia.Media.Brushes.LightGreen : Avalonia.Media.Brushes.Transparent;
                    toggleBtn.Content = isPreviewOn ? "✏️ Edit Mode" : "👁️ Preview";
                }
            }
        }
    }
}