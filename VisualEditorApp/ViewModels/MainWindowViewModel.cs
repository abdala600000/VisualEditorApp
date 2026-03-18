using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Dock.Model.Controls;
using Dock.Model.Core;
using Dock.Model.Mvvm.Controls;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using VisualEditor.Core.Messages;
using VisualEditor.Core.Models;
using VisualEditor.Toolbox.Outline;
using VisualEditorApp.Models;
using VisualEditorApp.Services;
using VisualEditorApp.ViewModels.Documents;
using VisualEditorApp.ViewModels.Tools;

namespace VisualEditorApp.ViewModels
{
    public partial class MainWindowViewModel : ObservableObject
    {
        private readonly DockFactory _factory;
        private readonly SolutionLoader _solutionLoader;
        private Process? _runningProcess;

        [ObservableProperty] private IRootDock? _layout;
        [ObservableProperty] private string _statusText = "Ready";
        [ObservableProperty] private string _solutionName = "No solution";
        [ObservableProperty] private string _activeDocumentStatus = "No document";
        [ObservableProperty] private Document? _activeDocument;
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

           new DocumentOutlineView { DataContext = DocumentOutlineViewModel.Instance };


            WorkspaceService.Instance.WorkspaceLoaded += OnWorkspaceLoaded;
            
            // Subscribe to build results
            MessageBus.BuildFinished += OnBuildFinished;
            MessageBus.SystemDiagnostic += OnSystemDiagnostic;
        }

        public bool IsLightTheme => !IsDarkTheme;

        [RelayCommand]
        private void SaveFile()
        {
            if (ActiveDocument is EditorDocumentViewModel editor) editor.Save();
            else if (ActiveDocument is WorkspaceViewModel workspace) workspace.Save();
            StatusText = $"Saved {ActiveDocument?.Title}";
        }

        [RelayCommand]
        private void SaveAll()
        {
            if (_factory.DocumentDock?.VisibleDockables is not null)
            {
                foreach (var dockable in _factory.DocumentDock.VisibleDockables)
                {
                    if (dockable is EditorDocumentViewModel editor && editor.IsModified) editor.Save();
                    else if (dockable is WorkspaceViewModel workspace && workspace.IsModified) workspace.Save();
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

        [RelayCommand]
        private async Task RebuildStartupProject()
        {
            var startupProject = WorkspaceService.Instance.CurrentStartupProject;
            if (startupProject != null)
            {
                StatusText = $"Rebuilding {startupProject.Name}...";
                _factory.ErrorListTool.Clear();
                await startupProject.RebuildCommand.ExecuteAsync(null);
            }
            else StatusText = "Set a startup project first.";
        }

        [RelayCommand]
        private async Task CleanStartupProject()
        {
            var startupProject = WorkspaceService.Instance.CurrentStartupProject;
            if (startupProject != null)
            {
                StatusText = $"Cleaning {startupProject.Name}...";
                await startupProject.CleanCommand.ExecuteAsync(null);
                StatusText = $"Clean finished for {startupProject.Name}";
            }
            else StatusText = "Set a startup project first.";
        }

        [RelayCommand]
        private void NewFile()
        {
            string baseDir = !string.IsNullOrEmpty(WorkspaceService.Instance.CurrentWorkspacePath) 
                ? Path.GetDirectoryName(WorkspaceService.Instance.CurrentWorkspacePath)! 
                : string.Empty;

            if (string.IsNullOrEmpty(baseDir)) return;

            var path = Path.Combine(baseDir, "NewFile.cs");
            int counter = 1;
            while (File.Exists(path))
            {
                path = Path.Combine(baseDir, $"NewFile_{counter++}.cs");
            }

            File.WriteAllText(path, "using System;\n\nnamespace NewNamespace\n{\n    public class NewFile\n    {\n    }\n}\n");
            LoadSolutionAsync(WorkspaceService.Instance.CurrentWorkspacePath);
            StatusText = $"Created {Path.GetFileName(path)}";
        }

        [RelayCommand]
        private void NewFolder()
        {
            string baseDir = !string.IsNullOrEmpty(WorkspaceService.Instance.CurrentWorkspacePath) 
                ? Path.GetDirectoryName(WorkspaceService.Instance.CurrentWorkspacePath)! 
                : string.Empty;

            if (string.IsNullOrEmpty(baseDir)) return;

            var path = Path.Combine(baseDir, "NewFolder");
            int counter = 1;
            while (Directory.Exists(path))
            {
                path = Path.Combine(baseDir, $"NewFolder_{counter++}");
            }

            Directory.CreateDirectory(path);
            LoadSolutionAsync(WorkspaceService.Instance.CurrentWorkspacePath);
            StatusText = $"Created {Path.GetFileName(path)}";
        }

        [RelayCommand]
        private void OpenRecent()
        {
            var recents = RecentProjectsService.GetRecentProjects();
            if (recents.Any()) LoadSolutionAsync(recents.First());
            else StatusText = "No recent projects found.";
        }

        [RelayCommand] private void ManageExtensions() => StatusText = "Extensions: Functionality coming soon...";
        [RelayCommand] private void ToggleSaveMode() => StatusText = "Save Mode: Auto-save toggled.";

        private void OnWorkspaceLoaded(object sender, string newPath)
        {
            RecentProjectsService.AddRecentProject(newPath);
            LoadSolutionAsync(newPath);
        }

        private void OnBuildFinished(BuildFinishedMessage msg)
        {
            _factory.ErrorListTool.LoadDiagnostics(msg.Diagnostics);
            
            // Also update Problems tool for backward compatibility
            _factory.ProblemsTool.UpdateDiagnostics(msg.Diagnostics.Select(d => 
                new Models.ProblemItemViewModel(d.Severity.ToString(), d.Description, d.File, d.Line, d.ProjectPath)));

            StatusText = msg.Success ? "Build succeeded." : "Build failed.";
        }

        private void OnSystemDiagnostic(SystemDiagnosticMessage msg)
        {
            _factory.ErrorListTool.AddDiagnostic(msg.Diagnostic);
            
            _factory.ProblemsTool.UpdateDiagnostics(new[] { 
                new Models.ProblemItemViewModel(msg.Diagnostic.Severity.ToString(), msg.Diagnostic.Description, msg.Diagnostic.File, msg.Diagnostic.Line, msg.Diagnostic.ProjectPath) 
            });
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
                _factory.ProblemsTool.UpdateDiagnostics(new List<Models.ProblemItemViewModel>());

                var projectCount = result.Solution.Projects.Count();
                StatusText = $"Loaded {projectCount} projects";

                // فتح الصفحة الافتراضية تلقائياً (MainWindow.axaml أو أول ملف axaml)
                await OpenDefaultPageAsync(solutionPath, result.Solution);
            }
            catch (Exception ex)
            {
                StatusText = $"Solution load failed: {ex.Message}";
                _factory.ErrorListTool.LoadDiagnostics(new List<DiagnosticItem>
                {
                    new DiagnosticItem
                    {
                        Severity = DiagnosticSeverity.Error,
                        Code = "SLN001",
                        Description = $"Failed to load solution: {ex.Message}",
                        File = solutionPath,
                        Project = Path.GetFileNameWithoutExtension(solutionPath)
                    }
                });
            }
        }

        private async Task OpenDefaultPageAsync(string solutionPath, Microsoft.CodeAnalysis.Solution solution)
        {
            try
            {
                string solutionDir = Path.GetDirectoryName(solutionPath) ?? "";

                // أولوية البحث: MainWindow.axaml ثم App.axaml ثم أي ملف axaml
                string[] priorities = { "MainWindow.axaml", "MainView.axaml", "App.axaml" };

                string? defaultFile = null;

                // نبحث في مجلد الـ solution أولاً
                foreach (var name in priorities)
                {
                    var found = Directory.GetFiles(solutionDir, name, SearchOption.AllDirectories)
                        .FirstOrDefault(f => !f.Contains("obj") && !f.Contains("bin"));
                    if (found != null) { defaultFile = found; break; }
                }

                // إذا لم نجد، نأخذ أول axaml ليس في obj/bin
                if (defaultFile == null)
                {
                    defaultFile = Directory.GetFiles(solutionDir, "*.axaml", SearchOption.AllDirectories)
                        .FirstOrDefault(f => !f.Contains("obj") && !f.Contains("bin"));
                }

                if (defaultFile != null)
                {
                    await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
                        _factory.OpenDocument(defaultFile));
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Could not open default page: {ex.Message}");
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

        [RelayCommand]
        private async Task NewSolution()
        {
            if (Avalonia.Application.Current?.ApplicationLifetime is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop)
            {
                var wizardWindow = new NewProjectWizardWindow(); // Reusing wizard for now or use a solution specific one if available
                await wizardWindow.ShowDialog(desktop.MainWindow);
            }
        }

        [RelayCommand]
        private async Task NewProject() => await NewSolution();

        [RelayCommand]
        private async Task RunProject()
        {
            var startupProject = WorkspaceService.Instance.CurrentStartupProject;
            if (startupProject == null || string.IsNullOrEmpty(startupProject.Path))
            {
                StatusText = "Set a startup project first.";
                return;
            }

            try
            {
                // 1. Build first to catch ALL errors (including libraries)
                StatusText = $"Building {startupProject.Name} before run...";
                _factory.ErrorListTool.Clear();
                await startupProject.BuildCommand.ExecuteAsync(null);

                // If build fail is detected (check if we have any errors in ErrorList)
                if (_factory.ErrorListTool.ErrorCount > 0)
                {
                    StatusText = "Run aborted: Build errors found.";
                    return;
                }

                StatusText = $"Running {startupProject.Name}...";
                string cmdArgs = $"run --project \"{startupProject.Path}\" --no-build"; // Use --no-build since we just built it
                ProcessStartInfo psi = new ProcessStartInfo
                {
                    FileName = "dotnet",
                    Arguments = cmdArgs,
                    UseShellExecute = true,
                    CreateNoWindow = false
                };
                _runningProcess = Process.Start(psi);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error: {ex.Message}");
                StatusText = "Failed to start project.";
                _factory.ErrorListTool.LoadDiagnostics(new List<DiagnosticItem>
                {
                    new DiagnosticItem 
                    { 
                        Severity = DiagnosticSeverity.Error, 
                        Code = "RUN001", 
                        Description = $"Failed to start project: {ex.Message}", 
                        File = startupProject.Path,
                        Project = startupProject.Name
                    }
                });
            }
        }

        [RelayCommand]
        private void StopProject()
        {
            try
            {
                if (_runningProcess != null && !_runningProcess.HasExited)
                {
                    _runningProcess.Kill(true);
                    StatusText = "Stopped project execution.";
                }
                else StatusText = "No project is currently running.";
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error stopping project: {ex.Message}");
            }
            finally
            {
                _runningProcess = null;
            }
        }

        [RelayCommand]
        private async Task BuildSolution()
        {
            if (!string.IsNullOrEmpty(WorkspaceService.Instance.CurrentWorkspacePath))
            {
                StatusText = "Building solution...";
                _factory.ErrorListTool.Clear();
                var slnItem = new SolutionItemViewModel(SolutionItemKind.Project, "Solution", WorkspaceService.Instance.CurrentWorkspacePath);
                await slnItem.BuildCommand.ExecuteAsync(null);
            }
        }

        [RelayCommand]
        private async Task RebuildSolution()
        {
            if (!string.IsNullOrEmpty(WorkspaceService.Instance.CurrentWorkspacePath))
            {
                StatusText = "Rebuilding solution...";
                _factory.ErrorListTool.Clear();
                var slnItem = new SolutionItemViewModel(SolutionItemKind.Project, "Solution", WorkspaceService.Instance.CurrentWorkspacePath);
                await slnItem.RebuildCommand.ExecuteAsync(null);
            }
        }

        [RelayCommand]
        private async Task BuildStartupProject()
        {
            var startupProject = WorkspaceService.Instance.CurrentStartupProject;
            if (startupProject != null)
            {
                StatusText = $"Building {startupProject.Name}...";
                _factory.ErrorListTool.Clear();
                await startupProject.BuildCommand.ExecuteAsync(null);
            }
            else
            {
                StatusText = "No startup project selected for build.";
            }
        }
    }
}
