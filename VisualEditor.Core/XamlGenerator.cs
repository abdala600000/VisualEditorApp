using Avalonia.Controls;
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

        // دالة الترجمة الداخلية (اللي إنت كنت كاتبها)
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

            bool hasChildren = false;

            // 2. معالجة الأبناء (Children)
            if (control is Panel panel && panel.Children.Count > 0)
            {
                sb.AppendLine(">");
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
                if (cc.Content is Control childCtrl)
                    BuildControlXaml(childCtrl, sb, indentLevel + 1);
                else
                    sb.AppendLine($"{indent}    {cc.Content}");
                hasChildren = true;
            }
            else if (control is Border border1 && border1.Child != null)
            {
                sb.AppendLine(">");
                if (border1.Child is Control childCtrl && childCtrl.Name != "SelectionAdorner")
                    BuildControlXaml(childCtrl, sb, indentLevel + 1);
                hasChildren = true;
            }
            else if (control is TextBlock tb && !string.IsNullOrEmpty(tb.Text))
            {
                sb.AppendLine(">");
                sb.AppendLine($"{indent}    {tb.Text}");
                hasChildren = true;
            }

            // 3. إغلاق التاج
            if (hasChildren)
                sb.AppendLine($"{indent}</{typeName}>");
            else
                sb.AppendLine(" />");
        }
    }
}