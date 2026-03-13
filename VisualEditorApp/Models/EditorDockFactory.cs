using Dock.Model.Controls;
using Dock.Model.Core;
using Dock.Model.Mvvm;
using Dock.Model.Mvvm.Controls;
using System;
using System.Collections.Generic;
using VisualEditor.Toolbox.Outline;
using VisualEditorApp.Models.Tools;
using VisualEditorApp.ViewModels;

namespace VisualEditorApp.Models
{

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
            var solutionExplorerTool= SolutionExplorerTool.Instance;
            var documentOutline = DocumentOutlineTool.Instance;

            // 2. تعيين الـ Context لكل أداة (هذا هو السر الذي يجعل الـ DataBinding يشتغل)
            toolboxTool.Context = _vm;
            propertiesTool.Context = _vm;
            documentOutline.Context = _vm;
            solutionExplorerTool.Context = _vm;

            // مساحة العمل (مستند)
            var workspaceDoc = new WorkspaceDocument
            {
                Id = "Workspace",
                Title = "Workspace",
                Context = _vm // ربط مساحة العمل بالـ ViewModel الرئيسي
            };

            // 3. إنشاء الحاويات (Docks)
            var leftDock = new ProportionalDock
            {

                Id = "LeftDock",
                Orientation = Orientation.Vertical,
                VisibleDockables = CreateList<IDockable>(
             new ToolDock
             {
                 ActiveDockable = documentOutline,
                 VisibleDockables = CreateList<IDockable>(solutionExplorerTool),
                 Proportion = 0.5 // نص المساحة للي فوق
             },
             new ProportionalDockSplitter(),
             new ToolDock
             {
                 ActiveDockable = propertiesTool,
                 VisibleDockables = CreateList<IDockable>(toolboxTool),
                 Proportion = 0.5 // نص المساحة للي تحت
             }
             )
 
            };

            var centerDock = new DocumentDock
            {
                Id = "CenterDock",
                Proportion = 0.6,
                ActiveDockable = workspaceDoc,
                VisibleDockables = CreateList<IDockable>(workspaceDoc)
            };

            var rightPane = new ProportionalDock
            {
                Id = "RightPane",
                Orientation = Orientation.Vertical,
                VisibleDockables = CreateList<IDockable>(
             new ToolDock
             {
                 ActiveDockable = documentOutline,
                 VisibleDockables = CreateList<IDockable>(documentOutline),
                 Proportion = 0.5 // نص المساحة للي فوق
             },
             new ProportionalDockSplitter(),
             new ToolDock
             {
                 ActiveDockable = propertiesTool,
                 VisibleDockables = CreateList<IDockable>(propertiesTool),
                 Proportion = 0.5 // نص المساحة للي تحت
             }
             )
            };

            var mainLayout = new ProportionalDock
            {
                Id = "MainLayout",
                Orientation = Orientation.Horizontal,
                VisibleDockables = CreateList<IDockable>(
                    leftDock,
                    new ProportionalDockSplitter(),
                    centerDock,
                    new ProportionalDockSplitter(),
                    rightPane // الجانب اليمين قطعة واحدة بتابات
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