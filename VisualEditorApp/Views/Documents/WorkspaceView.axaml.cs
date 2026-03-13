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

        private void BtnViewMode_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            var mainGrid = this.FindControl<Grid>("MainGrid");
            var btnDesign = this.FindControl<Button>("BtnDesign");
            var btnSplit = this.FindControl<Button>("BtnSplit");
            var btnXaml = this.FindControl<Button>("BtnXaml");

            // 1. »‰ÃÌ» «·„’„„ Ê«·„Õ—— ⁄‘«‰ ‰ Õﬂ„ ›Ì ŸÂÊ—Â„
            var designSurface = this.FindControl<Avalonia.Controls.Control>("MyDesignSurface");
            var codeEditor = this.FindControl<Avalonia.Controls.Control>("MyCodeEditor");

            if (sender is Button clickedBtn && clickedBtn.Tag is string mode && mainGrid != null)
            {
                //  ’›Ì— √·Ê«‰ «·“—«Ì—
                if (btnDesign != null) { btnDesign.Background = Avalonia.Media.Brushes.Transparent; btnDesign.FontWeight = Avalonia.Media.FontWeight.Normal; }
                if (btnSplit != null) { btnSplit.Background = Avalonia.Media.Brushes.Transparent; btnSplit.FontWeight = Avalonia.Media.FontWeight.Normal; }
                if (btnXaml != null) { btnXaml.Background = Avalonia.Media.Brushes.Transparent; btnXaml.FontWeight = Avalonia.Media.FontWeight.Normal; }

                //  ·ÊÌ‰ «·“—«— «·‰‘ÿ ( ﬁœ—  €Ì— «··Ê‰ ··Ì Ì—ÌÕﬂ)
                clickedBtn.Background = Avalonia.Media.Brush.Parse("#3E3E42");
                clickedBtn.FontWeight = Avalonia.Media.FontWeight.Bold;

                // 2.  Ê“Ì⁄ «·√»⁄«œ Ê«·≈Œ›«¡ Õ”» «·„Êœ:
                if (mode == "Design")
                {
                    if (designSurface != null) designSurface.IsVisible = true;
                    if (codeEditor != null) codeEditor.IsVisible = false; // ‰Œ›Ì «·„Õ——

                    // «·’› «·√Ê· («·„’„„) Ì«Œœ «·‘«‘…° «· «‰Ì («·”»·Ì —) ÌŒ ›Ì° «· «·  («·»«—) Ì«Œœ „”«Õ Â »”
                    mainGrid.RowDefinitions = RowDefinitions.Parse("*, 0, Auto");
                }
                else if (mode == "Split")
                {
                    if (designSurface != null) designSurface.IsVisible = true;
                    if (codeEditor != null) codeEditor.IsVisible = true;

                    // ‰—Ã⁄ «·‘«‘… 3 ’›Ê› “Ì „« ’„„‰«Â«
                    mainGrid.RowDefinitions = RowDefinitions.Parse("*, 5, *");
                }
                else if (mode == "XAML")
                {
                    if (designSurface != null) designSurface.IsVisible = false; // ‰Œ›Ì «·„’„„
                    if (codeEditor != null) codeEditor.IsVisible = true;

                    // «·’› «·√Ê· Ê«· «‰Ì ÌŒ ›Ê«° Ê«·’› «· «·  («·»«— + «·„Õ——) Ì«Œœ «·‘«‘… ﬂ·Â«
                    mainGrid.RowDefinitions = RowDefinitions.Parse("0, 0, *");
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