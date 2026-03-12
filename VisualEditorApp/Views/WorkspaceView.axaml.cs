using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.LogicalTree;
using Avalonia.Markup.Xaml;
using Avalonia.VisualTree;
using AvaloniaEdit.Highlighting;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text.RegularExpressions;
using VisualEditorApp.Models;
using VisualEditorApp.Models.Tools;
using VisualEditorApp.ViewModels;

namespace VisualEditorApp;

public partial class WorkspaceView : UserControl
{
    public static WorkspaceView? Instance { get; private set; }
    private Control? _selectedControl;

    public WorkspaceView()
    {
        InitializeComponent();
        Instance = this;

        // --- ÊİÚíá Êáæíä ÇáßæÏ (XML/XAML Highlighting) ---
        XamlEditor.SyntaxHighlighting = HighlightingManager.Instance.GetDefinition("XML");

        // ÇÚÊÑÇÖ Çáßáíß áÊÍÏíÏ ÇáÚäÕÑ (Tunneling)
        DesignSurface.AddHandler(InputElement.PointerPressedEvent, DesignSurface_PreviewPointerPressed, RoutingStrategies.Tunnel);
    }

    public void LoadDesign(Control rootControl)
    {
        DesignSurface.Content = rootControl;
        ClearSelection();

        // ÇÓÊÏÚÇÁ ÊÍÏíË ÇáÔÌÑÉ İæÑ ÊÍãíá ÇáÊÕãíã
        UpdateOutline(rootControl);
    }

    public void ClearWorkspace()
    {
        DesignSurface.Content = null;
        ClearSelection();
    }

    // --- 1. ÇáÊÍÏíÏ æÇáÇÚÊÑÇÖ ---
    private void DesignSurface_PreviewPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        // ÅĞÇ ßäÇ İí æÖÚ ÇáãÚÇíäÉ¡ áÇ ÊİÚá ÔíÆÇğ æÇÊÑß ÇáãÇæÓ íãÑ ááßäÊÑæá ÇáÍŞíŞí
        if (_isPreviewMode) return;

        if (e.Source is Control clickedControl && clickedControl != DesignSurface)
        {
            if (clickedControl is Window || clickedControl.Name == "DesignSurface")
            {
                ClearSelection();
                return;
            }

            SelectControl(clickedControl);
            AdornerCanvas.IsHitTestVisible = true;
            e.Handled = true; // ÇÚÊÑÇÖ ÇáãÇæÓ (íÍÏË İŞØ İí æÖÚ ÇáÊÕãíã)
        }
        else
        {
            ClearSelection();
        }


        
    }

    public void SelectControl(Control control)
    {
        _selectedControl = control;
        UpdateAdornerPosition();
        SelectionAdorner.IsVisible = true;

        // --- ãíÒÉ ÇáÊÒÇãä ãÚ ÇáßæÏ (Sync Selection) ---
        SyncCodeEditorToControl(control);
    }
    private void SyncCodeEditorToControl(Control control)
    {
        if (string.IsNullOrEmpty(XamlEditor.Text)) return;

        string typeName = control.GetType().Name;
        string xaml = XamlEditor.Text;

        // äÈÍË Úä Ãæá ÙåæÑ áÇÓã ÇáßäÊÑæá İí ÇáßæÏ
        // ãáÇÍÙÉ: İí ÇáãÔÇÑíÚ ÇáÖÎãÉ äÓÊÎÏã äÙÇã ÅÍÏÇËíÇÊ ÃÏŞ¡ æáßä åĞÇ íİí ÈÇáÛÑÖ ÍÇáíÇğ
        int index = xaml.IndexOf("<" + typeName);

        if (index != -1)
        {
            // ÊÍÑíß ÇáãÄÔÑ áãßÇä ÇáßæÏ
            XamlEditor.CaretOffset = index;
            // ÌÚá ÇáãÍÑÑ íäÒá ÂáíÇğ áãßÇä ÇáÓØÑ
            XamlEditor.ScrollToLine(XamlEditor.Document.GetLineByOffset(index).LineNumber);
        }
    }

    // ÏÇáÉ áÇÓÊŞÈÇá ÇáäÕ ãä ÇáÎÇÑÌ (MainWindow)
    public void SetXamlContent(string xml)
    {
        _isInternalUpdate = true;
        XamlEditor.Text = xml;
        _isInternalUpdate = false;
        RefreshDesigner(xml);
    }
    private void ClearSelection()
    {
        _selectedControl = null;
        SelectionAdorner.IsVisible = false;
        AdornerCanvas.IsHitTestVisible = false;
    }

    // --- 2. ÊÍÏíË ãæŞÚ ÅØÇÑ ÇáÊÍÏíÏ İæŞ ÇáßäÊÑæá ---
    private void UpdateAdornerPosition()
    {
        if (_selectedControl == null) return;

        // ÇáÍÕæá Úáì ÅÍÏÇËíÇÊ ÇáßäÊÑæá ÇáÍŞíŞí æÊÍÑíß ÇáÅØÇÑ ÇáÃÒÑŞ İæŞå
        var transform = _selectedControl.TransformToVisual(AdornerCanvas);
        if (transform != null)
        {
            var bounds = new Rect(new Point(0, 0), _selectedControl.Bounds.Size);
            var rectInAdorner = bounds.TransformToAABB(transform.Value);

            SelectionAdorner.Width = rectInAdorner.Width;
            SelectionAdorner.Height = rectInAdorner.Height;
            Canvas.SetLeft(SelectionAdorner, rectInAdorner.X);
            Canvas.SetTop(SelectionAdorner, rectInAdorner.Y);
        }
    }

    // --- 3. ãÍÑß ÇáÊßÈíÑ æÇáÊÕÛíÑ (Resizing Engine) ---
    private void Resize_DragDelta(object? sender, VectorEventArgs e)
    {
        if (_selectedControl == null || sender is not Thumb thumb) return;

        double deltaX = e.Vector.X;
        double deltaY = e.Vector.Y;

        // áæ ÇáßäÊÑæá ãáæÔ ÚÑÖ Ãæ Øæá ÕÑíÍ (NaN)¡ äÃÎĞ ÍÌãå ÇáÍÇáí ßÈÏÇíÉ
        double currentWidth = double.IsNaN(_selectedControl.Width) ? _selectedControl.Bounds.Width : _selectedControl.Width;
        double currentHeight = double.IsNaN(_selectedControl.Height) ? _selectedControl.Bounds.Height : _selectedControl.Height;

        double newWidth = currentWidth;
        double newHeight = currentHeight;

        if (thumb.Name == "TopLeft")
        {
            newWidth -= deltaX; newHeight -= deltaY;
        }
        else if (thumb.Name == "TopRight")
        {
            newWidth += deltaX; newHeight -= deltaY;
        }
        else if (thumb.Name == "BottomLeft")
        {
            newWidth -= deltaX; newHeight += deltaY;
        }
        else if (thumb.Name == "BottomRight")
        {
            newWidth += deltaX; newHeight += deltaY;
        }

        // ÊØÈíŞ ÇáãŞÇÓ ÇáÌÏíÏ Úáì ÇáßäÊÑæá ÇáÍŞíŞí (ãÚ ÇáÊÃßÏ Ãäå áÇ íÕÛÑ ÌÏÇğ)
        if (newWidth > 10) _selectedControl.Width = newWidth;
        if (newHeight > 10) _selectedControl.Height = newHeight;

        // ÊÍÏíË ãßÇä ÇáÅØÇÑ ÇáÃÒÑŞ áíØÇÈŞ ÇáÍÌã ÇáÌÏíÏ
        UpdateAdornerPosition();
    }
    private bool _isPreviewMode = false;

    public void SetPreviewMode(bool isPreview)
    {
        _isPreviewMode = isPreview;
        if (_isPreviewMode)
        {
            ClearSelection(); // ÅÎİÇÁ ÇáãÑÈÚÇÊ ÇáÒÑŞÇÁ İæÑÇğ
        }
    }
    private bool _isInternalUpdate = false;

    // ÏÇáÉ áÊÍÏíË ÇáäÕ ÚäÏ İÊÍ ãáİ ãä MainWindow
    public void SetXamlText(string xml)
    {
        _isInternalUpdate = true;
        XamlEditor.Text = xml;
        _isInternalUpdate = false;

        // ÊÍÏíË ÇáÊÕãíã İæÑÇğ
        RefreshDesigner(xml);
    }

    // ÍÏË ÚäÏ ßÊÇÈÉ Ãí ÔíÁ İí ÇáãÍÑÑ ÇáÓİáí
    private void XamlEditor_TextChanged(object? sender, EventArgs e)
    {
        if (_isInternalUpdate) return;

        // ÊÍÏíË ÇáÊÕãíã "Live" ÃËäÇÁ ÇáßÊÇÈÉ
        RefreshDesigner(XamlEditor.Text);
    }
    // --- ãÕİÇÉ ÊäÙíİ ÇáÜ XAML (XAML Sanitizer) ---
    private string SanitizeXaml(string originalXaml)
    {
        string clean = originalXaml;

        // 1. ÊÍæíá CompiledBinding Åáì Binding ÚÇÏí áßí íÚãá æŞÊ ÇáÊÕãíã
        clean = Regex.Replace(clean, @"\{CompiledBinding\b", "{Binding");

        // 2. ÅÒÇáÉ x:Class (áÃäåÇ ÊÊØáÈ ßæÏ Îáİí ÛíÑ ãæÌæÏ ÃËäÇÁ ÇáÊÕãíã)
        clean = Regex.Replace(clean, @"x:Class=""[^""]*""", "");

        // 3. ÅÒÇáÉ ÇáÃÍÏÇË (Events) ÇáÊí ÊÈÍË Úä ÏæÇá İí ÇáßæÏ ÇáÎáİí
        clean = Regex.Replace(clean, @"\s+(Click|PointerPressed|PointerReleased|KeyDown|KeyUp|Loaded|PointerMoved)=""[^""]*""", "");

        // ãáÇÍÙÉ åÇãÉ: áŞÏ ŞãäÇ ÈÅÒÇáÉ ãÓÍ x:Name ãä åäÇ¡ 
        // áÃäß ÊÓÊÎÏã ElementName bindings æÇáÊí ÊÚÊãÏ Úáì æÌæÏ ÇáÃÓãÇÁ.
        // ÈÏáÇğ ãä Ğáß¡ ÓäãÓÍ x:Name ãä ÇáÚäÇÕÑ ÛíÑ ÇáãÑÆíÉ İŞØ (ãËá Transforms) 
        // Ãæ äÊÑß Avalonia ÊÊÚÇãá ãÚ ÇáÃÓãÇÁ ÇáÕÍíÍÉ ááßäÊÑæáÇÊ.
        clean = Regex.Replace(clean, @"<([^>]+)\s+x:Name=""[^""]*""([^>]*)>\s*</\1>", "<$1$2></$1>"); // ÊäÙíİ Ãæáí ááÜ Transforms

        return clean;
    }
    private void RefreshDesigner(string xaml)
    {
        try
        {
            // ÇÓÊÎÏÇã ãÍÑß Avalonia ÇáÃÕáí áÊÍæíá ÇáäÕ áßäÊÑæá
            // ãáÇÍÙÉ: ÊÃßÏ ãä æÌæÏ ÏÇáÉ SanitizeXaml ÇáÊí ÚãáäÇåÇ ÓÇÈŞÇğ áÊäÙíİ ÇáßæÏ
            string cleanXml = SanitizeXaml(xaml);
            var parsed = Avalonia.Markup.Xaml.AvaloniaRuntimeXamlLoader.Parse<Control>(cleanXml);

            if (parsed != null)
            {
                LoadDesign(parsed);
            }
        }
        catch
        {
            // äÊÌÇåá ÇáÃÎØÇÁ ÃËäÇÁ ãÇ ÇáãÓÊÎÏã áÓå ÈíßÊÈ ßæÏ äÇŞÕ
        }
    }



    private void UpdateOutline(Control root)
    {
        var rootNode = BuildNode(root);

        // ÊÍÏíË ÇáäÓÎÉ ÇáæÍíÏÉ (ÇáÊí íÚÑÖåÇ ÇáÜ Dock ÍÇáíÇğ)
        // äÓÊÎÏã Clear æ Add ÈÏáÇğ ãä New áÖãÇä Ãä ÇáÜ TreeView ÊÔÚÑ ÈÇáÊÛííÑ
        DocumentOutlineTool.Instance.Nodes.Clear();
        DocumentOutlineTool.Instance.Nodes.Add(rootNode);
    }
    private ElementNode BuildNode(Control control)
    {
        var node = new ElementNode { Header = control.GetType().Name, RelatedControl = control };

        // ÇÓÊÎÏÇã ÇáÜ LogicalChildren ááÍÕæá Úáì ÇáÚäÇÕÑ ÇáãÊÏÇÎáÉ (ãËá Border Ìæå StackPanel)
        foreach (var child in control.GetLogicalChildren())
        {
            if (child is Control childControl)
            {
                node.Children.Add(BuildNode(childControl));
            }
        }
        return node;
    }
}

