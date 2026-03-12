using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using VisualEditorApp.Models;

namespace VisualEditorApp.Views
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            //  ‘€Ì· ‰Ÿ«„ «·‹ Dock
            var factory = new EditorDockFactory();
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
                    string xmlText = await System.IO.File.ReadAllTextAsync(filePath);

                    var parsedObject = AvaloniaRuntimeXamlLoader.Parse<object>(xmlText);

                    if (parsedObject is Control rootControl)
                    {
                        Control elementToLoad = rootControl;

                        // «·”Õ— Â‰«: ≈–« ﬂ«‰ «·Ã–— ⁄»«—… ⁄‰ ‰«›–…° ‰” Œ—Ã „Õ Ê«Â«
                        if (rootControl is Window window)
                        {
                            if (window.Content is Control windowContent)
                            {
                                // ‰›’· «·„Õ ÊÏ ⁄‰ «·‰«›–… «·ﬁœÌ„… ·ﬂÌ ‰ „ﬂ‰ „‰ ≈÷«› Â ·„”«Õ… «·⁄„·
                                window.Content = null;
                                elementToLoad = windowContent;
                            }
                        }

                        // ≈—”«· «·„Õ ÊÏ «·„” Œ—Ã (√Ê «·ﬂ‰ —Ê· «·⁄«œÌ) ≈·Ï ”ÿÕ «· ’„Ì„
                        WorkspaceView.Instance?.LoadDesign(elementToLoad);
                    }
                }
                catch (System.Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error loading file: {ex.Message}");
                }
            }
        }

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
    }
}