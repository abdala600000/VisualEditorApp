using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using VisualEditorApp.Models;

namespace VisualEditorApp;

public class PropertyItem
{
    public string Name { get; set; } = "";
    public object? Value { get; set; }
}

public class PropertyGroup
{
    public string Key { get; set; } = ""; // «”„ «·„Ã„Ê⁄… (Layout, Brushes...)
    public List<PropertyItem> Items { get; set; } = new();
}
public partial class PropertiesView : UserControl
{
    public static PropertiesView? Instance { get; private set; }
    private Control? _currentElement; //  €ÌÌ— «·‰Ê⁄ ··ﬂ‰ —Ê· «·⁄«œÌ
    private bool _isUpdatingFromCode = false;

    public PropertiesView()
    {
        InitializeComponent();
        Instance = this;
    }

    public void SetSelectedElement(Control? element)
    {
        if (element == null || GroupsControl == null)
        {
            if (GroupsControl != null) GroupsControl.ItemsSource = null;
            return;
        }

        _isUpdatingFromCode = true;

        // «·›· —… Â‰« ÂÌ «·”—
        var allProps = element.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance)
                        .Where(p => p.CanRead &&
                                    p.CanWrite &&
                                    p.GetIndexParameters().Length == 0) // <--- «·”ÿ— œÂ ÂÊ «··Ì ÂÌÕ· «·„‘ﬂ·…
                        .Select(p => {
                            try
                            {
                                return new PropertyItem
                                {
                                    Name = p.Name,
                                    Value = p.GetValue(element)
                                };
                            }
                            catch
                            {
                                return null; // · Ã‰» √Ì Œ«’Ì…  «‰Ì…  ÷—» √À‰«¡ «·ﬁ—«¡…
                            }
                        })
                        .Where(p => p != null)
                        .Cast<PropertyItem>()
                        .ToList();

        // ... »«ﬁÌ ﬂÊœ «· Ã„Ì⁄ (Grouping) “Ì „« ÂÊ ...
        var groupedData = new List<PropertyGroup>
    {
        new PropertyGroup { Key = "Layout", Items = allProps.Where(p => p.Name.Contains("Width") || p.Name.Contains("Height") || p.Name.Contains("Margin")).ToList() },
        new PropertyGroup { Key = "Appearance", Items = allProps.Where(p => p.Name.Contains("Background") || p.Name.Contains("Opacity")).ToList() },
        new PropertyGroup { Key = "Common", Items = allProps.Where(p => !p.Name.Contains("Width") && !p.Name.Contains("Background")).Take(20).ToList() }
    };

        GroupsControl.ItemsSource = groupedData.Where(g => g.Items.Any()).ToList();
        _isUpdatingFromCode = false;
    }

    private string GetCategory(PropertyInfo prop)
    {
        string n = prop.Name.ToLower();
        if (n.Contains("width") || n.Contains("height") || n.Contains("margin") || n.Contains("canvas")) return "Layout";
        if (n.Contains("color") || n.Contains("brush") || n.Contains("background")) return "Brushes";
        if (n.Contains("text") || n.Contains("font")) return "Typography";
        return "Common";
    }

}