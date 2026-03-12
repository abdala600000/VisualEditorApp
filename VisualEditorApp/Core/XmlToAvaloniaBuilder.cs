using Avalonia.Controls;
using Avalonia.Media;
using System;
using System.Reflection;
using System.Xml.Linq;
using System.Linq;

namespace VisualEditorApp.Core;

public class XmlToAvaloniaBuilder
{
    // الدالة الرئيسية اللي بتاخد كود الـ XAML وتبني التصميم
    public Control? BuildSafeDesignTree(string xamlCode)
    {
        try
        {
            // 1. قراءة الكود كـ XML عادي جداً (أمان تام ومفيش كراش بسبب أحداث أفلونيا)
            XDocument doc = XDocument.Parse(xamlCode);

            if (doc.Root == null) return null;

            // 2. إرسال العقدة الرئيسية للبناء
            return BuildControlFromNode(doc.Root);
        }
        catch (Exception ex)
        {
            // هنا بيصطاد أخطاء الـ XML (زي نسيان قفلة التاج) مش أخطاء أفلونيا
            System.Diagnostics.Debug.WriteLine($"XML Parse Error: {ex.Message}");
            return null;
        }
    }

    private Control? BuildControlFromNode(XElement node)
    {
        // تجاهل أي خصائص معقدة بتكتب كتاجات فرعية مؤقتاً (زي <Border.Background>)
        if (node.Name.LocalName.Contains(".")) return null;

        // 1. تحديد نوع الكنترول من اسم التاج
        Type? controlType = typeof(Control).Assembly.GetTypes()
            .FirstOrDefault(t => t.Name == node.Name.LocalName);

        if (controlType == null || !typeof(Control).IsAssignableFrom(controlType))
            return null;

        // 2. بناء الكنترول في الذاكرة (Reflection)
        Control newControl = (Control)Activator.CreateInstance(controlType)!;

        // 3. تطبيق الخصائص (Attributes)
        foreach (var attr in node.Attributes())
        {
            ApplyPropertySafe(newControl, attr.Name.LocalName, attr.Value);
        }

        // 4. معالجة العناصر الداخلية (Children)
        if (newControl is Panel panel)
        {
            foreach (var childNode in node.Elements())
            {
                var childControl = BuildControlFromNode(childNode);
                if (childControl != null)
                {
                    panel.Children.Add(childControl);
                }
            }
        }
        else if (newControl is ContentControl cc)
        {
            // لو جواه تاج تاني
            if (node.HasElements)
            {
                cc.Content = BuildControlFromNode(node.Elements().First());
            }
            // لو جواه نص عادي
            else if (!string.IsNullOrWhiteSpace(node.Value))
            {
                cc.Content = node.Value.Trim();
            }
        }

        return newControl;
    }

    // الفلتر الآمن لتطبيق الخصائص (بيتجاهل الأحداث والـ x:Class)
    private void ApplyPropertySafe(Control control, string propertyName, string value)
    {
        // قائمة بالمحظورات اللي بتكسر اللوحة
        string[] blockedProperties = { "Class", "Name", "Click", "PointerPressed", "Loaded" };
        if (blockedProperties.Contains(propertyName) || propertyName.Contains(":")) return;

        try
        {
            // هنا بنترجم الخصائص الأساسية يدوياً لضمان الأمان
            if (propertyName == "Width" && double.TryParse(value, out double w)) control.Width = w;
            else if (propertyName == "Height" && double.TryParse(value, out double h)) control.Height = h;
            else if (propertyName == "Margin") control.Margin = Avalonia.Thickness.Parse(value);
            else if (propertyName == "Canvas.Left" && double.TryParse(value, out double l)) Canvas.SetLeft(control, l);
            else if (propertyName == "Canvas.Top" && double.TryParse(value, out double t)) Canvas.SetTop(control, t);
            else if (propertyName == "Background")
            {
                // تحويل الألوان النصية لفرشاة (Brush)
                control.GetType().GetProperty("Background")?.SetValue(control, Brush.Parse(value));
            }
        }
        catch
        {
            // لو حصل خطأ في ترجمة خاصية معينة، بنتجاهلها والكنترول بيفضل شغال
        }
    }
}