using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Threading;
using AvaloniaEdit;
using AvaloniaEdit.CodeCompletion;
using AvaloniaEdit.Document;
using AvaloniaEdit.Editing;
using AvaloniaEdit.Rendering;
using System;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using XamlToCSharpGenerator.LanguageService;
using XamlToCSharpGenerator.LanguageService.Models;

namespace VisualEditor.CodeEditor; 

public sealed class AxamlTextEditor : TextEditor
{

    // 👈 الخاصية السحرية اللي بتجبر أفلونيا تلبسه الستايل الأصلي تلقائياً
    protected override Type StyleKeyOverride => typeof(AvaloniaEdit.TextEditor);

    public static readonly StyledProperty<string?> DocumentUriProperty =
        AvaloniaProperty.Register<AxamlTextEditor, string?>(nameof(DocumentUri));

    public static readonly StyledProperty<string?> WorkspaceRootProperty =
        AvaloniaProperty.Register<AxamlTextEditor, string?>(nameof(WorkspaceRoot));

    public static readonly DirectProperty<AxamlTextEditor, ImmutableArray<LanguageServiceDiagnostic>> DiagnosticsProperty =
        AvaloniaProperty.RegisterDirect<AxamlTextEditor, ImmutableArray<LanguageServiceDiagnostic>>(
            nameof(Diagnostics),
            editor => editor.Diagnostics);

    private readonly XamlLanguageServiceEngine _engine;
    private readonly AxamlDiagnosticColorizer _diagnosticColorizer;
    private CompletionWindow? _completionWindow;
    private DispatcherTimer? _analysisDebounce;
    private bool _documentOpened;
    private int _documentVersion;
    private ImmutableArray<LanguageServiceDiagnostic> _diagnostics = ImmutableArray<LanguageServiceDiagnostic>.Empty;
    private CancellationTokenSource? _analysisCts;

    public AxamlTextEditor()
        : this(new XamlLanguageServiceEngine())
    {
    }

    public AxamlTextEditor(XamlLanguageServiceEngine engine)
    {
        _engine = engine ?? throw new ArgumentNullException(nameof(engine));
        _diagnosticColorizer = new AxamlDiagnosticColorizer();

        ShowLineNumbers = true;
        Options.EnableHyperlinks = false;

        TextChanged += OnEditorTextChanged;
        AddHandler(KeyDownEvent, OnEditorKeyDown, RoutingStrategies.Tunnel);

        TextArea.TextEntered += OnTextEntered;
        TextArea.TextEntering += OnTextEntering;
        TextArea.TextView.LineTransformers.Add(_diagnosticColorizer);

        _analysisDebounce = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(120)
        };
        _analysisDebounce.Tick += OnAnalysisDebounceTick;

    }

    public string? DocumentUri
    {
        get => GetValue(DocumentUriProperty);
        set => SetValue(DocumentUriProperty, value);
    }

    public string? WorkspaceRoot
    {
        get => GetValue(WorkspaceRootProperty);
        set => SetValue(WorkspaceRootProperty, value);
    }

    public ImmutableArray<LanguageServiceDiagnostic> Diagnostics
    {
        get => _diagnostics;
        private set
        {
            SetAndRaise(DiagnosticsProperty, ref _diagnostics, value);
            _diagnosticColorizer.UpdateDiagnostics(value);
            TextArea.TextView.InvalidateVisual();
        }
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        _ = EnsureDocumentOpenAndAnalyzeAsync();
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == DocumentUriProperty)
        {
            _ = EnsureDocumentOpenAndAnalyzeAsync();
            return;
        }

        if (change.Property == WorkspaceRootProperty && _documentOpened)
        {
            _ = AnalyzeNowAsync();
        }
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        _analysisCts?.Cancel();
        _analysisCts?.Dispose();
        _analysisCts = null;

        if (!string.IsNullOrWhiteSpace(DocumentUri))
        {
            _engine.CloseDocument(DocumentUri!);
        }

        _documentOpened = false;
        _completionWindow?.Close();
        _completionWindow = null;
    }

    private void OnEditorTextChanged(object? sender, EventArgs e)
    {
        _analysisDebounce?.Stop();
        _analysisDebounce?.Start();
    }

    private void OnAnalysisDebounceTick(object? sender, EventArgs e)
    {
        _analysisDebounce?.Stop();
        _ = AnalyzeNowAsync();
    }

    private async Task EnsureDocumentOpenAndAnalyzeAsync()
    {
        if (string.IsNullOrWhiteSpace(DocumentUri))
        {
            return;
        }

        _analysisCts?.Cancel();
        _analysisCts?.Dispose();
        _analysisCts = new CancellationTokenSource();

        if (!_documentOpened)
        {
            _documentVersion = 1;
            var diagnostics = await _engine.OpenDocumentAsync(
                DocumentUri!,
                Text ?? string.Empty,
                version: _documentVersion,
                CreateOptions(),
                _analysisCts.Token).ConfigureAwait(false);
            await Dispatcher.UIThread.InvokeAsync(() => Diagnostics = diagnostics);
            _documentOpened = true;
            return;
        }

        await AnalyzeNowAsync().ConfigureAwait(false);
    }

    private async Task AnalyzeNowAsync()
    {
        if (string.IsNullOrWhiteSpace(DocumentUri))
        {
            return;
        }

        _analysisCts?.Cancel();
        _analysisCts?.Dispose();
        _analysisCts = new CancellationTokenSource();

        var diagnostics = await _engine.UpdateDocumentAsync(
            DocumentUri!,
            Text ?? string.Empty,
            version: ++_documentVersion,
            CreateOptions(),
            _analysisCts.Token).ConfigureAwait(false);

        await Dispatcher.UIThread.InvokeAsync(() => Diagnostics = diagnostics);
        _documentOpened = true;
    }

    private async Task ShowCompletionAsync()
    {
        if (string.IsNullOrWhiteSpace(DocumentUri) || Document is null)
        {
            return;
        }

        var position = ToSourcePosition(CaretOffset, Document);
        var completionItems = await _engine.GetCompletionsAsync(
            DocumentUri!,
            position,
            CreateOptions(),
            CancellationToken.None).ConfigureAwait(false);

        if (completionItems.IsDefaultOrEmpty)
        {
            return;
        }

        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            _completionWindow?.Close();
            _completionWindow = new CompletionWindow(TextArea);
            var data = _completionWindow.CompletionList.CompletionData;
            foreach (var completionItem in completionItems)
            {
                data.Add(new AxamlCompletionData(completionItem));
            }

            _completionWindow.Show();
            _completionWindow.Closed += (_, _) => _completionWindow = null;
        });
    }

    private void OnEditorKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Space && e.KeyModifiers.HasFlag(KeyModifiers.Control))
        {
            _ = ShowCompletionAsync();
            e.Handled = true;
        }
    }

    private void OnTextEntered(object? sender, TextInputEventArgs e)
    {
        if (string.IsNullOrEmpty(e.Text))
        {
            return;
        }

        var trigger = e.Text[0];
        if (trigger is '<' or ':' or '.' or '{' or ' ')
        {
            _ = ShowCompletionAsync();
        }
    }

    private void OnTextEntering(object? sender, TextInputEventArgs e)
    {
        if (string.IsNullOrEmpty(e.Text))
        {
            return;
        }

        if (_completionWindow?.CompletionList.SelectedItem is null)
        {
            return;
        }

        if (char.IsLetterOrDigit(e.Text[0]))
        {
            return;
        }

        _completionWindow.CompletionList.RequestInsertion(e);
    }

    private XamlLanguageServiceOptions CreateOptions()
    {
        return new XamlLanguageServiceOptions(WorkspaceRoot);
    }
    private static SourcePosition ToSourcePosition(int offset, TextDocument document)
    {
        var boundedOffset = Math.Max(0, Math.Min(offset, document.TextLength));
        var line = document.GetLineByOffset(boundedOffset);
        var lineIndex = line.LineNumber - 1;
        var character = boundedOffset - line.Offset;
        return new SourcePosition(lineIndex, character);
    }
}

internal sealed class AxamlDiagnosticColorizer : DocumentColorizingTransformer
{
    private ImmutableArray<LanguageServiceDiagnostic> _diagnostics = ImmutableArray<LanguageServiceDiagnostic>.Empty;

    private static readonly TextDecorationCollection ErrorDecoration = CreateUnderline(Brushes.IndianRed);
    private static readonly TextDecorationCollection WarningDecoration = CreateUnderline(Brushes.DarkOrange);
    private static readonly TextDecorationCollection InformationDecoration = CreateUnderline(Brushes.DodgerBlue);

    public void UpdateDiagnostics(ImmutableArray<LanguageServiceDiagnostic> diagnostics)
    {
        _diagnostics = diagnostics.IsDefault ? ImmutableArray<LanguageServiceDiagnostic>.Empty : diagnostics;
    }

    protected override void ColorizeLine(DocumentLine line)
    {
        if (_diagnostics.IsDefaultOrEmpty)
        {
            return;
        }

        var lineIndex = line.LineNumber - 1;
        foreach (var diagnostic in _diagnostics)
        {
            if (lineIndex < diagnostic.Range.Start.Line || lineIndex > diagnostic.Range.End.Line)
            {
                continue;
            }

            var startCharacter = lineIndex == diagnostic.Range.Start.Line ? diagnostic.Range.Start.Character : 0;
            var endCharacter = lineIndex == diagnostic.Range.End.Line
                ? diagnostic.Range.End.Character
                : line.Length;

            var startOffset = line.Offset + Math.Max(0, startCharacter);
            var endOffset = line.Offset + Math.Min(Math.Max(startCharacter + 1, endCharacter), line.Length);
            if (startOffset >= endOffset)
            {
                continue;
            }

            var decoration = diagnostic.Severity switch
            {
                LanguageServiceDiagnosticSeverity.Error => ErrorDecoration,
                LanguageServiceDiagnosticSeverity.Warning => WarningDecoration,
                _ => InformationDecoration
            };

            ChangeLinePart(startOffset, endOffset, element =>
            {
                element.TextRunProperties.SetTextDecorations(decoration);
            });
        }
    }

    private static TextDecorationCollection CreateUnderline(IBrush brush)
    {
        return
        [
            new TextDecoration
            {
                Location = TextDecorationLocation.Underline,
                Stroke = brush,
                StrokeThickness = 2,
                StrokeThicknessUnit = TextDecorationUnit.Pixel
            }
        ];
    }
}


internal sealed class AxamlCompletionData : ICompletionData
{
    private readonly XamlCompletionItem _item;

    public AxamlCompletionData(XamlCompletionItem item)
    {
        _item = item;
    }

    public IImage? Image => null;

    public string Text => _item.InsertText;

    public object Content => _item.Label;

    public object? Description => _item.Documentation ?? _item.Detail;

    public double Priority => 0;

    public void Complete(TextArea textArea, ISegment completionSegment, EventArgs insertionRequestEventArgs)
    {
        textArea.Document.Replace(completionSegment, Text);
    }
}

