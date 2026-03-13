using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.Messaging;
using Dock.Model.Controls;
using Dock.Model.Mvvm.Controls;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using VisualEditorApp.Core;
using VisualEditorApp.Models;
using VisualEditorApp.ViewModels;
using static VisualEditorApp.ViewModels.SolutionExplorerTool;

namespace VisualEditorApp.Views
{
    public partial class MainWindow : Window, IRecipient<OpenFileMessage>
    {
        public MainWindow()
        {
            InitializeComponent();
            // 1. ≈‰‘«¡ «·‹ ViewModel «·—∆Ì”Ì («·„Œ“‰)
            var vm = new MainWindowViewModel();
            DataContext = vm; // —»ÿ «·‹ XAML »«·‹ ViewModel

            // 2.  „—Ì— «·‹ VM ··„’‰⁄ ·÷„«‰ "ÊÕœ… «·‰”Œ"
            var factory = new EditorDockFactory(vm);
            var layout = factory.CreateLayout();
            factory.InitLayout(layout);

            MainDockControl.Layout = layout;
            // «·Õ· «·”Õ—Ì: ≈·€«¡ √Ì  ”ÃÌ· ”«»ﬁ ·Â–« «·ﬂ«∆‰ ﬁ»· «· ”ÃÌ· «·ÃœÌœ
            WeakReferenceMessenger.Default.UnregisterAll(this);
            WeakReferenceMessenger.Default.Register<OpenFileMessage>(this);
        }
        // --- œÊ«· ‘—Ìÿ «·ﬁÊ«∆„ ---

        private void NewProject_Click(object? sender, RoutedEventArgs e)
        {
            // Â‰« ”‰ﬂ » ·«Õﬁ« ﬂÊœ  ›—Ì€ „”«Õ… «·⁄„· √Ê  ÂÌ∆… „‘—Ê⁄ ÃœÌœ
            Debug.WriteLine(" „ «Œ Ì«—: ≈‰‘«¡ „‘—Ê⁄ ÃœÌœ");
        }

        private async void OpenProject_Click(object? sender, RoutedEventArgs e)
        {
            // «·Ê’Ê· ··‹ Window «·Õ«·Ì… »√ÕœÀ ÿ—Ìﬁ… ›Ì Avalonia 11
            if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                var topLevel = Avalonia.Controls.TopLevel.GetTopLevel(desktop.MainWindow);
                if (topLevel != null)
                {
                    // › Õ «·‹ Picker «·ÕœÌÀ
                    var folders = await topLevel.StorageProvider.OpenFolderPickerAsync(new Avalonia.Platform.Storage.FolderPickerOpenOptions
                    {
                        Title = "Open Project Folder",
                        AllowMultiple = false
                    });

                    if (folders.Count > 0)
                    {
                        // ≈—”«· —”«·… ·ﬂ· «·»—‰«„Ã ≈‰ ›ÌÂ ›Ê·œ— « › Õ
                        var path = folders[0].Path.LocalPath;
                        WeakReferenceMessenger.Default.Send(new FolderOpenedMessage(path));
                    }
                }
            }
        }

        private async void OpenFile_Click(object? sender, RoutedEventArgs e)
        {
            var topLevel = TopLevel.GetTopLevel(this);
            if (topLevel == null) return;

            var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                Title = "Select XML / XAML File",
                AllowMultiple = false
            });

            if (files.Count >= 1)
            {
                var filePath = files[0].Path.LocalPath;

                try
                {
                    // 1. ﬁ—«¡… «·‰’ «·Œ«„
                    string xmlText = await System.IO.File.ReadAllTextAsync(filePath);

                    // 2. «·”Õ— «·„⁄„«—Ì:  ‰ŸÌ› «·‹ XAML „‰ √Ì ﬂÊœ Ì”»» Crash ›Ì Ê÷⁄ «· ‘€Ì·
                    string cleanXmlText = SanitizeXaml(xmlText);

                    // 3.  „—Ì— «·‰’ «·‰ŸÌ› ··„Õ—ﬂ
                    var parsedObject = LiveDesignerCompiler.RenderLiveXaml(xmlText);

                    if (parsedObject is Control rootControl)
                    {
                        Control elementToLoad = rootControl;

                        if (rootControl is Window window && window.Content is Control windowContent)
                        {
                            window.Content = null;
                            elementToLoad = windowContent;
                        }

                        WorkspaceView.Instance?.LoadDesign(elementToLoad);
                        // ≈—”«· «·‰’ ··Ê—ﬂ ”»Ì” ·ÌŸÂ— ›Ì «·„Õ—— Ê«· ’„Ì„ „⁄«
                        WorkspaceView.Instance?.SetXamlContent(xmlText);
                    }
                }
                catch (System.Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error loading file: {ex.Message}");
                }
            }
        }
        private string SanitizeXaml(string originalXaml)
        {
            string clean = originalXaml;

            // 1. „”Õ «·‹ x:Class
            clean = Regex.Replace(clean, @"\s+x:Class=""[^""]*""", "");

            // 2.  ÕÊÌ· CompiledBinding
            clean = Regex.Replace(clean, @"\{CompiledBinding\b", "{Binding");

            // 3. „”Õ «·√Õœ«À (Events)
            clean = Regex.Replace(clean, @"\s+[A-Za-z]*(?:Click|Pressed|Released|Enter|Leave|Move|Wheel|Down|Up|Changed|Loaded|Unloaded|Opened|Closed|Tapped|TextInput|Focus|Checked|Unchecked)=""[^""]*""", "");

            // ======== «·≈÷«›… «·ÃœÌœ…: Õ„«Ì… «··ÊÕ… „‰ «·’Ê— «·„›ﬁÊœ… ========
            // «·›· — œÂ »Ì„”Õ Œ«’Ì… Source="" ·Ê ﬂ«‰  „”«— „Õ·Ì √Ê avares:// 
            // Ê»Ì”Ì»Â« ·Ê ﬂ«‰  —«»ÿ „‰ «·‰  (http √Ê https) ⁄‘«‰ ·Ê Õ»Ì   ⁄—÷ ’Ê—… „‰ «·‰  ›Ì «· ’„Ì„
            clean = Regex.Replace(clean, @"\s+Source=""(?!(http|https)://)[^""]*""", "");

            // («Œ Ì«—Ì) Õ„«Ì… ≈÷«›Ì… ·Œ’«∆’ «·’Ê— «· «‰Ì… “Ì «·›—«‘Ì (ImageBrush)
            clean = Regex.Replace(clean, @"<ImageBrush\s+ImageSource=""(?!(http|https)://)[^""]*""", "<ImageBrush ");

            // ======== «·≈÷«›… «·ÃœÌœ…: œ—⁄ Õ„«Ì… «·ŒÿÊÿ «·„Œ’’… ========
            // «·›· — œÂ »Ì„”Õ Œ«’Ì… FontFamily »«·ﬂ«„· ·Ê ﬂ«‰ ÃÊ«Â« „”«— avares √Ê resm √Ê ⁄·«„… # » «⁄… «·ŒÿÊÿ
            clean = Regex.Replace(clean, @"\s+FontFamily=""[^""]*(avares://|resm://|#|\.ttf|\.otf)[^""]*""", "");

            return clean;
        }
        // --- „’›«…  ‰ŸÌ› «·‹ XAML (XAML Sanitizer) ---
        //private string SanitizeXaml(string originalXaml)
        //{
        //    string clean = originalXaml;

        //    // 1.  ÕÊÌ· CompiledBinding ≈·Ï Binding ⁄«œÌ ·ﬂÌ Ì⁄„· Êﬁ  «· ’„Ì„
        //    clean = Regex.Replace(clean, @"\{CompiledBinding\b", "{Binding");

        //    // 2. ≈“«·… x:Class (·√‰Â«   ÿ·» ﬂÊœ Œ·›Ì €Ì— „ÊÃÊœ √À‰«¡ «· ’„Ì„)
        //    clean = Regex.Replace(clean, @"x:Class=""[^""]*""", "");

        //    // 3. ≈“«·… «·√Õœ«À (Events) «· Ì  »ÕÀ ⁄‰ œÊ«· ›Ì «·ﬂÊœ «·Œ·›Ì
        //    clean = Regex.Replace(clean, @"\s+(Click|PointerPressed|PointerReleased|KeyDown|KeyUp|Loaded|PointerMoved)=""[^""]*""", "");

        //    // „·«ÕŸ… Â«„…: ·ﬁœ ﬁ„‰« »≈“«·… „”Õ x:Name „‰ Â‰«° 
        //    // ·√‰ﬂ  ” Œœ„ ElementName bindings Ê«· Ì  ⁄ „œ ⁄·Ï ÊÃÊœ «·√”„«¡.
        //    // »œ·« „‰ –·ﬂ° ”‰„”Õ x:Name „‰ «·⁄‰«’— €Ì— «·„—∆Ì… ›ﬁÿ („À· Transforms) 
        //    // √Ê ‰ —ﬂ Avalonia   ⁄«„· „⁄ «·√”„«¡ «·’ÕÌÕ… ··ﬂ‰ —Ê·« .
        //    clean = Regex.Replace(clean, @"<([^>]+)\s+x:Name=""[^""]*""([^>]*)>\s*</\1>", "<$1$2></$1>"); //  ‰ŸÌ› √Ê·Ì ··‹ Transforms

        //    return clean;
        //}

        // --- œ«·… „”«⁄œ… ·«” Œ—«Ã «·√—ﬁ«„ „‰ Œ’«∆’ «·‹ XML »√„«‰ ---
        private double GetDoubleAttribute(XElement element, string attributeName, double defaultValue)
        {
            var attr = element.Attribute(attributeName);
            if (attr != null && double.TryParse(attr.Value, out double result))
            {
                return result;
            }
            return defaultValue;
        }

        private void RunProject_Click(object? sender, RoutedEventArgs e)
        {
            // Â‰« ”‰ﬂ » ﬂÊœ  ‘€Ì· (Compile and Run) ··„‘—Ê⁄ «·„› ÊÕ
            Debug.WriteLine(" „ «Œ Ì«—:  ‘€Ì· «·„‘—Ê⁄");
        }

        private void PreviewToggle_Checked(object? sender, RoutedEventArgs e)
        {
            if (sender is ToggleButton tb)
            {
                // ≈—”«· «·Õ«·… (True ··„⁄«Ì‰…° False ·· ’„Ì„)
                WorkspaceView.Instance?.SetPreviewMode(tb.IsChecked ?? false);

                //  €ÌÌ— ·Ê‰ «·“—«— ·· ‰»ÌÂ
                tb.Content = (tb.IsChecked ?? false) ? "RUNNING (Live)" : "Preview Mode";
            }
        }





        public async void Receive(OpenFileMessage message)
        {
          


            string xmlText = await System.IO.File.ReadAllTextAsync(message.FilePath);

            // 2. «·”Õ— «·„⁄„«—Ì:  ‰ŸÌ› «·‹ XAML „‰ √Ì ﬂÊœ Ì”»» Crash ›Ì Ê÷⁄ «· ‘€Ì·
            string cleanXmlText = SanitizeXaml(xmlText);

            // 3.  „—Ì— «·‰’ «·‰ŸÌ› ··„Õ—ﬂ
            var parsedObject = AvaloniaRuntimeXamlLoader.Parse<object>(cleanXmlText);

            if (parsedObject is Control rootControl)
            {
                Control elementToLoad = rootControl;

                if (rootControl is Window window && window.Content is Control windowContent)
                {
                    window.Content = null;
                    elementToLoad = windowContent;
                }

                WorkspaceView.Instance?.LoadDesign(elementToLoad);
                // ≈—”«· «·‰’ ··Ê—ﬂ ”»Ì” ·ÌŸÂ— ›Ì «·„Õ—— Ê«· ’„Ì„ „⁄«
                WorkspaceView.Instance?.SetXamlContent(xmlText);
            }
        }
    }
}