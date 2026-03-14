using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;


namespace VisualEditor.Toolbox.Prop;

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
