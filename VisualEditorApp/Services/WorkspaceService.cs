using System;

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
    }
}
