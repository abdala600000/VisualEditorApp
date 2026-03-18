using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Media;
using System.Reflection;
using System.Text;

namespace VisualEditor.Core
{
    public static class XamlGenerator
    {
        // الدالة الرئيسية اللي بتناديها من بره
        public static string GenerateXaml(Control rootControl)
        {
            if (rootControl == null) return string.Empty;

            var sb = new StringBuilder();

            // كتابة الترويسة (Header)
            sb.AppendLine("<UserControl xmlns=\"https://github.com/avaloniaui\"");
            sb.AppendLine("             xmlns:x=\"http://schemas.microsoft.com/winfx/2006/xaml\">");

            // البدء في ترجمة الكنترول الأساسي وكل اللي جواه
            BuildControlXaml(rootControl, sb, 1);

            sb.AppendLine("</UserControl>");

            return sb.ToString();
        }

        // دالة الترجمة الداخلية
        private static void BuildControlXaml(Control control, StringBuilder sb, int indentLevel)
        {
            string indent = new string(' ', indentLevel * 4);
            string typeName = control.GetType().Name;

            sb.Append($"{indent}<{typeName}");

            // 1. الخصائص الأساسية
            if (!double.IsNaN(control.Width)) sb.Append($" Width=\"{(int)control.Width}\"");
            if (!double.IsNaN(control.Height)) sb.Append($" Height=\"{(int)control.Height}\"");

            if (control.Margin != default)
                sb.Append($" Margin=\"{control.Margin.Left},{control.Margin.Top},{control.Margin.Right},{control.Margin.Bottom}\"");

            if (control.Parent is Canvas)
            {
                double left = Canvas.GetLeft(control);
                double top = Canvas.GetTop(control);
                if (!double.IsNaN(left)) sb.Append($" Canvas.Left=\"{(int)left}\"");
                if (!double.IsNaN(top)) sb.Append($" Canvas.Top=\"{(int)top}\"");
            }

            if (control is Border border && border.CornerRadius != default)
            {
                sb.Append($" CornerRadius=\"{border.CornerRadius.TopLeft}\"");
            }

            // 2. Background
            var bgValue = control.GetType().GetProperty("Background")?.GetValue(control);
            if (bgValue is SolidColorBrush bgBrush)
                sb.Append($" Background=\"{FormatColor(bgBrush.Color)}\"");

            // 3. Foreground
            var fgValue = control.GetType().GetProperty("Foreground")?.GetValue(control);
            if (fgValue is SolidColorBrush fgBrush)
                sb.Append($" Foreground=\"{FormatColor(fgBrush.Color)}\"");

            // 4. FontSize
            var fontSizeProp = control.GetType().GetProperty("FontSize");
            if (fontSizeProp != null)
            {
                var fontSizeValue = fontSizeProp.GetValue(control);
                if (fontSizeValue is double fontSize && fontSize != 12.0)
                    sb.Append($" FontSize=\"{fontSize}\"");
            }

            // 5. RenderTransform — emitted as property element, requires open tag
            string? renderTransformElement = BuildRenderTransformElement(control, typeName, indent + "    ");

            bool hasChildren = false;

            // 6. معالجة الأبناء (Children)
            if (control is Panel panel && panel.Children.Count > 0)
            {
                sb.AppendLine(">");
                if (renderTransformElement != null) sb.AppendLine(renderTransformElement);
                foreach (var child in panel.Children)
                {
                    if (child is Control childCtrl && childCtrl.Name != "SelectionAdorner")
                        BuildControlXaml(childCtrl, sb, indentLevel + 1);
                }
                hasChildren = true;
            }
            else if (control is ContentControl cc && cc.Content != null)
            {
                sb.AppendLine(">");
                if (renderTransformElement != null) sb.AppendLine(renderTransformElement);
                if (cc.Content is Control childCtrl)
                    BuildControlXaml(childCtrl, sb, indentLevel + 1);
                else
                    sb.AppendLine($"{indent}    {cc.Content}");
                hasChildren = true;
            }
            else if (control is Border border1 && border1.Child != null)
            {
                sb.AppendLine(">");
                if (renderTransformElement != null) sb.AppendLine(renderTransformElement);
                if (border1.Child is Control childCtrl && childCtrl.Name != "SelectionAdorner")
                    BuildControlXaml(childCtrl, sb, indentLevel + 1);
                hasChildren = true;
            }
            else if (control is TextBlock tb && !string.IsNullOrEmpty(tb.Text))
            {
                sb.AppendLine(">");
                if (renderTransformElement != null) sb.AppendLine(renderTransformElement);
                sb.AppendLine($"{indent}    {tb.Text}");
                hasChildren = true;
            }
            else if (renderTransformElement != null)
            {
                // نحتاج open tag لأن RenderTransform property element موجود
                sb.AppendLine(">");
                sb.AppendLine(renderTransformElement);
                hasChildren = true;
            }

            // 7. إغلاق التاج
            if (hasChildren)
                sb.AppendLine($"{indent}</{typeName}>");
            else
                sb.AppendLine(" />");
        }

        private static string? BuildRenderTransformElement(Control control, string typeName, string childIndent)
        {
            if (control.RenderTransform == null) return null;

            string transformContent;
            if (control.RenderTransform is RotateTransform rt)
                transformContent = $"<RotateTransform Angle=\"{rt.Angle}\"/>";
            else if (control.RenderTransform is SkewTransform st)
                transformContent = $"<SkewTransform AngleX=\"{st.AngleX}\" AngleY=\"{st.AngleY}\"/>";
            else if (control.RenderTransform is TransformGroup tg)
            {
                var sb = new StringBuilder();
                sb.Append("<TransformGroup>");
                foreach (var t in tg.Children)
                {
                    if (t is SkewTransform s)
                        sb.Append($"<SkewTransform AngleX=\"{s.AngleX}\" AngleY=\"{s.AngleY}\"/>");
                    else if (t is RotateTransform r)
                        sb.Append($"<RotateTransform Angle=\"{r.Angle}\"/>");
                }
                sb.Append("</TransformGroup>");
                transformContent = sb.ToString();
            }
            else
                return null;

            return $"{childIndent}<{typeName}.RenderTransform>{transformContent}</{typeName}.RenderTransform>";
        }

        private static string FormatColor(Color color)
        {
            // استخدام الاسم إذا كان متاحاً، وإلا الـ hex
            if (color.A == 255)
                return $"#{color.R:X2}{color.G:X2}{color.B:X2}";
            return $"#{color.A:X2}{color.R:X2}{color.G:X2}{color.B:X2}";
        }
    }
}