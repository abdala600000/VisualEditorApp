using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using VisualEditorApp.Models;
using VisualEditorApp.ViewModels;

namespace VisualEditorApp.Views
{
    public partial class MainWindow : Window
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
        }
        // --- œÊ«· ‘—Ìÿ «·ﬁÊ«∆„ ---

        private void NewProject_Click(object? sender, RoutedEventArgs e)
        {
            // Â‰« ”‰ﬂ » ·«Õﬁ« ﬂÊœ  ›—Ì€ „”«Õ… «·⁄„· √Ê  ÂÌ∆… „‘—Ê⁄ ÃœÌœ
            Debug.WriteLine(" „ «Œ Ì«—: ≈‰‘«¡ „‘—Ê⁄ ÃœÌœ");
        }

        private void OpenProject_Click(object? sender, RoutedEventArgs e)
        {
            // Â‰« ”‰ﬂ » ﬂÊœ › Õ „Ã·œ «·„‘—Ê⁄
            Debug.WriteLine(" „ «Œ Ì«—: › Õ „‘—Ê⁄");
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
                catch (System.Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error loading file: {ex.Message}");
                }
            }
        }
        private string SanitizeXaml(string originalXaml)
        {
            string clean = originalXaml;

            // 1. ≈“«·… x:Class (·√‰Â«   ÿ·» ﬂÊœ Œ·›Ì €Ì— „ÊÃÊœ √À‰«¡ «· ’„Ì„)
            clean = Regex.Replace(clean, @"x:Class=""[^""]*""", "");

            // 2. ≈“«·… x:Name ( ”»» „‘«ﬂ· „⁄ «·⁄‰«’— €Ì— «·„—∆Ì… „À· Transforms)
            clean = Regex.Replace(clean, @"x:Name=""[^""]*""", "");

            // 3. ≈“«·… √Õœ«À «·„«Ê” Ê«·ﬂÌ»Ê—œ «·‘«∆⁄… (·√‰Â« ” »ÕÀ ⁄‰ œÊ«· ›Ì «·ﬂÊœ «·Œ·›Ì Ê ‰Â«—)
            // „À· Click="Btn_Click" √Ê KeyDown="Window_KeyDown"
            clean = Regex.Replace(clean, @"\s+(Click|PointerPressed|PointerReleased|KeyDown|KeyUp|Loaded|PointerMoved)=""[^""]*""", "");

            // 4. ≈“«·… √Ì „”«›«  “«∆œ…  —ﬂÂ« «· ‰ŸÌ›
            clean = Regex.Replace(clean, @"\s+>", ">");

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
    }
}