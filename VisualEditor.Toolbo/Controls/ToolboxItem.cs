namespace VisualEditor.Toolbox.Controls
{
    public class ToolboxItem
    {
        public string Name { get; set; } = "";
        public string IconPath { get; set; } = ""; // ممكن نحط SVG Path للأيقونة
        public Type? ControlType { get; set; } // الأهم: نوع الكنترول (مثلاً typeof(Button))
    }
}
