using System;
using VisualEditorApp.Models;
using VisualEditor.Core.Models;

namespace VisualEditorApp.Services
{
    public class WorkspaceService
    {
        // 1. نسخة واحدة بس من المدير للبرنامج كله (Singleton)
        public static WorkspaceService Instance { get; } = new WorkspaceService();

        // 2. المسار الحالي للمشروع المفتوح
        public string CurrentWorkspacePath { get; private set; } = string.Empty;

        // 3. الإشارة المباشرة اللي السوليوشن اكسبلورر هيسمعها
        public event EventHandler<string>? WorkspaceLoaded;

        // 🎯 إشارة تغيير مشروع التشغيل (Startup)
        public event EventHandler<SolutionItemViewModel>? StartupProjectChanged;

        // 🎯 إشارة جديدة: للانتقال لسطر معين في ملف (مهمة للـ Error List)
        public event EventHandler<(string Path, int Line)>? NavigationRequested;

        public event EventHandler<(string Path, int Line)>? ErrorNavigationRequested; // للذهاب لسطر الخطأ

        // منغلق عشان محدش يقدر يعمل منه نسخ تانية بالغلط
        private WorkspaceService() { }

        // 4. الدالة اللي هنناديها لما المشروع يتكريت
        public void LoadWorkspace(string path)
        {
            CurrentWorkspacePath = path;

            // بنبلغ أي حد مهتم (زي السوليوشن اكسبلورر) إن في مشروع جديد اتفتح
            WorkspaceLoaded?.Invoke(this, path);
        }

        // 🎯 مرجع للمشروع الأخضر (Startup) الحالي
        public SolutionItemViewModel? CurrentStartupProject { get; private set; }

        public void SetCurrentStartupProject(SolutionItemViewModel project)
        {
            // إطفاء القديم
            if (CurrentStartupProject != null)
                CurrentStartupProject.IsStartupProject = false;

            // تفعيل الجديد
            CurrentStartupProject = project;
            CurrentStartupProject.IsStartupProject = true;

            // 💡 إطلاق الإشارة عشان أي جزء في البرنامج (زي التولبار) يعرف إن الـ Startup اتغير
            StartupProjectChanged?.Invoke(this, project);
        }

        // 🎯 دالة جديدة: لفتح ملف عند سطر معين (لما تدوس دبل كليك على خطأ)
        public void NavigateToFile(string filePath, int line)
        {
            NavigationRequested?.Invoke(this, (filePath, line));
        }

        // --- 🎯 السحر هنا: الانتقال للخطأ ---
        public void GoToError(DiagnosticItem error)
        {
            if (string.IsNullOrEmpty(error.File)) return;

            // إرسال إشارة للمحرر (Editor) عشان يفتح الملف ويقف عند السطر
            ErrorNavigationRequested?.Invoke(this, (error.File, error.Line));
        }
    }
}