namespace VisualEditor.Toolbox.Prop;

public class PropertyGroup
{
    public string Key { get; set; } = ""; // ÇÓã ÇáãÌãæÚÉ (Layout, Brushes...)
    public List<PropertyItem> Items { get; set; } = new();
}
