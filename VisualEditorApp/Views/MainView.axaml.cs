using System;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Platform.Storage;
using Avalonia.VisualTree;
using VisualEditorApp.ViewModels;
using VisualEditorApp.Services;

namespace VisualEditorApp.Views
{
    public partial class MainView : UserControl
    {
        private const double MacMenuLeftMargin = 75;

        public MainView()
        {
            InitializeComponent();
            ApplyPlatformInsets();
            GlobalErrorListener.Register();
        }

        private void OnTitleBarPointerPressed(object? sender, PointerPressedEventArgs e)
        {
            if (!e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
            {
                return;
            }

            if (e.Source is Visual source && source.GetSelfAndVisualAncestors().OfType<MenuItem>().Any())
            {
                return;
            }

            if (TopLevel.GetTopLevel(this) is Window window)
            {
                window.BeginMoveDrag(e);
                e.Handled = true;
            }
        }

        private void ApplyPlatformInsets()
        {
            if (!OperatingSystem.IsMacOS())
            {
                return;
            }

            var menu = this.FindControl<Menu>("MainMenu");
            if (menu is not null)
            {
                menu.Margin = new Thickness(MacMenuLeftMargin, 0, 0, 0);
                menu.Padding = new Thickness(0);
            }
        }

        private async void OnOpenSolutionClicked(object? sender, RoutedEventArgs e)
        {
            var storageProvider = (this.GetVisualRoot() as TopLevel)?.StorageProvider;
            if (storageProvider is null)
            {
                return;
            }

            var result = await storageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                Title = "Open Solution, Project or File",
                FileTypeFilter =
                [
                    new FilePickerFileType("Solution/Project") { Patterns = ["*.sln", "*.slnx", "*.csproj"] },
                    new FilePickerFileType("All Files") { Patterns = ["*.*"] }
                ],
                AllowMultiple = false
            });

            var file = result.FirstOrDefault();
            if (file is null)
            {
                return;
            }

            var path = file.Path.LocalPath;
            if (DataContext is MainWindowViewModel viewModel)
            {
                await viewModel.LoadSolutionAsync(path);
            }
        }
    }
}
