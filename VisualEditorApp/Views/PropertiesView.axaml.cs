using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using System;

namespace VisualEditorApp;

public partial class PropertiesView : UserControl
{
    public static PropertiesView? Instance { get; private set; }
    private DesignerItem? _currentElement;
    private bool _isUpdatingFromCode = false;

    public PropertiesView()
    {
        InitializeComponent();
        Instance = this;
    }

    // åĞå ÇáÏÇáÉ ÓíÊã ÇÓÊÏÚÇÄåÇ ãä ãÓÇÍÉ ÇáÚãá ÚäÏ ÇáäŞÑ Úáì Ãí ÚäÕÑ
    public void SetSelectedElement(DesignerItem? element)
    {
        _currentElement = element;
        _isUpdatingFromCode = true; // áãäÚ ÊÔÛíá ÍÏË TextChanged ÃËäÇÁ ÇáŞÑÇÁÉ

        if (_currentElement != null)
        {
            // ÇÓÊÎÑÇÌ ÇÓã ÇáÚäÕÑ ÇáÏÇÎáí (ãËáÇğ Button Ãæ Rectangle)
            var contentControl = _currentElement.FindControl<ContentControl>("ShapeContainer");
            ElementTypeText.Text = contentControl?.Content?.GetType().Name ?? "DesignerItem";

            WidthBox.Text = Math.Round(_currentElement.Width, 2).ToString();
            HeightBox.Text = Math.Round(_currentElement.Height, 2).ToString();
            LeftBox.Text = Math.Round(Canvas.GetLeft(_currentElement), 2).ToString();
            TopBox.Text = Math.Round(Canvas.GetTop(_currentElement), 2).ToString();
        }
        else
        {
            ElementTypeText.Text = "None";
            WidthBox.Text = ""; HeightBox.Text = ""; LeftBox.Text = ""; TopBox.Text = "";
        }

        _isUpdatingFromCode = false;
    }

    // åĞÇ ÇáÍÏË íÚãá ÚäÏãÇ íßÊÈ ÇáãÓÊÎÏã ÑŞãÇğ ÌÏíÏÇğ İí Ãí TextBox
    private void PropertyValue_Changed(object? sender, TextChangedEventArgs e)
    {
        if (_isUpdatingFromCode || _currentElement == null) return;

        if (double.TryParse(WidthBox.Text, out double w) && w > 0) _currentElement.Width = w;
        if (double.TryParse(HeightBox.Text, out double h) && h > 0) _currentElement.Height = h;

        if (double.TryParse(LeftBox.Text, out double x)) Canvas.SetLeft(_currentElement, x);
        if (double.TryParse(TopBox.Text, out double y)) Canvas.SetTop(_currentElement, y);
    }
}