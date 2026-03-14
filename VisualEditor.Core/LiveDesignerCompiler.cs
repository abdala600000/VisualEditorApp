using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using System.Reflection;
using System.Text.RegularExpressions;
using VisualEditor.Core.Models;
using VisualEditor.Core.Services;

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
        //public static Control RenderLiveXaml(string xamlText)
        //{


        //    try
        //    {
        //        // ========================================================
        //        // 💡 التريكة الذهبية: تنظيف الـ XAML من الكلاسات والأحداث
        //        // ========================================================

        //        // 1. مسح الكلاس المرتبط (Code-Behind)
        //        xamlText = Regex.Replace(xamlText, @"x:Class=""[^""]*""", "");

        //        // 2. مسح أحداث الضغط (Events)
        //        xamlText = Regex.Replace(xamlText, @"\b(Click|Tapped|SelectionChanged|TextChanged)=""[^""\{]*""", "");

        //        // 3. 👈 السطر الجديد (الصور): مسح مسارات الصور الخارجية لمنع الكراش
        //        xamlText = Regex.Replace(xamlText, @"Source=""avares://[^""]*""", "");

        //        // 4. 👈 السطر الجديد (الخطوط): استبدال الخطوط الخارجية بخط افتراضي آمن
        //        xamlText = Regex.Replace(xamlText, @"avares://[^<""]*", "Arial");

        //        // (اختياري) تحويل Window إلى UserControl
        //        xamlText = xamlText.Replace("<Window", "<UserControl").Replace("</Window>", "</UserControl>");
        //        // لتشاهد اسم دالة الـ Load في الإصدار الذي قمت بتثبيته (قد تكون RuntimeLoader أو XamlLoader)
        //        var loadedObject = XamlToCSharpGenerator.Runtime.AvaloniaSourceGeneratedXamlLoader.Load(xamlText, designMode: true);

        //        if (loadedObject is Control control)
        //        {
        //            return control;
        //        }

        //        // إذا كان المحمل عبارة عن ستايل (Style) وليس واجهة مرئية
        //        return new Border
        //        {
        //            Background = Brushes.LightGray,
        //            Child = new TextBlock { Text = "تم تحميل الستايل بنجاح (AXSG Engine)" }
        //        };
        //    }
        //    catch (Exception ex)
        //    {
        //        // 3. Fallback UI لعرض الأخطاء في المصمم
        //        return new Border
        //        {
        //            BorderBrush = Brushes.Red,
        //            BorderThickness = new Avalonia.Thickness(2),
        //            Background = Brushes.LightYellow,
        //            Padding = new Avalonia.Thickness(10),
        //            Child = new TextBlock
        //            {
        //                Text = $"⚠️ خطأ في محرك AXSG:\n{ex.Message}",
        //                Foreground = Brushes.DarkRed,
        //                FontWeight = FontWeight.Bold,
        //                TextWrapping = TextWrapping.Wrap
        //            }
        //        };
        //    }
        //}


        // 1. كلاس الـ GPS (مزود المسارات اللي فهمناه من المكتبة)
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
                    fileUri = new Uri($"avares://{projName}");
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
                catch { }
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

        // 1. الدالة الأساسية اللي الرسام بينادي عليها
        //private static void LoadProjectAssemblies(string filePath)
        //{
        //    try
        //    {
        //        // إيجاد جذر الحل (Solution Root)
        //        string solutionDir = FindSolutionRoot(Path.GetDirectoryName(filePath));
        //        if (string.IsNullOrEmpty(solutionDir)) return;

        //        // 💡 استخدام المنطق الجديد: البحث عن مشروع التشغيل (Exe)
        //        string startupBin = GetStartupProjectBin(solutionDir);

        //        // لو ملقيناش Startup Project (مثلاً مشروع مكتبة لوحده)، نستخدم المنطق القديم كخطة بديلة
        //        if (string.IsNullOrEmpty(startupBin))
        //        {
        //            startupBin = _lastBinDir; // أو أي مسار افتراضي
        //        }

        //        if (!string.IsNullOrEmpty(startupBin))
        //        {
        //            _lastBinDir = startupBin;
        //            System.Diagnostics.Debug.WriteLine($"🚀 Loading from Startup Project: {startupBin}");

        //            var dllFiles = Directory.GetFiles(startupBin, "*.dll", SearchOption.AllDirectories);
        //            foreach (var dll in dllFiles)
        //            {
        //                try
        //                {
        //                    var name = AssemblyName.GetAssemblyName(dll).Name;
        //                    if (AppDomain.CurrentDomain.GetAssemblies().All(a => a.GetName().Name != name))
        //                    {
        //                        Assembly.Load(File.ReadAllBytes(dll));
        //                    }
        //                }
        //                catch { }
        //            }
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        System.Diagnostics.Debug.WriteLine($"🔴 Error loading assemblies: {ex.Message}");
        //    }
        //}

        //// 2. 🎯 الدالة الجديدة (المستشعر): بتدور على المشروع اللي نوعه Exe
        private static string GetStartupProjectBin(string solutionDir)
        {
            try
            {
                var projectFiles = Directory.GetFiles(solutionDir, "*.csproj", SearchOption.AllDirectories);

                foreach (var projFile in projectFiles)
                {
                    // قراءة أول 1000 حرف من الملف (كافي جداً لمعرفة الـ OutputType) لسرعة الأداء
                    string header = File.ReadLines(projFile).Take(30).Aggregate("", (a, b) => a + b);

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
            catch { }
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
        //private static void LoadProjectAssemblies(string filePath)
        //{
        //    try
        //    {
        //        var fileInfo = new FileInfo(filePath);
        //        string currentDir = fileInfo.DirectoryName;

        //        // 1. 🕵️ البحث عن "جذر الحل" (Solution Root) - بنفضل نرجع لورا لحد ما نلاقي ملف .sln
        //        string solutionDir = null;
        //        var dirInfo = new DirectoryInfo(currentDir);

        //        while (dirInfo != null)
        //        {
        //            // Debug: بنشوف إحنا واقفين فين دلوقتي
        //            System.Diagnostics.Debug.WriteLine($"Searching for Solution in: {dirInfo.FullName}");

        //            if (dirInfo.GetFiles("*.sln").Any())
        //            {
        //                solutionDir = dirInfo.FullName;
        //                break;
        //            }
        //            dirInfo = dirInfo.Parent; // ارجع خطوة لورا
        //        }

        //        if (string.IsNullOrEmpty(solutionDir))
        //        {
        //            System.Diagnostics.Debug.WriteLine("⚠️ لم يتم العثور على ملف .sln، سأحاول البحث في الفولدرات المجاورة.");
        //            solutionDir = Directory.GetParent(currentDir)?.Parent?.Parent?.FullName;
        //        }

        //        // 2. تحديد اسم المشروع من مسار الملف الحالي
        //        // لو المسار D:\Solution\Feed_Project\Views\Page.axaml -> اسم المشروع Feed_Project
        //        string[] parts = currentDir.Split(Path.DirectorySeparatorChar);
        //        string baseProjectName = "";

        //        // بندور على الفولدر اللي قبل Views أو الفولدر الرئيسي
        //        int viewsIndex = Array.LastIndexOf(parts, "Views");
        //        if (viewsIndex > 0) baseProjectName = parts[viewsIndex - 1];
        //        else baseProjectName = parts.Last();

        //        System.Diagnostics.Debug.WriteLine($"🎯 Base Project Name: {baseProjectName}");

        //        // 3. 🚀 البحث عن مجلد الـ Desktop المناسب
        //        // بندور على أي فولدر جوه الـ Solution يكون فيه اسم المشروع + كلمة Desktop
        //        var solutionFolder = new DirectoryInfo(solutionDir);
        //        var desktopDir = solutionFolder.GetDirectories($"*{baseProjectName}*.Desktop", SearchOption.AllDirectories)
        //                                       .FirstOrDefault();

        //        if (desktopDir != null)
        //        {
        //            System.Diagnostics.Debug.WriteLine($"✅ Found Desktop Project: {desktopDir.FullName}");

        //            // 4. البحث عن مجلد الـ bin جوه الـ Desktop
        //            string binPath = FindLatestBin(desktopDir.FullName);

        //            if (!string.IsNullOrEmpty(binPath))
        //            {
        //                _lastBinDir = binPath;
        //                System.Diagnostics.Debug.WriteLine($"📦 Loading DLLs from: {binPath}");

        //                var dllFiles = Directory.GetFiles(binPath, "*.dll", SearchOption.AllDirectories);
        //                foreach (var dll in dllFiles)
        //                {
        //                    try
        //                    {
        //                        var name = AssemblyName.GetAssemblyName(dll).Name;
        //                        if (AppDomain.CurrentDomain.GetAssemblies().All(a => a.GetName().Name != name))
        //                        {
        //                            Assembly.Load(File.ReadAllBytes(dll));
        //                        }
        //                    }
        //                    catch { /* تجاهل الملفات المعطوبة */ }
        //                }
        //            }
        //        }
        //        else
        //        {
        //            System.Diagnostics.Debug.WriteLine("❌ لم أجد مجلد .Desktop لهذا المشروع.");
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        System.Diagnostics.Debug.WriteLine($"🔴 Error in Path Traversal: {ex.Message}");
        //    }
        //}

        //// دالة مساعدة لجلب أحدث مجلد bin (Debug/net8.0 مثلاً)
        //private static string FindLatestBin(string projectPath)
        //{
        //    string binRoot = Path.Combine(projectPath, "bin");
        //    if (!Directory.Exists(binRoot)) return null;

        //    // بنجيب أحدث ملف DLL تم إنتاجه عشان نضمن إننا في الفولدر الصح (Debug أو Release)
        //    var latestDll = new DirectoryInfo(binRoot)
        //        .GetFiles("*.dll", SearchOption.AllDirectories)
        //        .OrderByDescending(f => f.LastWriteTime)
        //        .FirstOrDefault();

        //    return latestDll?.DirectoryName;
        //}

        //private static void LoadProjectAssemblies(string filePath)
        //{
        //    try
        //    {
        //        string dir = Path.GetDirectoryName(filePath);
        //        while (!string.IsNullOrEmpty(dir))
        //        {
        //            string binDir = Path.Combine(dir, "bin");
        //            if (Directory.Exists(binDir))
        //            {
        //                // بنجيب كل الـ DLLs في الـ bin وكل الفولدرات اللي جواه (Debug/net8.0)
        //                var dllFiles = Directory.GetFiles(binDir, "*.dll", SearchOption.AllDirectories);
        //                foreach (var dll in dllFiles)
        //                {
        //                    try
        //                    {
        //                        var name = AssemblyName.GetAssemblyName(dll).Name;
        //                        if (!_loadedAssemblies.ContainsKey(name))
        //                        {
        //                            // تحميل المكتبة كـ Bytes وحفظها في القاموس
        //                            var asm = Assembly.Load(File.ReadAllBytes(dll));
        //                            _loadedAssemblies[name] = asm;
        //                        }
        //                    }
        //                    catch { }
        //                }
        //                break;
        //            }
        //            dir = Directory.GetParent(dir)?.FullName;
        //        }
        //    }
        //    catch { }
        //}
    }
}

