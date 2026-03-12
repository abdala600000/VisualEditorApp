using Dock.Model.Controls;
using Dock.Model.Core;
using Dock.Model.Mvvm;
using Dock.Model.Mvvm.Controls;
using System;
using System.Collections.Generic;
using System.Text;
using VisualEditorApp.Models.Tools;

namespace VisualEditorApp.Models
{
    // أضفنا هذا الكلاس الصغير لتمثيل مستند مساحة العمل
    public class WorkspaceDocument : Document
    {
    }

    public class EditorDockFactory : Factory
    {
        public override IRootDock CreateLayout()
        {
            // 1. إنشاء المحتوى الداخلي (الأدوات ومساحة العمل)
            var toolboxTool = new ToolboxTool { Id = "Toolbox", Title = "Toolbox" };
            var workspaceDoc = new WorkspaceDocument { Id = "Workspace", Title = "Workspace" };
            // إنشاء أداة الخصائص
            var propertiesTool = new PropertiesTool { Id = "Properties", Title = "Properties" };

            // 2. إنشاء الحاويات (Docks) ووضع المحتوى بداخلها كـ Active (هذا ما كان ينقصنا لكي تظهر)
            var leftDock = new ToolDock
            {
                Id = "LeftDock",
                Proportion = 0.2, // تأخذ 20% من مساحة الشاشة
                ActiveDockable = toolboxTool,
                VisibleDockables = CreateList<IDockable>(toolboxTool)
            };

            var centerDock = new DocumentDock
            {
                Id = "CenterDock",
                Proportion = 0.6, // تأخذ 60% من مساحة الشاشة
                ActiveDockable = workspaceDoc,
                VisibleDockables = CreateList<IDockable>(workspaceDoc)
            };

            // وضعها في الحاوية اليمنى وتفعيلها
            var rightDock = new ToolDock
            {
                Id = "RightDock",
                Proportion = 0.2, // تأخذ 20% من الشاشة
                ActiveDockable = propertiesTool,
                VisibleDockables = CreateList<IDockable>(propertiesTool)
            };

            // 3. تجميع الحاويات في هيكل نسبي مقسم
            var mainLayout = new ProportionalDock
            {
                Id = "MainLayout",
                Orientation = Orientation.Horizontal,
                VisibleDockables = CreateList<IDockable>(
                    leftDock,
                    new ProportionalDockSplitter(), // الفاصل القابل للسحب
                    centerDock,
                    new ProportionalDockSplitter(), // الفاصل القابل للسحب
                    rightDock
                )
            };

            // 4. تعيين الجذر الأساسي
            var root = CreateRootDock();
            root.Id = "Root";
            root.ActiveDockable = mainLayout;
            root.DefaultDockable = mainLayout;
            root.VisibleDockables = CreateList<IDockable>(mainLayout);

            return root;
        }

        public override void InitLayout(IDockable layout)
        {
            ContextLocator = new Dictionary<string, Func<object?>>();
            base.InitLayout(layout);
        }
    }
}
