# VisualEditorApp

A cross-platform **XAML Visual Designer IDE** built with [Avalonia UI](https://avaloniaui.net/) and .NET 10. It provides a drag-and-drop design surface, live XAML preview, code editing, solution management, and a full IDE-like docking layout — all in one desktop application.

---

## Screenshots

> *(Add screenshots here)*

---

## Features

- **Live XAML Designer** — drag controls onto a canvas and see them rendered in real time
- **XAML Code Editor** — syntax-aware editor with live sync between design and code
- **Solution Explorer** — open `.sln` / `.slnx` solutions and browse projects and files
- **Undo / Redo** — full history for move, resize, rotate, skew, delete, and paste operations (capped at 50 steps)
- **Multi-Selection** — rubber-band selection and group drag/move
- **Rotation & Skew** — interactive handles with snapping (15° for rotate, 5° for skew)
- **Smart Rulers & Zoom** — pan-and-zoom canvas with live rulers
- **Properties Panel** — reflects selected control properties dynamically via reflection
- **Document Outline** — tree view of the current design's element hierarchy
- **Error List / Problems Panel** — build diagnostics with severity, file, and line info
- **Toolbox** — drag Avalonia controls from the toolbox onto the design surface
- **Build & Run** — build/rebuild/clean startup project, run with F5, cancel in-progress builds
- **Dark / Light Theme** — switchable Rider-style themes
- **New Project Wizard** — create new Avalonia projects from templates
- **Recent Projects** — quick access to recently opened solutions

---

## Architecture

The solution is split into 5 focused class libraries + 1 main app:

```
VisualEditorApp.slnx
│
├── VisualEditorApp              ← Main application (entry point, shell, ViewModels)
├── VisualEditor.Core            ← Shared engine (XAML parsing, patching, live compiler)
├── VisualEditor.Designer        ← Design surface (canvas, adorners, handles, history)
├── VisualEditor.CodeEditor      ← AXAML text editor (AvaloniaEdit integration)
└── VisualEditor.Toolbo          ← Toolbox, Document Outline, Properties panel
```

---

## Project Breakdown

### `VisualEditorApp` — Shell & Orchestration

The main executable. Hosts the docking layout and wires everything together.

| Path | Purpose |
|------|---------|
| `Program.cs` | App entry point |
| `App.axaml.cs` | Avalonia app bootstrap |
| `Views/MainWindow.axaml` | Top-level window |
| `Views/MainView.axaml` | Main layout: menu bar, toolbar, dock area, status bar |
| `ViewModels/MainWindowViewModel.cs` | Central ViewModel — build commands, solution loading, theme switching |
| `ViewModels/DockFactory.cs` | Creates and manages the dockable tool/document layout |
| `Views/Documents/WorkspaceView.axaml.cs` | Hosts the designer surface + code editor side by side |
| `Views/Documents/EditorDocumentView.axaml` | Pure code editor document tab |
| `Views/Tools/SolutionExplorerView.axaml` | File tree for the loaded solution |
| `Views/Tools/PropertiesToolView.axaml.cs` | Displays selected control properties via reflection |
| `Views/Tools/ErrorListToolView.axaml` | Build errors/warnings list |
| `Views/Tools/ToolboxView.axaml` | Avalonia control palette |
| `Views/Tools/TerminalToolView.axaml` | Embedded terminal panel |
| `Services/SolutionLoader.cs` | Loads `.sln`/`.slnx` using Roslyn |
| `Services/WorkspaceService.cs` | Singleton tracking the active workspace and startup project |
| `Services/RecentProjectsService.cs` | Persists recently opened solutions |
| `Models/SolutionItemViewModel.cs` | Represents a project node with Build/Rebuild/Clean commands |

---

### `VisualEditor.Core` — XAML Engine

The brain of the designer. Handles all XAML manipulation without touching the UI.

| File | Purpose |
|------|---------|
| `LiveDesignerCompiler.cs` | Loads user project DLLs and renders XAML to live Avalonia controls using `AvaloniaSourceGeneratedXamlLoader`. Accepts `CancellationToken` |
| `XamlDOMPatcher.cs` | Surgically patches XAML strings — moves elements, updates properties, writes `RenderTransform` as proper property elements (`<Control.RenderTransform>`) |
| `XamlGenerator.cs` | Generates XAML markup from a live control tree, including `Background`, `Foreground`, `FontSize`, and `RenderTransform` |
| `XamlSanitizer.cs` | Cleans XAML before rendering — strips event handlers, preserves `TransformGroup`/`RotateTransform`/`SkewTransform` property elements |
| `Messages/MessageBus.cs` | Simple static event bus for cross-component communication |
| `Messages/ControlSelectedMessage.cs` | Fired when the user selects a control on the canvas |
| `Messages/DesignChangedMessage.cs` | Fired when the design surface changes |
| `Messages/BuildFinishedMessage.cs` | Fired when a build completes with diagnostics |
| `Models/AppSettings.cs` | Persisted settings (last opened solution, startup bin path) |
| `Services/SettingsService.cs` | Load/save `AppSettings` to disk |

---

### `VisualEditor.Designer` — Design Surface

The interactive canvas where users visually design their UI.

| File | Purpose |
|------|---------|
| `DesignerSurfaceView.axaml.cs` | Core designer — 11 regions covering selection, dragging, resizing, rotation, skew, zoom, rulers, drag-and-drop, context menu, clipboard, and undo/redo |
| `DesignerSurfaceView.axaml` | XAML layout: ZoomBorder, Canvas, AdornerCanvas, DropLayer, rulers, SelectionAdorner with resize/rotate/skew handles |
| `Services/HistoryService.cs` | Undo/Redo stack using `LinkedList<Action>`, capped at 50 entries |

**Designer regions:**

1. Fields & state
2. Constructor & event wiring
3. Selection logic (single + multi)
4. Mouse drag & multi-selection (rubber band)
5. Rulers & zoom
6. Public API & keyboard shortcuts
7. Resize logic (8-handle resizing with rotation compensation)
8. Rotation logic (WpfDesigner-style angle-between-vectors)
8b. Skew logic (SkewX / SkewY handles)
9. Zoom & navigation + Drag-and-drop
10. Context menu (Bring to Front, Send to Back, Duplicate, Delete, Copy, Paste)
11. Clipboard operations (Copy, Paste, Clone)

---

### `VisualEditor.CodeEditor` — AXAML Text Editor

| File | Purpose |
|------|---------|
| `SmartEditorView.axaml.cs` | Text editor view wrapping AvaloniaEdit with AXAML syntax support |
| `AxamlTextEditor.cs` | Custom editor control with XAML-aware features |

---

### `VisualEditor.Toolbo` — Toolbox & Panels

| Path | Purpose |
|------|---------|
| `Controls/ToolboxView.axaml.cs` | Palette of draggable Avalonia controls |
| `Controls/ToolboxDragBehavior.cs` | Implements drag-and-drop from toolbox to designer |
| `Controls/ToolboxItem.cs` | Represents a single draggable control type |
| `Outline/DocumentOutlineView.axaml.cs` | Tree view showing the element hierarchy of the current design |
| `Outline/DocumentOutlineViewModel.cs` | Singleton ViewModel for the outline tree |
| `Outline/ElementNode.cs` | Tree node model |
| `Prop/PropertiesView.axaml.cs` | Property editor panel |
| `Prop/PropertyItem.cs` | Single property row model |
| `Prop/PropertyGroup.cs` | Grouped property section |

---

## How It Works — End to End

```
User opens a .sln file
        │
        ▼
SolutionLoader (Roslyn) parses the solution
        │
        ▼
DockFactory creates document tabs for .axaml files
        │
        ▼
WorkspaceView hosts:
  ├── DesignerSurfaceView  ←── LiveDesignerCompiler renders XAML → live Control
  └── SmartEditorView      ←── AvaloniaEdit shows raw XAML text
        │
        ▼
User drags/edits controls
        │
        ├── XamlDOMPatcher patches the XAML string (position, size, transform)
        ├── HistoryService records undo/redo actions
        └── MessageBus broadcasts DesignChanged → CodeEditor updates
```

---

## Key Design Decisions

- **No code generation at design time** — the designer works directly with live Avalonia control instances, not a simulated model
- **XAML as source of truth** — all changes are immediately reflected back into the XAML string via `XamlDOMPatcher`
- **RenderTransform as property elements** — `RotateTransform`, `SkewTransform`, and `TransformGroup` are written as proper XAML property elements, not attributes
- **Undo/Redo via closures** — history entries are pairs of `Action` lambdas (undo/redo), keeping the history service generic and simple
- **Cancellable builds** — all build operations use `CancellationTokenSource` and expose a Cancel button in the toolbar

---

## Tech Stack

| Technology | Usage |
|-----------|-------|
| [Avalonia UI](https://avaloniaui.net/) | Cross-platform UI framework |
| .NET 10 | Runtime & language (C# 13) |
| [CommunityToolkit.Mvvm](https://learn.microsoft.com/en-us/dotnet/communitytoolkit/mvvm/) | `ObservableObject`, `RelayCommand`, source generators |
| [Dock](https://github.com/wieslawsoltes/Dock) | Dockable layout system (tool panels + document tabs) |
| [AvaloniaEdit](https://github.com/AvaloniaUI/AvaloniaEdit) | Code editor component |
| [Microsoft.CodeAnalysis (Roslyn)](https://github.com/dotnet/roslyn) | Solution/project parsing |
| XamlToCSharpGenerator.Runtime | Live XAML rendering engine |

---

## Getting Started

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- Windows / Linux / macOS

### Build & Run

```bash
git clone https://github.com/abdala600000/VisualEditorApp.git
cd VisualEditorApp
dotnet run --project VisualEditorApp
```

### Open a Solution

1. Launch the app
2. **File → Open Solution** and select a `.sln` or `.slnx` file
3. The designer opens the first `.axaml` file automatically

### Design a UI

1. Drag controls from the **Toolbox** onto the canvas
2. Resize using the 8 corner/edge handles
3. Rotate using the top handle
4. Skew using the side ellipse handles
5. Edit properties in the **Properties** panel
6. Switch to the **Code** tab to edit XAML directly

---

## License

MIT
