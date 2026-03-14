using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.Messaging;
using System;
using System.Xml;
using VisualEditor.CodeEditor;
using VisualEditor.Core;
using VisualEditor.Core.Messages;
using VisualEditor.Designer;
using VisualEditorApp.ViewModels.Documents;

namespace VisualEditorApp.Views.Documents
{
    public partial class WorkspaceView : UserControl
    {
        private bool _isUpdating = false;

        public static WorkspaceView? Instance { get; private set; }

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

            WeakReferenceMessenger.Default.Register<ControlSelectedMessage>(this, (recipient, message) =>
            {
                if (message.SenderName == "Outline" && message.SelectedControl != null)
                {

                    MyDesignSurface?.SelectControl(message.SelectedControl);
                }
            });
            // 👂 الاشتراك في إشارة الـ Build
            WeakReferenceMessenger.Default.Register<ProjectBuiltMessage>(this, (r, m) =>
            {
                // لما الإشارة تيجي، بنطلب من المصمم يعيد الرسم
                Dispatcher.UIThread.InvokeAsync(() =>
                {
                    LiveDesignerCompiler.Refresh(); // تحميل الـ DLLs الجديدة
                    TriggerRender(); // نده دالة الرسم بتاعتك تانى
                });
            });
        }
        private void TriggerRender()
        {
            // الكود اللي بياخد الـ Text الحالي ويبعته للـ RenderLiveXaml
            // this.ResultControl = LiveDesignerCompiler.RenderLiveXaml(this.Text, this.FilePath);
        }
        private void InitializeControls()
        {



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
                var rootControl = MyDesignSurface.RootDesign;
                if (rootControl != null)
                {
                    string generatedXaml = XamlGenerator.GenerateXaml(rootControl);
                    MyCodeEditor.SetXamlText(generatedXaml);
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
                 SetXamlText(newXamlText, "");
              
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
                WeakReferenceMessenger.Default.Send(new ControlSelectedMessage(selectedControl, "Properties"));
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

                    Avalonia.Controls.Control finalElementToDisplay = newControl as Avalonia.Controls.Control;

                    // 3. كود استخراج الـ Window والنافذة الوهمية
                    if (newControl is Avalonia.Controls.Window window)
                    {
                        var windowContent = window.Content as Avalonia.Controls.Control;
                        window.Content = null; // فصل المحتوى

                        var fakeWindow = new Avalonia.Controls.Border
                        {
                            Background = window.Background ?? Avalonia.Media.Brushes.White,
                            Width = double.IsNaN(window.Width) ? 800 : window.Width,
                            Height = double.IsNaN(window.Height) ? 450 : window.Height,
                            Child = windowContent,
                            BoxShadow = Avalonia.Media.BoxShadows.Parse("0 5 15 0 #40000000")
                        };

                        finalElementToDisplay = fakeWindow; // ده اللي هيتعرض
                    }

                    // 🎯 4. السحر هنا: نطبق الـ DataContext على العنصر النهائي اللي هيظهر للمستخدم
                    if (designContext != null && finalElementToDisplay != null)
                    {
                        finalElementToDisplay.DataContext = designContext;
                    }

                    MyDesignSurface.LoadDesign(finalElementToDisplay);
                        WeakReferenceMessenger.Default.Send(new DesignTreeUpdatedMessage(finalElementToDisplay));

                    









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
    }
}