using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using System.Diagnostics;
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
            // «” Œœ«„ StorageProvider ·› Õ ‰«›–… «Œ Ì«— „·›
            var topLevel = TopLevel.GetTopLevel(this);
            if (topLevel == null) return;

            var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                Title = "«Œ — „·› XML √Ê XAML",
                AllowMultiple = false,
                FileTypeFilter = new[]
                {
                new FilePickerFileType("XML / XAML Files") { Patterns = new[] { "*.xml", "*.xaml" } },
                new FilePickerFileType("All Files") { Patterns = new[] { "*.*" } }
            }
            });

            if (files.Count >= 1)
            {
                // «·Õ’Ê· ⁄·Ï „”«— «·„·› «·–Ì «Œ «—Â «·„” Œœ„
                var filePath = files[0].Path.LocalPath;
                Debug.WriteLine($" „ «Œ Ì«— «·„·›: {filePath}");

                // «·ŒÿÊ… «·ﬁ«œ„… ” ﬂÊ‰ ﬁ—«¡… „Õ ÊÏ Â–« «·„·› Ê—”„Â œ«Œ· WorkspaceView
            }
        }

        private void RunProject_Click(object? sender, RoutedEventArgs e)
        {
            // Â‰« ”‰ﬂ » ﬂÊœ  ‘€Ì· (Compile and Run) ··„‘—Ê⁄ «·„› ÊÕ
            Debug.WriteLine(" „ «Œ Ì«—:  ‘€Ì· «·„‘—Ê⁄");
        }
    }
}