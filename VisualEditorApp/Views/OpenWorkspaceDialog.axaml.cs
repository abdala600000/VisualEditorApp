using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using VisualEditorApp.ViewModels;

namespace VisualEditorApp;

public partial class OpenWorkspaceDialog : Window
{
    public OpenWorkspaceDialog()
    {
        InitializeComponent();
    }

    // „ €Ì— Â‰Õ›Ÿ ›ÌÂ «Œ Ì«— «·„” Œœ„
    public string Result { get; private set; } = "Cancel";

    // Œ·Ì‰« «·ﬂ·«” Ìﬁ»· «·‹ ViewModel ›Ì «·‹ Constructor
    public OpenWorkspaceDialog(OpenWorkspaceViewModel vm)
    {
        InitializeComponent();
        DataContext = vm;

        vm.RequestClose += (s, result) =>
        {
            this.Result = result; // Õ›Ÿ «·‰ ÌÃ… (OpenCurrent, OpenNew, Cancel)
            this.Close(result);   // ≈€·«ﬁ «·‰«›–…
        };
    }
}