using Dock.Model.Controls;
using Dock.Model.Core;
using Dock.Model.Mvvm;
using Dock.Model.Mvvm.Controls;
using System;
using System.Collections.Generic;
using VisualEditorApp.Models.Tools;
using VisualEditorApp.ViewModels;

namespace VisualEditorApp.Models
{
    public class WorkspaceDocument : Document
    {
    }

    public class EditorDockFactory : Factory
    {
        private readonly MainWindowViewModel _vm;

        public EditorDockFactory(MainWindowViewModel vm)
        {
            _vm = vm;
        }

        public override IRootDock CreateLayout()
        {
            // 1. استخدام الأدوات من الـ ViewModel مباشرة لضمان "وحدة المصدر"
            var toolboxTool = _vm.Toolbox;
            var propertiesTool = _vm.Properties;
            var documentOutline = DocumentOutlineTool.Instance;

            // 2. تعيين الـ Context لكل أداة (هذا هو السر الذي يجعل الـ DataBinding يشتغل)
            toolboxTool.Context = _vm;
            propertiesTool.Context = _vm;
            documentOutline.Context = _vm;

            // مساحة العمل (مستند)
            var workspaceDoc = new WorkspaceDocument
            {
                Id = "Workspace",
                Title = "Workspace",
                Context = _vm // ربط مساحة العمل بالـ ViewModel الرئيسي
            };

            // 3. إنشاء الحاويات (Docks)
            var leftDock = new ToolDock
            {
                Id = "LeftDock",
                Proportion = 0.2,
                ActiveDockable = toolboxTool,
                VisibleDockables = CreateList<IDockable>(toolboxTool)
            };

            var centerDock = new DocumentDock
            {
                Id = "CenterDock",
                Proportion = 0.6,
                ActiveDockable = workspaceDoc,
                VisibleDockables = CreateList<IDockable>(workspaceDoc)
            };

            var rightDock = new ToolDock
            {
                Id = "RightDock",
                Proportion = 0.2,
                ActiveDockable = documentOutline,
                // نضع الشجرة والخصائص معاً في القائمة اليمنى
                VisibleDockables = CreateList<IDockable>(documentOutline, propertiesTool)
            };

            // 4. تجميع الهيكل النهائي
            var mainLayout = new ProportionalDock
            {
                Id = "MainLayout",
                Orientation = Orientation.Horizontal,
                VisibleDockables = CreateList<IDockable>(
                    leftDock,
                    new ProportionalDockSplitter(),
                    centerDock,
                    new ProportionalDockSplitter(),
                    rightDock
                )
            };

            var root = CreateRootDock();
            root.Id = "Root";
            root.ActiveDockable = mainLayout;
            root.DefaultDockable = mainLayout;
            root.VisibleDockables = CreateList<IDockable>(mainLayout);

            return root;
        }

        public override void InitLayout(IDockable layout)
        {
            // إعداد محددات السياق لضمان عمل نظام الـ Docking بسلاسة عند السحب والإفلات
            this.ContextLocator = new Dictionary<string, Func<object?>>
            {
                ["Root"] = () => _vm,
                ["MainLayout"] = () => _vm,
                ["LeftDock"] = () => _vm,
                ["RightDock"] = () => _vm,
                ["CenterDock"] = () => _vm,
                ["Workspace"] = () => _vm,
                ["DocumentOutline"] = () => _vm.DocumentOutline,
                ["Properties"] = () => _vm.Properties,
                ["Toolbox"] = () => _vm.Toolbox
            };

            base.InitLayout(layout);
        }
    }
}