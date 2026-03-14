using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using VisualEditor.Core;
using VisualEditor.Core.Messages;
using VisualEditor.Core.Services;

namespace VisualEditorApp.Models
{
    public partial class SolutionItemViewModel : ObservableObject
    {
        [ObservableProperty] private bool _isExpanded;
        [ObservableProperty] private bool _isSelected;

        public SolutionItemViewModel(SolutionItemKind kind, string name, string? path)
        {
            Kind = kind;
            Name = name;
            Path = path;
            Children = new ObservableCollection<SolutionItemViewModel>();
        }

        public SolutionItemKind Kind { get; }

        public string Name { get; }

        public string? Path { get; }

        public ObservableCollection<SolutionItemViewModel> Children { get; }

        public bool IsLeaf => Kind == SolutionItemKind.Document;


        // 💡 خاصية عشان نحدد هل العنصر ده مشروع (عشان نظهر زراير البناء)
        public bool IsProjectNode => Kind == SolutionItemKind.Project || Kind == SolutionItemKind.Solution;

        // ==========================================
        // 🚀 أوامر القائمة (Commands)
        // ==========================================

        [RelayCommand]
        private void Open()
        {
            // هنا هتحط الكود بتاعك اللي بيفتح الملف في المحرر 
            // (زي اللي كنت بتعمله في الـ Double Click)
            System.Diagnostics.Debug.WriteLine($"Opening file: {Path}");
        }

        [RelayCommand]
        private async Task BuildAsync()
        {
            if (IsProjectNode && !string.IsNullOrEmpty(Path))
            {
                System.Diagnostics.Debug.WriteLine($"Starting Build for: {Name}...");
                // 1. تشغيل الـ Build (الأمر اللي عملناه قبل كده)
                bool success = await RunDotnetCommandAsync("build", Path);

                if (success)
                {
                    // 2. 📢 إرسال إشارة لكل الأجزاء المهتمة (زي المصمم) إن في DLLs جديدة جاهزة
                    // بنستخدم Messenger بتاع CommunityToolkit
                    WeakReferenceMessenger.Default.Send(new ProjectBuiltMessage(Path));

                    System.Diagnostics.Debug.WriteLine("✅ Build Succeeded! Refreshing Designer...");
                }
            }
        }

        [RelayCommand]
        private async Task CleanAsync()
        {
            if (IsProjectNode && !string.IsNullOrEmpty(Path))
            {
                System.Diagnostics.Debug.WriteLine($"Starting Clean for: {Name}...");
                await RunDotnetCommandAsync("clean", Path);
            }
        }

        // ==========================================
        // ⚙️ المحرك السري: تشغيل أوامر الدوت نت في الخلفية
        // ==========================================
        private async Task<bool> RunDotnetCommandAsync(string command, string projectPath)
        {
            return await Task.Run(() =>
            {
                try
                {
                    var processInfo = new ProcessStartInfo
                    {
                        FileName = "dotnet",
                        Arguments = $"{command} \"{projectPath}\"",
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    };

                    using var process = new Process { StartInfo = processInfo };

                    process.OutputDataReceived += (s, e) => { if (e.Data != null) Debug.WriteLine(e.Data); };
                    process.ErrorDataReceived += (s, e) => { if (e.Data != null) Debug.WriteLine($"ERROR: {e.Data}"); };

                    process.Start();
                    process.BeginOutputReadLine();
                    process.BeginErrorReadLine();

                    process.WaitForExit();

                    Debug.WriteLine($"=== {command.ToUpper()} COMPLETED with code {process.ExitCode} ===");

                    if (process.ExitCode == 0)
                    {
                        return true;
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Failed to run {command}: {ex.Message}");
                }
                return false; // Ensure a value is always returned
            });
        }


        [RelayCommand]
        private void SetAsStartup()
        {
            if (!IsProjectNode || string.IsNullOrEmpty(Path)) return;

            try
            {
                // 1. تحديد مكان مجلد الـ bin للمشروع المختار
                string projDir = System.IO.Path.GetDirectoryName(Path);
                string binPath = System.IO.Path.Combine(projDir, "bin");

                if (System.IO.Directory.Exists(binPath))
                {
                    // 2. البحث عن أحدث مجلد Debug أو Release
                    var latestBin = new System.IO.DirectoryInfo(binPath)
                        .GetDirectories("*", System.IO.SearchOption.AllDirectories)
                        .Where(d => d.GetFiles("*.dll").Length > 2)
                        .OrderByDescending(d => d.LastWriteTime)
                        .FirstOrDefault();

                    if (latestBin != null)
                    {
                        // 3. 💾 حفظ في الإعدادات (باستخدام الخدمة اللي عملناها)
                        var settings = SettingsService.Load();
                        settings.LastStartupProjectBin = latestBin.FullName;
                        SettingsService.Save(settings);

                        // 4. 🚀 تحديث المحرك فوراً عشان يحمل الـ DLLs الجديدة
                        LiveDesignerCompiler.UpdateStartupPath(latestBin.FullName);

                        System.Diagnostics.Debug.WriteLine($"✅ Startup Project set to: {Name}");
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"🔴 Error setting startup project: {ex.Message}");
            }
        }
    }
}
