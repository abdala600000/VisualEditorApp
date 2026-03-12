using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Platform.Storage;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
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
                AllowMultiple = false,
                FileTypeFilter = new[]
                {
                new FilePickerFileType("Avalonia XAML") { Patterns = new[] { "*.xml", "*.xaml", "*.axaml" } }
            }
            });

            if (files.Count >= 1)
            {
                var filePath = files[0].Path.LocalPath;
                Debug.WriteLine($"Selected file: {filePath}");

                try
                {
                    // 1. ﬁ—«¡… „Õ ÊÏ «·„·› ﬂ‰’
                    string xamlText = await File.ReadAllTextAsync(filePath);

                    // 2.  Õ·Ì· «·‹ XAML Ê ÕÊÌ·Â ≈·Ï ﬂ«∆‰«  „—∆Ì…
                    var parsedObject = AvaloniaRuntimeXamlLoader.Parse<object>(xamlText);

                    if (parsedObject is Control rootControl)
                    {
                        // 3.  ‰ŸÌ› „”«Õ… «·⁄„· «·Õ«·Ì…
                        WorkspaceView.Instance?.ClearWorkspace();

                        // 4. ≈–« ﬂ«‰ «·„·› ÌÕ ÊÌ ⁄·Ï Õ«ÊÌ… (Panel) „À· Canvas √Ê Grid
                        if (rootControl is Panel panel)
                        {
                            // ‰‰”Œ «·⁄‰«’— ›Ì ﬁ«∆„… „‰›’·… À„ ‰›’·Â« ⁄‰ «·Õ«ÊÌ… «·√’·Ì…
                            var children = panel.Children.ToList();
                            panel.Children.Clear();

                            foreach (var child in children)
                            {
                                if (child is Control uiControl)
                                {
                                    // «” Œ—«Ã Œ’«∆’ «·⁄‰’—
                                    double left = Canvas.GetLeft(uiControl);
                                    double top = Canvas.GetTop(uiControl);
                                    double width = uiControl.Width;
                                    double height = uiControl.Height;

                                    // ≈—”«· «·⁄‰’— ·„”«Õ… «·⁄„· ·Ì „  €·Ì›Â
                                    WorkspaceView.Instance?.AddWrappedElement(uiControl, left, top, width, height);
                                }
                            }
                        }
                        else
                        {
                            // ≈–« ﬂ«‰ «·„·› ÌÕ ÊÌ ⁄·Ï ⁄‰’— Ê«Õœ ›ﬁÿ („À·« <Button> „»«‘—)
                            WorkspaceView.Instance?.AddWrappedElement(rootControl, 50, 50, rootControl.Width, rootControl.Height);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error parsing XAML: {ex.Message}");
                    // Ì„ﬂ‰ﬂ ·«Õﬁ« ⁄—÷ MessageBox Â‰« ·≈Œ»«— «·„” Œœ„ »ÊÃÊœ Œÿ√ ›Ì «·„·›
                }
            }
        }

        private void RunProject_Click(object? sender, RoutedEventArgs e)
        {
            // Â‰« ”‰ﬂ » ﬂÊœ  ‘€Ì· (Compile and Run) ··„‘—Ê⁄ «·„› ÊÕ
            Debug.WriteLine(" „ «Œ Ì«—:  ‘€Ì· «·„‘—Ê⁄");
        }
    }
}