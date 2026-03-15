using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Dock.Model.Controls;
using Dock.Model.Core;
using Dock.Model.Mvvm.Controls;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using VisualEditorApp.Services;
using VisualEditorApp.ViewModels.Documents;
using VisualEditorApp.ViewModels.Tools;

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
            // 🎯 التسجيل المباشر والنظيف مع الـ Service
            WorkspaceService.Instance.WorkspaceLoaded += OnWorkspaceLoaded;
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
        // الدالة اللي هتشتغل أول ما المدير يبلغنا بمسار جديد
        private void OnWorkspaceLoaded(object sender, string newPath)
        {
            // هنا بنادي على دالة قراءة الهارد ديسك بتاعتك
            LoadSolutionAsync(newPath);
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
        [RelayCommand]
        private async void OpenNewProject()
        {
            if (Avalonia.Application.Current?.ApplicationLifetime is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop)
            {
                // 🎯 فتح البرواز الذكي بدل الشاشات القديمة
                var wizardWindow = new NewProjectWizardWindow();
                await wizardWindow.ShowDialog(desktop.MainWindow);
            }
        }
        [RelayCommand]
        private async Task RunProject()
        {
            var startupProject = WorkspaceService.Instance.CurrentStartupProject;

            if (startupProject == null || string.IsNullOrEmpty(startupProject.Path))
            {
                Debug.WriteLine("🔴 مفيش مشروع مختار كـ Startup! حدد مشروع كليك يمين أولاً.");
                return;
            }

            try
            {
                Debug.WriteLine($"🚀 جاري تشغيل المشروع: {startupProject.Name}...");

                // استخدام dotnet run مع تحديد مسار المشروع (.csproj)
                // خيار --no-build بيخليه يفتح بسرعة لو إنت لسه عامل Build
                string cmdArgs = $"run --project \"{startupProject.Path}\"";

                ProcessStartInfo psi = new ProcessStartInfo
                {
                    FileName = "dotnet",
                    Arguments = cmdArgs,
                    UseShellExecute = true, // 🎯 دي اللي هتفتح شاشة Console خارجية (سودة)
                    CreateNoWindow = false
                };

                Process.Start(psi);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"🔴 فشل تشغيل المشروع: {ex.Message}");
            }
        }

        [RelayCommand]
        private async Task BuildStartupProject()
        {
            var startupProject = WorkspaceService.Instance.CurrentStartupProject;

            if (startupProject != null)
            {
                // بننادي دالة الـ Build اللي إنت مبرمجها جوه الـ ViewModel بتاع المشروع
                await startupProject.BuildCommand.ExecuteAsync(null);
            }
            else
            {
                Debug.WriteLine("⚠️ مفيش مشروع Startup مبني حالياً عشان أعمله Build.");
            }
        }
    }
}
