using Avalonia;
using Avalonia.Controls;
using Avalonia.Data.Converters;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Reflection;
using VisualEditorApp.Models;

namespace VisualEditorApp;

public class PropertyTypeToVisibleConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is Type type && parameter is string target)
        {
            // ›Õ’ œﬁÌﬁ ··√‰Ê«⁄
            bool isBool = type == typeof(bool);
            // ·Ê «·‰Ê⁄ ÂÊ Color √Ê »ÌÕ ÊÌ ⁄·Ï ﬂ·„… Brush (“Ì SolidColorBrush)
            bool isColor = type.Name.Contains("Color") || type.Name.Contains("Brush") || typeof(Avalonia.Media.IBrush).IsAssignableFrom(type);

            switch (target.ToLower())
            {
                case "bool": return isBool;
                case "color": return isColor;
                case "text": return !isBool && !isColor;
            }
        }
        return false;
    }
    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => null;
}

public class BrushToColorConverter : IValueConverter
{
    // ⁄„· ‰”Œ… Static ⁄‘«‰ ‰Ê’· ·Â« »”ÂÊ·… „‰ «·‹ XAML
    public static readonly BrushToColorConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        // ·Ê «·ﬁÌ„… Ã«Ì… Brush (“Ì Background) »‰ÕÊ·Â« ·‹ Color ⁄‘«‰ «·‹ ColorPicker Ì›Â„Â«
        if (value is ISolidColorBrush solidBrush)
            return solidBrush.Color;

        if (value is Color color)
            return color;

        return Colors.Transparent; // ﬁÌ„… «› —«÷Ì…
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        // ·„« «·„” Œœ„ ÌŒ «— ·Ê‰ „‰ «·‹ ColorPicker »‰—Ã⁄Â ﬂ‹ SolidColorBrush ··ﬂ‰ —Ê·
        if (value is Color color)
            return new SolidColorBrush(color);

        return null;
    }
}
public class PropertyItem
{
    public string Name { get; set; } = "";
    public object? Value { get; set; }
    public Type? PropertyType { get; set; }
    public PropertyInfo? Info { get; set; } // ≈÷«›… „—Ã⁄ ··Œ«’Ì…
    public object? Target { get; set; }    // „—Ã⁄ ··ﬂ‰ —Ê· ‰›”Â

    // œ«·… · ÕœÌÀ «·ﬁÌ„… ›Ì «·ﬂ‰ —Ê· «·ÕﬁÌﬁÌ
    public void UpdateValue(object newValue)
    {
        try
        {
            var converted = Convert.ChangeType(newValue, PropertyType!);
            Info?.SetValue(Target, converted);
        }
        catch { /* Œÿ√ ›Ì «· ÕÊÌ· */ }
    }
}
public class PropertyGroup
{
    public string Key { get; set; } = ""; // «”„ «·„Ã„Ê⁄… (Layout, Brushes...)
    public List<PropertyItem> Items { get; set; } = new();
}
public partial class PropertiesView : UserControl
{
    public static PropertiesView? Instance { get; private set; }
    private Control? _currentElement;
    private bool _isUpdatingFromCode = false;
    private bool _isAlphabetical = false;
    private List<PropertyItem> _rawProperties = new(); // «·„’œ— «·√’·Ì

    public PropertiesView()
    {
        InitializeComponent();
        Instance = this;
    }

    public void SetSelectedElement(Control? element)
    {
        _currentElement = element;
        if (element == null) return;

        _isUpdatingFromCode = true;
        NameEditor.Text = element.Name ?? "Unnamed";
        SelectedTypeText.Text = $"({element.GetType().Name})";

        // 1. Ã·» «·Œ’«∆’ „—… Ê«Õœ… ›ﬁÿ
        _rawProperties = GetFilteredProperties(element);

        // 2.  ÕœÌÀ «·‘«‘…
        RefreshDisplay();
        _isUpdatingFromCode = false;
    }

    private void RefreshDisplay()
    {
        if (_currentElement == null || _rawProperties == null) return;

        //  ÿ»Ìﬁ «·›· —… («·»ÕÀ) √Ê·«
        var searchText = SearchBox?.Text?.ToLower() ?? "";
        var filtered = _rawProperties.Where(p => p.Name.ToLower().Contains(searchText)).ToList();

        if (_isAlphabetical)
        {
            GroupsControl.ItemsSource = new List<PropertyGroup> {
                new PropertyGroup { Key = "Properties A-Z", Items = filtered.OrderBy(p => p.Name).ToList() }
            };
        }
        else
        {
            // «· Ã„Ì⁄ Õ”» «·‹ Category
            GroupsControl.ItemsSource = filtered.GroupBy(p => GetCategory(p.Info!))
                .Select(g => new PropertyGroup { Key = g.Key, Items = g.ToList() })
                .OrderBy(g => g.Key).ToList();
        }
    }

    // —»ÿ «·»ÕÀ »«·‹ RefreshDisplay
    private void SearchBox_KeyUp(object? sender, KeyEventArgs e) => RefreshDisplay();

    private void SortAlphabetical_Click(object? sender, RoutedEventArgs e) { _isAlphabetical = true; RefreshDisplay(); }
    private void SortByCategory_Click(object? sender, RoutedEventArgs e) { _isAlphabetical = false; RefreshDisplay(); }

    private List<PropertyItem> GetFilteredProperties(Control element)
    {
        return element.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.CanRead && p.CanWrite && p.GetIndexParameters().Length == 0)
            .Select(p => {
                try
                {
                    return new PropertyItem
                    {
                        Name = p.Name,
                        Value = p.GetValue(element),
                        PropertyType = p.PropertyType,
                        Info = p,
                        Target = element
                    };
                }
                catch { return null; }
            }).Where(p => p != null).Cast<PropertyItem>().ToList();
    }

    private string GetCategory(PropertyInfo prop)
    {
        string n = prop.Name.ToLower();
        if (n.Contains("width") || n.Contains("height") || n.Contains("margin") || n.Contains("canvas") || n.Contains("position")) return "Layout";
        if (n.Contains("color") || n.Contains("brush") || n.Contains("background") || n.Contains("opacity")) return "Appearance";
        if (n.Contains("text") || n.Contains("font") || n.Contains("content")) return "Content";
        return "Common";
    }
}