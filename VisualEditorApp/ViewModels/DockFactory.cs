using System;
using System.Collections.Generic;
using System.Linq;
using Dock.Avalonia.Controls;
using Dock.Model.Controls;
using Dock.Model.Core;
using Dock.Model.Mvvm;
using Dock.Model.Mvvm.Controls;
using VisualEditorApp.ViewModels.Documents;
using VisualEditorApp.ViewModels.Tools;

namespace VisualEditorApp.ViewModels
{
    public partial class DockFactory : Factory
    {
        private IDocumentDock? _documentDock;
        private IRootDock? _rootDock;

        public DockFactory(Action<string> openDocument)
        {
            SolutionExplorer = new SolutionExplorerViewModel(openDocument);
            StructureTool = new StructureToolViewModel();
            PropertiesTool = new PropertiesToolViewModel();
            ProblemsTool = new ProblemsToolViewModel();
            TerminalTool = new TerminalToolViewModel();
        }

        public SolutionExplorerViewModel SolutionExplorer { get; }
        public StructureToolViewModel StructureTool { get; }
        public PropertiesToolViewModel PropertiesTool { get; }
        public ProblemsToolViewModel ProblemsTool { get; }
        public TerminalToolViewModel TerminalTool { get; }
        public IDocumentDock? DocumentDock => _documentDock;

        public override IRootDock CreateLayout()
        {
            var documentDock = new DocumentDock
            {
                Id = "Documents",
                Title = "Documents",
                IsCollapsable = false,
                CanCreateDocument = false,
                ActiveDockable = null,
                VisibleDockables = CreateList<IDockable>()
            };

            var leftDock = new ToolDock
            {
                Id = "LeftDock",
                Proportion = 0.22,
                Alignment = Alignment.Left,
                GripMode = GripMode.Visible,
                ActiveDockable = SolutionExplorer,
                VisibleDockables = CreateList<IDockable>(SolutionExplorer, StructureTool)
            };

            var rightDock = new ToolDock
            {
                Id = "RightDock",
                Proportion = 0.2,
                Alignment = Alignment.Right,
                GripMode = GripMode.Visible,
                ActiveDockable = PropertiesTool,
                VisibleDockables = CreateList<IDockable>(PropertiesTool)
            };

            var bottomDock = new ToolDock
            {
                Id = "BottomDock",
                Proportion = 0.25,
                Alignment = Alignment.Bottom,
                GripMode = GripMode.Visible,
                ActiveDockable = ProblemsTool,
                VisibleDockables = CreateList<IDockable>(ProblemsTool, TerminalTool)
            };

            var mainHorizontal = new ProportionalDock
            {
                Orientation = Orientation.Horizontal,
                IsCollapsable = false,
                VisibleDockables = CreateList<IDockable>
                (
                    leftDock,
                    new ProportionalDockSplitter(),
                    documentDock,
                    new ProportionalDockSplitter(),
                    rightDock
                )
            };

            var mainLayout = new ProportionalDock
            {
                Orientation = Orientation.Vertical,
                IsCollapsable = false,
                VisibleDockables = CreateList<IDockable>
                (
                    mainHorizontal,
                    new ProportionalDockSplitter(),
                    bottomDock
                )
            };

            var rootDock = CreateRootDock();
            rootDock.IsCollapsable = false;
            rootDock.VisibleDockables = CreateList<IDockable>(mainLayout);
            rootDock.ActiveDockable = mainLayout;
            rootDock.DefaultDockable = mainLayout;

            _documentDock = documentDock;
            _rootDock = rootDock;

            return rootDock;
        }

        public void OpenDocument(string path)
        {
            if (_documentDock is null) return;

            var existing = _documentDock.VisibleDockables?
                .OfType<EditorDocumentViewModel>()
                .FirstOrDefault(doc => string.Equals(doc.FilePath, path, StringComparison.OrdinalIgnoreCase));

            if (existing is not null)
            {
                SetActiveDockable(existing);
                SetFocusedDockable(_documentDock, existing);
                return;
            }

            var document = EditorDocumentViewModel.LoadFromFile(path);
            document.Id = path;
            _documentDock.VisibleDockables ??= CreateList<IDockable>();
            AddDockable(_documentDock, document);
            SetActiveDockable(document);
            SetFocusedDockable(_documentDock, document);
        }

        public void CloseAllDocuments()
        {
            if (_documentDock?.VisibleDockables is null) return;
            var documents = _documentDock.VisibleDockables.OfType<EditorDocumentViewModel>().ToList();
            foreach (var document in documents)
            {
                RemoveDockable(document, true);
            }
        }

        public override void InitLayout(IDockable layout)
        {
            ContextLocator = new Dictionary<string, Func<object?>>
            {
                [nameof(IRootDock)] = () => _rootDock,
                [nameof(IDocumentDock)] = () => _documentDock,
                ["SolutionExplorer"] = () => SolutionExplorer,
                ["Structure"] = () => StructureTool,
                ["Properties"] = () => PropertiesTool,
                ["Problems"] = () => ProblemsTool,
                ["Terminal"] = () => TerminalTool
            };

            HostWindowLocator = new Dictionary<string, Func<IHostWindow?>>
            {
                [nameof(IDockWindow)] = () => new HostWindow()
            };

            base.InitLayout(layout);
        }
    }
}
