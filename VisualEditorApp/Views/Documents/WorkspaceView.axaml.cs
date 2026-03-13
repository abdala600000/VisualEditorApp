using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Markup.Xaml;
using CommunityToolkit.Mvvm.Messaging;
using System;
using VisualEditor.Core;
using VisualEditor.Core.Messages;
using VisualEditor.Designer;
using VisualEditor.CodeEditor;
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
                    SetXamlText(vm.Text);
                }
            };

            WeakReferenceMessenger.Default.Register<ControlSelectedMessage>(this, (recipient, message) =>
            {
                if (message.SenderName == "Outline" && message.SelectedControl != null)
                {

                    MyDesignSurface?.SelectControl(message.SelectedControl);
                }
            });
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

        private void BtnViewMode_Click(object? sender, RoutedEventArgs e)
        {
            var mainGrid = this.FindControl<Grid>("MainGrid");
            var btnDesign = this.FindControl<Button>("BtnDesign");
            var btnSplit = this.FindControl<Button>("BtnSplit");
            var btnXaml = this.FindControl<Button>("BtnXaml");

            if (sender is Button clickedBtn && clickedBtn.Tag is string mode && mainGrid != null)
            {
                if (btnDesign != null) { btnDesign.Background = Brushes.Transparent; btnDesign.FontWeight = FontWeight.Normal; }
                if (btnSplit != null) { btnSplit.Background = Brushes.Transparent; btnSplit.FontWeight = FontWeight.Normal; }
                if (btnXaml != null) { btnXaml.Background = Brushes.Transparent; btnXaml.FontWeight = FontWeight.Normal; }

                clickedBtn.Background = Brush.Parse("#DDDDDD");
                clickedBtn.FontWeight = FontWeight.Bold;

                if (mode == "Design")
                {
                    mainGrid.RowDefinitions = RowDefinitions.Parse("*, Auto, 0, 0");
                }
                else if (mode == "Split")
                {
                    mainGrid.RowDefinitions = RowDefinitions.Parse("*, Auto, 5, *");
                }
                else if (mode == "XAML")
                {
                    mainGrid.RowDefinitions = RowDefinitions.Parse("0, Auto, 0, *");
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
                var newControl = LiveDesignerCompiler.RenderLiveXaml(newXamlText);
                if (newControl != null)
                {
                    MyDesignSurface.LoadDesign(newControl);
                    WeakReferenceMessenger.Default.Send(new DesignTreeUpdatedMessage(newControl));
                }
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
                 // Logic for selection if needed
             }
        }

        public void SetXamlText(string xml)
        {
           
            

            if (MyCodeEditor == null || MyDesignSurface == null) return;

            MyCodeEditor.SetXamlText(xml);

            try
            {
                var newControl = LiveDesignerCompiler.RenderLiveXaml(xml);
                if (newControl != null)
                {
                    MyDesignSurface.LoadDesign(newControl);
                    WeakReferenceMessenger.Default.Send(new DesignTreeUpdatedMessage(newControl));
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