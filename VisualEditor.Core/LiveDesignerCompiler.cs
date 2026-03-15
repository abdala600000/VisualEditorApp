using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using System.Reflection;
using System.Text.RegularExpressions;
using VisualEditor.Core.Models;
using VisualEditor.Core.Services;
using VisualEditor.Core.Messages;

namespace VisualEditor.Core
{
    public static class LiveDesignerCompiler
    {
        // قاموس لحفظ الـ DLLs اللي حملناها في الذاكرة لتسريع الوصول إليها
        private static readonly Dictionary<string, Assembly> _loadedAssemblies = new(StringComparer.OrdinalIgnoreCase);

        private static string _lastBinDir; // متغير لحفظ مسار الـ bin
        private static AppSettings _currentSettings;
        static LiveDesignerCompiler()
        {

            // 1. أول ما البرنامج يشتغل، يحمل الإعدادات القديمة
            _currentSettings = SettingsService.Load();
            _lastBinDir = _currentSettings.LastStartupProjectBin;


            // 🚀 السحر هنا: حل مشكلة 'Could not load file or assembly'
            AppDomain.CurrentDomain.AssemblyResolve += (sender, args) =>
            {
                // بناخد الاسم الصغير للمكتبة (مثلاً MyStudio.Common)
                string shortName = new AssemblyName(args.Name).Name;

                // 1. بندور في القاموس بتاعنا
                if (_loadedAssemblies.TryGetValue(shortName, out var asm))
                    return asm;

                // 2. بندور في الـ Assemblies اللي الـ .NET حملها فعلاً
                return AppDomain.CurrentDomain.GetAssemblies()
                    .FirstOrDefault(a => a.GetName().Name == shortName);
            };
        }
        
        internal sealed class DesignerUriContextServiceProvider : IServiceProvider, IUriContext
        {
            public Uri BaseUri { get; set; }

            public DesignerUriContextServiceProvider(string filePath)
            {
                // بنحول المسار لـ URI عشان المكتبة تقدر تبني عليه المسارات النسبية
                BaseUri = new Uri($"file:///{filePath.Replace('\\', '/')}");
            }

            public object? GetService(Type serviceType)
            {
                return serviceType == typeof(IUriContext) ? this : null;
            }
        }

        public static Control RenderLiveXaml(string xamlText, string filePath)
        {  
            string projName = null;
            try
            {
                // 1. شحن كل الـ DLLs من مجلد bin للذاكرة فوراً
                LoadProjectAssemblies(filePath);

                // 2. تفعيل محرك AXSG (مهم جداً)
                XamlToCSharpGenerator.Runtime.AvaloniaSourceGeneratedXamlLoader.Enable();

                // 3. تجهيز البيانات الأساسية
              
                var classMatch = System.Text.RegularExpressions.Regex.Match(xamlText, @"x:Class\s*=\s*""([^""]+)""");
                if (classMatch.Success) projName = classMatch.Groups[1].Value.Split('.')[0];

                // تنظيف الـ XAML لضمان الرسم حتى لو الـ DLL لسه مافيهوش الكلاس
                string safeXaml = System.Text.RegularExpressions.Regex.Replace(xamlText, @"x:Class\s*=\s*""[^""]*""", "");

                // 2. مسح أحداث الضغط (Events)
                safeXaml = Regex.Replace(safeXaml, @"\b(Click|Tapped|SelectionChanged|TextChanged)=""[^""\{]*""", "");

                Uri fileUri = filePath.StartsWith("file://", StringComparison.OrdinalIgnoreCase)
                    ? new Uri(filePath) : new Uri($"file:///{filePath.Replace('\\', '/')}");


                if (!string.IsNullOrEmpty(projName))
                {
                    // بنقوله دور على الموارد (avares) في المشروع بتاع المستخدم
                    fileUri = new Uri($"avares://{projName}/");
                }

                // 4. الحصول على الـ Anchor من المشروع الأساسي
                _loadedAssemblies.TryGetValue(projName ?? "", out var targetAssembly);
                Type anchorType = targetAssembly?.GetTypes().FirstOrDefault();

                // 5. الاستدعاء النهائي
                var loadedObject = XamlToCSharpGenerator.Runtime.AvaloniaSourceGeneratedXamlLoader.Load(
                    xaml: safeXaml,
                    localAssemblyAnchorType: anchorType,
                    localAssemblyName: projName,
                    baseUri: fileUri,
                    designMode: true);

                return loadedObject as Control ?? new Border();
            }
            catch (Exception ex)
            {
                // إبلاغ قائمة الأخطاء فوراً بالخطأ التقني
                MessageBus.Send(SystemDiagnosticMessage.Create(DiagnosticSeverity.Error, "DESIGN003", $"XAML Render Error: {ex.Message}", filePath));

                return new Border
                {
                    Background = Avalonia.Media.Brushes.LightYellow,
                    Padding = new Avalonia.Thickness(10),
                    Child = new TextBlock
                    {
                        Text = $"❌ Runtime Error:\n{ex.Message}\n\nCheck if '{projName}' and its dependencies are built.",
                        Foreground = Avalonia.Media.Brushes.Red,
                        TextWrapping = Avalonia.Media.TextWrapping.Wrap
                    }
                };
            }
        }



        private static void LoadProjectAssemblies(string filePath)
        {
            // 2. لو المسار متسجل عندنا ومشروع الـ bin لسه موجود، نستخدمه فوراً لتوفير الوقت
            if (!string.IsNullOrEmpty(_lastBinDir) && Directory.Exists(_lastBinDir))
            {
                LoadFromDirectory(_lastBinDir);
                return;
            }

            // 3. لو مش موجود، نشغل منطق البحث الذكي اللي عملناه (GetStartupProjectBin)
            string solutionDir = FindSolutionRoot(Path.GetDirectoryName(filePath));
            string startupBin = GetStartupProjectBin(solutionDir);

            if (!string.IsNullOrEmpty(startupBin))
            {
                _lastBinDir = startupBin;

                // 4. 💾 حفظ المسار الجديد عشان المرة الجاية
                _currentSettings.LastStartupProjectBin = startupBin;
                _currentSettings.LastOpenedSolution = solutionDir;
                SettingsService.Save(_currentSettings);

                LoadFromDirectory(startupBin);
            }
        }

        // دالة مساعدة لتحميل الملفات
        private static void LoadFromDirectory(string path)
        {
            var dllFiles = Directory.GetFiles(path, "*.dll", SearchOption.AllDirectories);
            foreach (var dll in dllFiles)
            {
                try
                {
                    var name = AssemblyName.GetAssemblyName(dll).Name;
                    if (AppDomain.CurrentDomain.GetAssemblies().All(a => a.GetName().Name != name))
                    {
                        Assembly.Load(File.ReadAllBytes(dll));
                    }
                }
                catch (Exception ex) 
                { 
                    System.Diagnostics.Debug.WriteLine($"Error loading DLL {dll}: {ex.Message}");
                    MessageBus.Send(SystemDiagnosticMessage.Create(DiagnosticSeverity.Warning, "DLL001", $"Failed to load assembly {Path.GetFileName(dll)}: {ex.Message}"));
                }
            }
        }


        public static void Refresh()
        {
            // بما إننا بنستخدم File.ReadAllBytes، فإحنا مش عاملين Lock للملفات
            // فبمجرد ما ننادي على LoadProjectAssemblies تاني، هيقرأ النسخ الجديدة
            if (!string.IsNullOrEmpty(_lastBinDir))
            {
                LoadFromDirectory(_lastBinDir);
            }
        }

         
        private static string GetStartupProjectBin(string solutionDir)
        {
            try
            {
                var projectFiles = Directory.GetFiles(solutionDir, "*.csproj", SearchOption.AllDirectories);

                foreach (var projFile in projectFiles)
                {
                    // استخدام FileShare.ReadWrite لمنع الـ File Locking
                    string header = "";
                    using (var fs = new FileStream(projFile, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                    using (var reader = new StreamReader(fs))
                    {
                        char[] buffer = new char[1000];
                        int bytesRead = reader.Read(buffer, 0, buffer.Length);
                        header = new string(buffer, 0, bytesRead);
                    }

                    if (header.Contains("<OutputType>WinExe</OutputType>") ||
                        header.Contains("<OutputType>Exe</OutputType>"))
                    {
                        string projDir = Path.GetDirectoryName(projFile);
                        string binPath = Path.Combine(projDir, "bin");

                        if (Directory.Exists(binPath))
                        {
                            // نأخذ أحدث مجلد (Debug/net8.0 مثلاً)
                            var latestBin = new DirectoryInfo(binPath)
                                .GetDirectories("*", SearchOption.AllDirectories)
                                .Where(d => d.GetFiles("*.dll").Length > 5)
                                .OrderByDescending(d => d.LastWriteTime)
                                .FirstOrDefault();

                            return latestBin?.FullName;
                        }
                    }
                }
            }
            catch (Exception ex) 
            { 
               MessageBus.Send(SystemDiagnosticMessage.Create(DiagnosticSeverity.Error, "SYS003", $"Error finding startup bin: {ex.Message}"));
            }
            return null;
        }



        // دالة مساعدة للوصول لملف الـ .sln
        private static string FindSolutionRoot(string startDir)
        {
            var dir = new DirectoryInfo(startDir);
            while (dir != null)
            {
                if (dir.GetFiles("*.sln").Any()) return dir.FullName;
                dir = dir.Parent;
            }
            return null;
        }
        public static void UpdateStartupPath(string newPath)
        {
            _lastBinDir = newPath;
            // اختياري: ممكن تنادي على LoadProjectAssemblies هنا لو عاوز المصمم يتحدث فوراً
        }
        
    }
}

