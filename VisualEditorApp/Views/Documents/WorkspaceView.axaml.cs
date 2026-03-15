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
using VisualEditor.Core.Models;
using VisualEditorApp.ViewModels;
using VisualEditorApp.ViewModels.Documents;
using VisualEditorApp.ViewModels.Tools;
using System.Linq;
using System.Collections.Generic;

namespace VisualEditorApp.Views.Documents
{
    public partial class WorkspaceView : UserControl
    {
        private bool _isUpdating = false;
        private DispatcherTimer? _debounceTimer;

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

            MessageBus.ProjectBuilt += (m) =>
            {
                Dispatcher.UIThread.InvokeAsync(() =>
                {
                    LiveDesignerCompiler.Refresh();
                    if (DataContext is WorkspaceViewModel vm)
                    {
                        SetXamlText(vm.Text, vm.FilePath);
                    }
                });
            };
        }

        private void HookPropertiesWindow()
        {
            MessageBus.PropertyChanged += (message) =>
            {
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
                
                _debounceTimer = new DispatcherTimer
                {
                    Interval = TimeSpan.FromMilliseconds(500)
                };
                _debounceTimer.Tick += (s, e) =>
                {
                    _debounceTimer.Stop();
                    if (MyCodeEditor != null && !string.IsNullOrEmpty(MyCodeEditor.Text))
                    {
                        Dispatcher.UIThread.InvokeAsync(() => {
                            SetXamlText(MyCodeEditor.Text, (DataContext as WorkspaceViewModel)?.FilePath ?? "");
                        });
                    }
                };
            }

            if (MyDesignSurface != null)
            {
                MyDesignSurface.DesignChanged += MyDesignSurface_DesignChanged;
                MyDesignSurface.SelectionChanged += MyDesignSurface_SelectionChanged;
                MyDesignSurface.ElementAdded += MyDesignSurface_ElementAdded;
                MyDesignSurface.ElementRemoved += MyDesignSurface_ElementRemoved;
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
                if (btnDesign != null) { btnDesign.Background = Brushes.Transparent; btnDesign.FontWeight = FontWeight.Normal; }
                if (btnSplit != null) { btnSplit.Background = Brushes.Transparent; btnSplit.FontWeight = FontWeight.Normal; }
                if (btnXaml != null) { btnXaml.Background = Brushes.Transparent; btnXaml.FontWeight = FontWeight.Normal; }

                clickedBtn.Background = Brush.Parse("#3E3E42");
                clickedBtn.FontWeight = FontWeight.Bold;

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
                    var targetControl = designer.SelectedControl;
                    if (targetControl != null && !string.IsNullOrEmpty(targetControl.Name))
                    {
                        string xml = MyCodeEditor.Text;
                        
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

                        if (targetControl.RenderTransform is RotateTransform rt)
                        {
                            xml = XamlDOMPatcher.PatchProperty(xml, targetControl, "RenderTransform", $"rotate({rt.Angle})");
                        }
                        
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
            
            _debounceTimer?.Stop();
            _debounceTimer?.Start();
        }

        private void MyDesignSurface_SelectionChanged(object? sender, Control? selectedControl)
        {
            if (selectedControl != null)
            {
                MessageBus.Send(new ControlSelectedMessage(selectedControl, "Properties"));
            }
        }

        private void MyDesignSurface_ElementAdded(object? sender, (Control Element, Control Parent) args)
        {
            if (_isUpdating || MyCodeEditor == null) return;
            _isUpdating = true;
            try
            {
                string xml = MyCodeEditor.Text;
                string parentName = args.Parent?.Name ?? "";
                
                // 🎯 إذا كان الأب هو الـ Root أو أجزاء الـ Window الوهمية، نرسل أمر الإضافة للعنصر الجذري في الـ XAML مباشرة
                if (args.Parent == MyDesignSurface.RootDesign || 
                    parentName == "SimulatedContentArea" || 
                    parentName == "SimulatedWindowFrame" || 
                    parentName == "SimulatedTitleBar")
                {
                    parentName = ""; 
                }

                string typeName = args.Element.GetType().Name;
                string newName = args.Element.Name ?? "";

                string extraProps = $"Width=\"{args.Element.Width}\" Height=\"{args.Element.Height}\"";
                if (args.Parent is Canvas)
                {
                    extraProps += $" Canvas.Left=\"{Canvas.GetLeft(args.Element)}\" Canvas.Top=\"{Canvas.GetTop(args.Element)}\"";
                }

                string newXml = XamlDOMPatcher.AddElement(xml, parentName, typeName, newName, extraProps);
                if (newXml != xml)
                {
                    MyCodeEditor.SetXamlText(newXml);
                }
            }
            finally { _isUpdating = false; }
        }

        private void MyDesignSurface_ElementRemoved(object? sender, Control element)
        {
            if (_isUpdating || MyCodeEditor == null || string.IsNullOrEmpty(element.Name)) return;
            _isUpdating = true;
            try
            {
                string xml = MyCodeEditor.Text;
                string newXml = XamlDOMPatcher.RemoveElement(xml, element.Name);
                if (newXml != xml)
                {
                    MyCodeEditor.SetXamlText(newXml);
                }
            }
            finally { _isUpdating = false; }
        }

        public void SetXamlText(string xml, string filePath)
        {
            if (MyCodeEditor == null || MyDesignSurface == null) return;

            MyCodeEditor.SetXamlText(xml);

            try
            {
                var newControl = LiveDesignerCompiler.RenderLiveXaml(xml, filePath);

                if (newControl != null)
                {
                    object designContext = null;
                    if (newControl is Control originalControl)
                    {
                        designContext = Design.GetDataContext(originalControl);
                    }

                    Control? finalElementToDisplay = null;

                    if (newControl is Window window)
                    {
                        var windowContent = window.Content as Control;
                        window.Content = null;

                        // بناء نافذة وهمية (Simulated Window)
                        var windowFrame = new Grid
                        {
                            Name = window.Name, // 🎯 الحفاظ على الاسم عشان التحديد يشتغل صح
                            Width = double.IsNaN(window.Width) ? 800 : window.Width,
                            Height = double.IsNaN(window.Height) ? 450 : window.Height,
                            ClipToBounds = false,
                            RowDefinitions = RowDefinitions.Parse("30, *")
                        };

                        // شريط العنوان
                        var titleBar = new Border
                        {
                            Name = "SimulatedTitleBar",
                            Background = Brush.Parse("#3C3C3C"),
                            CornerRadius = new CornerRadius(5, 5, 0, 0),
                            Child = new TextBlock 
                            { 
                                Text = window.Title ?? "Window Preview", 
                                VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,
                                Margin = new Thickness(10, 0),
                                Foreground = Brushes.White,
                                FontSize = 12
                            }
                        };
                        Grid.SetRow(titleBar, 0);

                        // المحتوى
                        var contentArea = new Border
                        {
                            Name = "SimulatedContentArea",
                            Background = window.Background ?? Brushes.White,
                            CornerRadius = new CornerRadius(0, 0, 5, 5),
                            Child = windowContent,
                            BoxShadow = BoxShadows.Parse("0 5 15 0 #40000000")
                        };
                        Grid.SetRow(contentArea, 1);

                        windowFrame.Children.Add(titleBar);
                        windowFrame.Children.Add(contentArea);

                        finalElementToDisplay = windowFrame;
                    }
                    else if (newControl is Control ctrl)
                    {
                        // 🎯 هنا قمنا بإزالة الـ Wrapper Border لتجنب تعقيد هيكلة الـ XAML
                        // وضع كنترول المستخدم النهائي مباشرة هو الأفضل للمزامنة مع الكود
                        finalElementToDisplay = ctrl;
                    }

                    if (designContext != null && finalElementToDisplay != null)
                    {
                        finalElementToDisplay.DataContext = designContext;
                        if (finalElementToDisplay is Border b && b.Child != null)
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
                MessageBus.Send(SystemDiagnosticMessage.Create(DiagnosticSeverity.Error, "VIEW001", $"Error during initial rendering: {ex.Message}", "WorkspaceView.axaml"));
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

        private void OnPreviewButton_Click(object? sender, RoutedEventArgs e)
        {
            if (sender is ToggleButton toggleBtn)
            {
                bool isPreviewOn = toggleBtn.IsChecked == true;

                if (MyDesignSurface != null)
                {
                    MyDesignSurface.SetPreviewMode(isPreviewOn);
                    toggleBtn.Background = isPreviewOn ? Brushes.LightGreen : Brushes.Transparent;
                    toggleBtn.Content = isPreviewOn ? "✏️ Edit Mode" : "👁️ Preview";
                }
            }
        }
    }
}