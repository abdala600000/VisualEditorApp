using Avalonia.Controls;
using AvaloniaEdit.Highlighting;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using XamlToCSharpGenerator.Editor.Avalonia;
using XamlToCSharpGenerator.LanguageService;
using XamlToCSharpGenerator.LanguageService.Models;
using XamlToCSharpGenerator.LanguageService.Workspace;

namespace VisualEditor.CodeEditor
{
    public partial class SmartEditorView : UserControl
    {
        private AxamlTextEditor _editor;
        private XamlLanguageServiceEngine _engine;
        private bool _isInternalUpdate = false;

        // 👈 الحدث (Event) السحري اللي هنكلم بيه البرنامج بره
        public event EventHandler<string> XamlTextChanged;

        public SmartEditorView()
        {
            InitializeComponent();
            InitializeSmartEditor();
        }

        private void InitializeSmartEditor()
        {
            var references = AppDomain.CurrentDomain.GetAssemblies()
                .Where(a => !a.IsDynamic && !string.IsNullOrWhiteSpace(a.Location))
                .Select(a => MetadataReference.CreateFromFile(a.Location))
                .Cast<MetadataReference>()
                .ToList();

            var compilation = CSharpCompilation.Create(
                "VisualEditorLiveCompilation",
                references: references,
                options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
            );

            var provider = new InMemoryCompilationProvider(compilation);
            _engine = new XamlLanguageServiceEngine(provider);

            _editor = new AxamlTextEditor(_engine)
            {
                DocumentUri = "file:///LiveDesigner.axaml",
                WorkspaceRoot = AppContext.BaseDirectory,
                SyntaxHighlighting = HighlightingManager.Instance.GetDefinition("XML"),
                Margin = new Avalonia.Thickness(5),
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Stretch,
                VerticalAlignment = Avalonia.Layout.VerticalAlignment.Stretch
            };

            _editor.TextChanged += OnEditorTextChanged;
            EditorContainer.Content = _editor;
        }

        // دالة عشان الشاشة الرئيسية تبعت كود للمحرر (زي وقت الـ Drag & Drop)
        public void SetXamlText(string xml)
        {
            _isInternalUpdate = true;
            _editor.Text = xml;
            _isInternalUpdate = false;
        }

        // إرسال الكود الجديد للخارج عندما يكتب المستخدم
        private void OnEditorTextChanged(object? sender, EventArgs e)
        {
            if (!_isInternalUpdate && !string.IsNullOrWhiteSpace(_editor.Text))
            {
                // إطلاق الحدث وإرسال النص الجديد للشاشة الرئيسية
                XamlTextChanged?.Invoke(this, _editor.Text);
            }
        }
    }

    // =========================================================
    // كلاس الخداع السحري (مخفي داخل المكتبة ولا يراه أحد بالخارج)
    // =========================================================
    internal sealed class InMemoryCompilationProvider : ICompilationProvider
    {
        private readonly Compilation _compilation;

        public InMemoryCompilationProvider(Compilation compilation)
        {
            _compilation = compilation;
        }

        public Task<CompilationSnapshot> GetCompilationAsync(
            string filePath,
            string? workspaceRoot,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(new CompilationSnapshot(
                ProjectPath: workspaceRoot,
                Project: null,
                Compilation: _compilation,
                Diagnostics: ImmutableArray<LanguageServiceDiagnostic>.Empty));
        }

        public void Invalidate(string filePath)
        {
        }

        public void Dispose()
        {
        }
    }
}