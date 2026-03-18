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
                var targetControl = MyDesignSurface.SelectedControl;
                if (targetControl == null || string.IsNullOrEmpty(targetControl.Name)) return;

                string xml = MyCodeEditor.Text;

                if (targetControl.Parent is Canvas)
                {
                    double left = Canvas.GetLeft(targetControl);
                    double top  = Canvas.GetTop(targetControl);
                    if (!double.IsNaN(left) && left >= 0)
                        xml = XamlDOMPatcher.PatchProperty(xml, targetControl, "Canvas.Left", ((int)Math.Round(left)).ToString());
                    if (!double.IsNaN(top) && top >= 0)
                        xml = XamlDOMPatcher.PatchProperty(xml, targetControl, "Canvas.Top", ((int)Math.Round(top)).ToString());
                    xml = XamlDOMPatcher.RemoveAttribute(xml, targetControl.Name, "Margin");
                }
                else
                {
                    var m = targetControl.Margin;
                    if (!double.IsNaN(m.Left) && !double.IsNaN(m.Top) && m.Left >= 0 && m.Top >= 0)
                        xml = XamlDOMPatcher.PatchProperty(xml, targetControl, "Margin",
                            $"{(int)Math.Round(m.Left)},{(int)Math.Round(m.Top)},0,0");
                }

                double w = double.IsNaN(targetControl.Width)  ? targetControl.Bounds.Width  : targetControl.Width;
                double h = double.IsNaN(targetControl.Height) ? targetControl.Bounds.Height : targetControl.Height;
                if (w > 0) xml = XamlDOMPatcher.PatchProperty(xml, targetControl, "Width",  ((int)Math.Round(w)).ToString());
                if (h > 0) xml = XamlDOMPatcher.PatchProperty(xml, targetControl, "Height", ((int)Math.Round(h)).ToString());

                if (targetControl.RenderTransform is RotateTransform rt)
                    xml = XamlDOMPatcher.PatchProperty(xml, targetControl, "RenderTransform", $"rotate({rt.Angle})");

                if (xml != MyCodeEditor.Text)
                    MyCodeEditor.SetXamlText(xml);
            }
            finally { _isUpdating = false; }
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

                // الـ parent الحقيقي في الـ XAML
                var parent = args.Parent;
                string parentName = "";
                bool parentIsCanvas = false;

                bool isRoot = parent == MyDesignSurface.RootDesign;
                if (!isRoot && !string.IsNullOrEmpty(parent?.Name))
                {
                    parentName = parent.Name;
                    parentIsCanvas = parent is Canvas;
                }

                string typeName = args.Element.GetType().Name;
                string newName  = args.Element.Name ?? "";

                double w = double.IsNaN(args.Element.Width)  ? 100 : args.Element.Width;
                double h = double.IsNaN(args.Element.Height) ? 30  : args.Element.Height;

                string extraProps = $"Width=\"{(int)Math.Round(w)}\" Height=\"{(int)Math.Round(h)}\"";

                if (parentIsCanvas)
                {
                    double left = Canvas.GetLeft(args.Element);
                    double top  = Canvas.GetTop(args.Element);
                    if (double.IsNaN(left) || left < 0) left = 10;
                    if (double.IsNaN(top)  || top  < 0) top  = 10;
                    extraProps += $" Canvas.Left=\"{(int)Math.Round(left)}\" Canvas.Top=\"{(int)Math.Round(top)}\"";
                }

                string newXml = XamlDOMPatcher.AddElement(xml, parentName, typeName, newName, extraProps);
                if (newXml != xml)
                {
                    MyCodeEditor.SetXamlText(newXml);
                    if (MyDesignSurface?.RootDesign != null)
                        MessageBus.Send(new DesignTreeUpdatedMessage(MyDesignSurface.RootDesign));
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

                    // 🎯 تحديث الـ Outline يدوياً
                    if (MyDesignSurface?.RootDesign != null)
                    {
                        MessageBus.Send(new DesignTreeUpdatedMessage(MyDesignSurface.RootDesign));
                    }
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
                if (newControl == null) return;

                object? designContext = newControl is Control oc ? Design.GetDataContext(oc) : null;

                Control? finalElementToDisplay = null;

                if (newControl is Window window)
                {
                    // نعرض محتوى الـ Window مباشرة بدون أي wrapper
                    var windowContent = window.Content as Control;
                    window.Content = null;
                    finalElementToDisplay = windowContent;
                }
                else if (newControl is Control ctrl)
                {
                    finalElementToDisplay = ctrl;
                }

                if (finalElementToDisplay == null) return;

                if (designContext != null)
                    finalElementToDisplay.DataContext = designContext;

                MyDesignSurface.LoadDesign(finalElementToDisplay);
                MessageBus.Send(new DesignTreeUpdatedMessage(finalElementToDisplay));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"SetXamlText error: {ex.Message}");
                MessageBus.Send(SystemDiagnosticMessage.Create(
                    DiagnosticSeverity.Error, "VIEW001",
                    $"Render error: {ex.Message}", "WorkspaceView.axaml"));
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