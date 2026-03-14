using System;
using VisualEditorApp.Models;

namespace VisualEditorApp.Services
{
    public class WorkspaceService
    {
        // 1. نسخة واحدة بس من المدير للبرنامج كله (Singleton)
        public static WorkspaceService Instance { get; } = new WorkspaceService();

        // 2. المسار الحالي للمشروع المفتوح
        public string CurrentWorkspacePath { get; private set; }

        // 3. الإشارة المباشرة اللي السوليوشن اكسبلورر هيسمعها
        public event EventHandler<string> WorkspaceLoaded;

        // منغلق عشان محدش يقدر يعمل منه نسخ تانية بالغلط
        private WorkspaceService() { }

        // 4. الدالة اللي هنناديها لما المشروع يتكريت
        public void LoadWorkspace(string path)
        {
            CurrentWorkspacePath = path;

            // بنبلغ أي حد مهتم (زي السوليوشن اكسبلورر) إن في مشروع جديد اتفتح
            WorkspaceLoaded?.Invoke(this, path);
        }

        
        public void SetCurrentStartupProject(SolutionItemViewModel newStartup)
        {
            // 1. لو في مشروع كان Startup قبل كده، بنطفيه (IsStartupProject = false)
            if (_currentStartup != null)
            {
                _currentStartup.IsStartupProject = false;
            }

            // 2. بنفعل المشروع الجديد
            _currentStartup = newStartup;
            _currentStartup.IsStartupProject = true;

            // 3. (اختياري) لو عاوز تبلغ أجزاء تانية في البرنامج
            StartupProjectChanged?.Invoke(this, newStartup);
        }

        private SolutionItemViewModel? _currentStartup;
        public event EventHandler<SolutionItemViewModel>? StartupProjectChanged;
    }
}
