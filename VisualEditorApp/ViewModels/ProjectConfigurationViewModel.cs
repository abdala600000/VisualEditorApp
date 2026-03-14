using Avalonia;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Threading.Tasks;
using VisualEditorApp.Services;

namespace VisualEditorApp.ViewModels
{
    public partial class ProjectConfigurationViewModel : ObservableObject
    {
        // ضيف المتغير ده مع بقية المتغيرات اللي فوق
        [ObservableProperty]
        private bool _initializeGit = true; // الافتراضي متعلم عليه صح
        [ObservableProperty]
        private string _templateShortName; // ده اللي هيشيل كلمة زي (avalonia.app)

    

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(ProjectPathPreview))]
        [NotifyPropertyChangedFor(nameof(SolutionPathPreview))]
        private string _projectName = "MyProject";

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(ProjectPathPreview))]
        [NotifyPropertyChangedFor(nameof(SolutionPathPreview))]
        private string _location = @"C:\Users\User\Documents";

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(SolutionPathPreview))]
        private string _solutionName = "MyProject";

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(ProjectPathPreview))]
        private bool _createProjectDirectory = true;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(SolutionPathPreview))]
        private bool _createSolutionDirectory = true;

        [ObservableProperty]
        private bool _addProjectToSolution = true;

        // 🎯 الإشارة اللي هنبعتها عشان نقفل الشاشة بعد ما نخلص
        public event EventHandler RequestClose;
        // قائمة المشاريع في الجدول
        public ObservableCollection<ProjectItem> ProjectsList { get; } = new();
        public ProjectConfigurationViewModel(string templateShortName)
        {
            TemplateShortName = templateShortName;
            // إضافة عنصر افتراضي للجدول
            ProjectsList.Add(new ProjectItem { Name = "MyProject", CreateDir = true });
        }

        // 🎯 الحساب الحي لمسار المشروع (Live Preview)
        public string ProjectPathPreview
        {
            get
            {
                if (string.IsNullOrWhiteSpace(Location) || string.IsNullOrWhiteSpace(ProjectName)) return "";

                string path = Location;
                // إذا كان المجلد الخاص بالـ Solution مفعل
                if (CreateSolutionDirectory && !string.IsNullOrWhiteSpace(SolutionName))
                {
                    path = Path.Combine(path, SolutionName);
                }

                if (CreateProjectDirectory)
                {
                    path = Path.Combine(path, ProjectName);
                }

                return Path.Combine(path, $"{ProjectName}.csproj");
            }
        }

        // 🎯 الحساب الحي لمسار الـ Solution
        public string SolutionPathPreview
        {
            get
            {
                if (string.IsNullOrWhiteSpace(Location) || string.IsNullOrWhiteSpace(SolutionName)) return "";

                string path = Location;
                if (CreateSolutionDirectory)
                {
                    path = Path.Combine(path, SolutionName);
                }
                return Path.Combine(path, $"{SolutionName}.sln");
            }
        }


        [RelayCommand]
        private async Task Browse(object? parameter)
        {
            // 1. بنحول الـ parameter لـ Visual (اللي هو الزرار اللي اتبعت من الـ XAML)
            if (parameter is Visual visual)
            {
                // 2. بنباصي الـ visual للخدمة عشان الإيرور يختفي
                var selectedPath = await Services.FolderPickerService.SelectFolderAsync(visual);

                if (!string.IsNullOrEmpty(selectedPath))
                {
                    Location = selectedPath;
                }
            }
        }
        [RelayCommand]
        private void Cancel()
        {
            // ضرب جرس الإغلاق عشان المايسترو يقفل الشاشة
            RequestClose?.Invoke(this, EventArgs.Empty);
        }

        // ضيف الإشارة دي فوق في الكلاس عشان نبلغ الـ MainWindow إننا خلصنا وجاهزين نـ Load
        public event EventHandler<string> WorkspaceCreated;

        [RelayCommand]
        private void Create()
        {
            try
            {
                // 1. تظبيط مسارات الفولدرات
                string slnDir = CreateSolutionDirectory ? Path.Combine(Location, SolutionName) : Location;
                Directory.CreateDirectory(slnDir);

                string projDir = CreateProjectDirectory ? Path.Combine(slnDir, ProjectName) : slnDir;

                // 2. إنشاء المشروع الفعلي (بناءً على التمبليت اللي جاي من الشاشة الأولى)
                ExecuteCommand("dotnet", $"new {TemplateShortName} -n \"{ProjectName}\" -o \"{projDir}\"", slnDir);

                // 3. إنشاء السوليوشن وربطه (لو المستخدم طالب ده)
                if (CreateSolutionDirectory)
                {
                    ExecuteCommand("dotnet", $"new sln -n \"{SolutionName}\"", slnDir);

                    string slnPath = Path.Combine(slnDir, $"{SolutionName}.slnx");
                    string csprojPath = Path.Combine(projDir, $"{ProjectName}.csproj");

                    // نتأكد إن ملف المشروع اتكريت فعلاً قبل ما نربطه
                    if (System.IO.File.Exists(csprojPath))
                    {
                        ExecuteCommand("dotnet", $"sln \"{slnPath}\" add \"{csprojPath}\"", slnDir);
                    }
                }

                // 4. قفل الشاشة وفتح السوليوشن في البرنامج
                string finalPath = CreateSolutionDirectory ? Path.Combine(slnDir, $"{SolutionName}.slnx") : Path.Combine(projDir, $"{ProjectName}.csproj");
                WorkspaceService.Instance.LoadWorkspace(finalPath);
                RequestClose?.Invoke(this, EventArgs.Empty);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"🔴 خطأ في الإنشاء: {ex.Message}");
            }
        }

        // 🎯 دالة التنفيذ المباشرة (استخدمنا WaitForExit عشان نجبر البرنامج يستنى الأمر يخلص بجد)
        private void ExecuteCommand(string cmd, string args, string workingDir)
        {
            var process = System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = cmd,
                Arguments = args,
                WorkingDirectory = workingDir,
                CreateNoWindow = true,
                UseShellExecute = false
            });

            // السر هنا: الكود هيقف ومش هيكمل غير لما الدوت نت يخلص إنشاء الملفات
            process?.WaitForExit();
        }
    }

    // كلاس بسيط لتمثيل السطر جوه الجدول
    public class ProjectItem : ObservableObject
    {
        private string _name;
        public string Name { get => _name; set => SetProperty(ref _name, value); }

        private bool _createDir;
        public bool CreateDir { get => _createDir; set => SetProperty(ref _createDir, value); }
    }
}