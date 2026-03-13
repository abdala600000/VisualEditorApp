using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Dock.Model.Controls;
using Dock.Model.Core;
using VisualEditorApp.ViewModels.Tools;
using VisualEditorApp.ViewModels.Documents;
using VisualEditorApp.Services;

namespace VisualEditorApp.ViewModels
{
    public partial class MainWindowViewModel : ObservableObject
    {
        private readonly DockFactory _factory;
        private readonly SolutionLoader _solutionLoader;
        [ObservableProperty] private IRootDock? _layout;
        [ObservableProperty] private string _statusText = "Ready";
        [ObservableProperty] private string _solutionName = "No solution";
        [ObservableProperty] private string _activeDocumentStatus = "No document";
        [ObservableProperty] private EditorDocumentViewModel? _activeDocument;
        [ObservableProperty] private bool _isDarkTheme = true;

        public MainWindowViewModel()
        {
            _factory = new DockFactory(OpenDocument);
            _solutionLoader = new SolutionLoader();

            var layout = _factory.CreateLayout();
            if (layout is not null)
            {
                _factory.InitLayout(layout);
            }

            Layout = layout;
        }

        public bool IsLightTheme => !IsDarkTheme;

        [RelayCommand]
        private void SaveFile()
        {
            ActiveDocument?.Save();
            StatusText = $"Saved {ActiveDocument?.Title}";
        }

        [RelayCommand]
        private void SaveAll()
        {
            if (_factory.DocumentDock?.VisibleDockables is not null)
            {
                foreach (var document in _factory.DocumentDock.VisibleDockables.OfType<EditorDocumentViewModel>())
                {
                    if (document.IsModified)
                    {
                        document.Save();
                    }
                }
            }
            StatusText = "Saved all documents";
        }

        [RelayCommand]
        private void CloseSolution()
        {
            _factory.SolutionExplorer.Clear();
            _factory.PropertiesTool.UpdateSelection(null);
            _factory.ProblemsTool.Clear();
            _factory.CloseAllDocuments();
            _solutionLoader.Clear();
            ActiveDocument = null;
            SolutionName = "No solution";
            StatusText = "Ready";
        }

        [RelayCommand]
        private void Exit()
        {
            if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktopLifetime)
            {
                desktopLifetime.Shutdown();
            }
        }

        [RelayCommand]
        private void SwitchToLightTheme() => SetTheme(Avalonia.Styling.ThemeVariant.Light);

        [RelayCommand]
        private void SwitchToDarkTheme() => SetTheme(Avalonia.Styling.ThemeVariant.Dark);

        private void SetTheme(Avalonia.Styling.ThemeVariant themeVariant)
        {
            if (Application.Current is not null)
            {
                Application.Current.RequestedThemeVariant = themeVariant;
                IsDarkTheme = themeVariant == Avalonia.Styling.ThemeVariant.Dark;
            }
        }

        public async Task LoadSolutionAsync(string solutionPath)
        {
            StatusText = "Loading solution...";
            SolutionName = Path.GetFileName(solutionPath);

            try
            {
                var result = await _solutionLoader.LoadAsync(solutionPath, CancellationToken.None);
                if (result.Solution is null)
                {
                    StatusText = "Failed to load solution.";
                    return;
                }

                _factory.SolutionExplorer.LoadSolution(result.Solution);
                _factory.ProblemsTool.UpdateDiagnostics(result.Diagnostics.Select(d => 
                    new Models.ProblemItemViewModel(d.Kind.ToString(), d.Message)));

                var projectCount = result.Solution.Projects.Count();
                StatusText = $"Loaded {projectCount} projects";
            }
            catch (Exception ex)
            {
                StatusText = $"Solution load failed: {ex.Message}";
            }
        }

        public void OpenDocument(string path)
        {
            _factory.OpenDocument(path);
        }

        public void CloseLayout()
        {
            if (Layout is IDock dock && dock.Close.CanExecute(null))
            {
                dock.Close.Execute(null);
            }
        }
    }
}
