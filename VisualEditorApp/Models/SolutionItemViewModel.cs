using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using VisualEditor.Core;
using VisualEditor.Core.Messages;
using VisualEditor.Core.Models;
using VisualEditor.Core.Services;
using VisualEditorApp.Services;

namespace VisualEditorApp.Models
{
    public partial class SolutionItemViewModel : ObservableObject
    {
        [ObservableProperty] private string _name;
        [ObservableProperty] private string _path;
        [ObservableProperty] private SolutionItemKind _kind;
        [ObservableProperty] private ObservableCollection<SolutionItemViewModel> _children = new();
        [ObservableProperty] private bool _isExpanded;
        [ObservableProperty] private bool _isBuilding;
        [ObservableProperty] [NotifyPropertyChangedFor(nameof(Self))] private bool _isStartupProject;

        public SolutionItemViewModel(SolutionItemKind kind, string name, string? path)
        {
            _kind = kind;
            _name = name;
            _path = path ?? string.Empty;
        }

        public bool IsProjectNode => Kind == SolutionItemKind.Project;
        public bool IsSolutionNode => Kind == SolutionItemKind.Solution;

        public SolutionItemViewModel Self => this;

        // ==========================================
        // 🛠️ الأوامر (Commands)
        // ==========================================

        [RelayCommand]
        private void Open()
        {
            System.Diagnostics.Debug.WriteLine($"Opening file: {Path}");
        }

        [RelayCommand]
        private void SetAsStartup()
        {
            if (IsProjectNode)
            {
                WorkspaceService.Instance.SetCurrentStartupProject(this);


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
                    }
                }
            }
        }

        [RelayCommand]
        private async Task BuildAsync()
        {
            if ((IsProjectNode || IsSolutionNode) && !string.IsNullOrEmpty(Path))
            {
                try
                {
                    IsBuilding = true;
                    var diagnostics = new List<DiagnosticItem>();
                    bool success = await RunDotnetCommandAsync("build", Path, diagnostics);
                    MessageBus.Send(new BuildFinishedMessage(success, diagnostics));
                }
                finally
                {
                    IsBuilding = false;
                }
            }
        }

        [RelayCommand]
        private async Task CleanAsync()
        {
            if ((IsProjectNode || IsSolutionNode) && !string.IsNullOrEmpty(Path))
            {
                try
                {
                    IsBuilding = true;
                    var diagnostics = new List<DiagnosticItem>();
                    bool success = await RunDotnetCommandAsync("clean", Path, diagnostics);
                    MessageBus.Send(new BuildFinishedMessage(success, diagnostics));
                }
                finally
                {
                    IsBuilding = false;
                }
            }
        }

        [RelayCommand]
        private async Task RebuildAsync()
        {
            if ((IsProjectNode || IsSolutionNode) && !string.IsNullOrEmpty(Path))
            {
                try
                {
                    IsBuilding = true;
                    var diagnostics = new List<DiagnosticItem>();
                    bool success = await RunDotnetCommandAsync("build /t:Rebuild", Path, diagnostics);
                    MessageBus.Send(new BuildFinishedMessage(success, diagnostics));
                }
                finally
                {
                    IsBuilding = false;
                }
            }
        }

        // ==========================================
        // ⚙️ المحرك السري: تشغيل أوامر الدوت نت وتعقب الأخطاء
        // ==========================================
        private async Task<bool> RunDotnetCommandAsync(string command, string projectPath, List<DiagnosticItem> diagnostics = null)
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
                    
                    // Regex for MSBuild errors/warnings: file(line[,col]): kind code: msg [project]
                    var regex = new Regex(@"^(?<file>.*?)\((?<line>\d+)(?:,(?<col>\d+))?\): (?<kind>error|warning) (?<code>\w+): (?<msg>.*?)(\s+\[(?<proj>.*)\])?$", RegexOptions.Compiled);

                    process.OutputDataReceived += (s, e) =>
                    {
                        if (string.IsNullOrEmpty(e.Data)) return;
                        Debug.WriteLine(e.Data);

                        if (diagnostics != null)
                        {
                            var match = regex.Match(e.Data);
                            if (match.Success)
                            {
                                string filePath = match.Groups["file"].Value;
                                string projectInMsg = match.Groups["proj"].Value;
                                
                                if (!System.IO.Path.IsPathRooted(filePath))
                                {
                                    string? projectDir = System.IO.Path.GetDirectoryName(projectPath);
                                    if (projectDir != null)
                                        filePath = System.IO.Path.GetFullPath(System.IO.Path.Combine(projectDir, filePath));
                                }

                                var diag = new DiagnosticItem
                                {
                                    File = filePath,
                                    Line = int.Parse(match.Groups["line"].Value),
                                    Column = match.Groups["col"].Success ? int.Parse(match.Groups["col"].Value) : 0,
                                    Severity = match.Groups["kind"].Value.ToLower() == "error" ? DiagnosticSeverity.Error : DiagnosticSeverity.Warning,
                                    Code = match.Groups["code"].Value,
                                    Description = match.Groups["msg"].Value,
                                    Project = !string.IsNullOrEmpty(projectInMsg) ? System.IO.Path.GetFileNameWithoutExtension(projectInMsg) : System.IO.Path.GetFileNameWithoutExtension(projectPath),
                                    ProjectPath = !string.IsNullOrEmpty(projectInMsg) ? projectInMsg : projectPath
                                };
                                diagnostics.Add(diag);
                            }
                        }
                    };

                    process.ErrorDataReceived += (s, e) => 
                    { 
                        if (e.Data != null) 
                        {
                            Debug.WriteLine($"ERROR: {e.Data}");
                            // Already handled by Regex if it's a standard error, but for raw errors:
                            if (e.Data.Contains(": error "))
                            {
                                // Captured by logic above usually
                            }
                        }
                    };

                    process.Start();
                    process.BeginOutputReadLine();
                    process.BeginErrorReadLine();

                    process.WaitForExit();
                    return process.ExitCode == 0;
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Failed to run {command}: {ex.Message}");
                    
                    if (diagnostics != null)
                    {
                        diagnostics.Add(new DiagnosticItem
                        {
                            Severity = DiagnosticSeverity.Error,
                            Code = "SYS001",
                            Description = $"System Error: Failed to run {command}. {ex.Message}",
                            Project = System.IO.Path.GetFileNameWithoutExtension(projectPath),
                            ProjectPath = projectPath,
                            File = projectPath
                        });
                    }
                }
                return false;
            });
        }
    }
}
