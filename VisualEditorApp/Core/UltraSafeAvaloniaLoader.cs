using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;

namespace VisualEditorApp.Core
{
    public static class UltraSafeAvaloniaLoader
    {
        public static object LoadSafe(string xamlText, object target = null)
        {
            try
            {
                // 1. تنظيف عميق للـ XAML قبل الإرسال للمحمل
                string cleanedXaml = DeepSanitize(xamlText);

                using var stream = new MemoryStream(Encoding.UTF8.GetBytes(cleanedXaml));

                // 2. محاولة التحميل مع ربط الـ Code-behind إذا وُجد
                return AvaloniaRuntimeXamlLoader.Load(stream, null, target);
            }
            catch (Exception ex)
            {
                // 3. في حالة فشل التحميل الكلي، نُظهر رسالة خطأ داخل المحرر بدلاً من الانهيار
                return CreateErrorFallback(ex.Message);
            }
        }
        public static string DeepSanitize(string xaml)
        {
            try
            {
                // 1. تنظيف النصوص والروابط المكسورة (كما فعلنا سابقاً)
                string resPattern = @"avares://Feed_Project/[^""'\s<>]*";
                xaml = System.Text.RegularExpressions.Regex.Replace(xaml, resPattern, "");

                var doc = new XmlDocument();
                doc.LoadXml(xaml);
                var root = doc.DocumentElement;

                if (root == null) return xaml;

                // 2. حل مشكلة InvalidCastException (تحويل Window إلى UserControl)
                // إذا كان الجذر Window، نغيره لـ UserControl لكي يقبله المحرر كـ Content
                if (root.LocalName == "Window")
                {
                    // إنشاء عنصر UserControl جديد ونقل كل الخصائص والمحتوى إليه
                    XmlElement newRoot = doc.CreateElement("UserControl", "https://github.com");

                    // نقل كافة الـ Attributes (الخصائص)
                    foreach (XmlAttribute attr in root.Attributes)
                    {
                        // نتجنب نقل الخصائص التي تنفرد بها الـ Window (مثل Title, Icon)
                        string[] windowOnly = { "Title", "Icon", "WindowStartupLocation", "WindowState", "CanResize" };
                        if (!windowOnly.Contains(attr.LocalName))
                        {
                            newRoot.SetAttribute(attr.Name, attr.Value);
                        }
                    }

                    // نقل كافة العناصر الداخلية
                    while (root.HasChildNodes)
                    {
                        newRoot.AppendChild(root.FirstChild);
                    }

                    doc.ReplaceChild(newRoot, root);
                    root = newRoot;
                }

                // 3. تنظيف الـ x:Class والـ Namespaces
                if (root.HasAttribute("x:Class")) root.RemoveAttribute("x:Class");

                // 4. جولة التنظيف المعتادة (الصور، الترانزفورم، الأحداث)
                var allElements = doc.GetElementsByTagName("*");
                for (int i = allElements.Count - 1; i >= 0; i--)
                {
                    XmlElement el = (XmlElement)allElements[i];

                    // حذف FontFamily الفارغ (حل مشكلة Constructor)
                    if (el.LocalName == "FontFamily" && string.IsNullOrWhiteSpace(el.InnerText))
                    {
                        el.ParentNode?.RemoveChild(el);
                        continue;
                    }

                    // حذف الخصائص الممنوعة
                    string[] forbiddenNames = { "ScaleTransform", "RotateTransform", "SkewTransform", "MatrixTransform" };
                    if (forbiddenNames.Contains(el.LocalName))
                    {
                        el.RemoveAttribute("Name");
                        el.RemoveAttribute("x:Name");
                    }

                    // حذف الـ Events
                    for (int j = el.Attributes.Count - 1; j >= 0; j--)
                    {
                        if (IsEventAttribute(el.Attributes[j].Name)) el.RemoveAttribute(el.Attributes[j].Name);
                    }
                }

                return doc.OuterXml;
            }
            catch { return "<UserControl xmlns='https://github.com'></UserControl>"; }
        }

        //public static string DeepSanitize(string xaml)
        //{
        //    try
        //    {
        //        var doc = new XmlDocument();
        //        doc.LoadXml(xaml);
        //        var root = doc.DocumentElement;

        //        if (root != null && root.HasAttribute("x:Class"))
        //            root.RemoveAttribute("x:Class");

        //        var allElements = doc.GetElementsByTagName("*");
        //        foreach (XmlElement el in allElements)
        //        {
        //            // 1. معالجة الصور ومصادر الـ Bitmap (حل مشكلة avares)
        //            if (el.HasAttribute("Source") || el.HasAttribute("Background"))
        //            {
        //                FixResourcePath(el, "Source");
        //                FixResourcePath(el, "Background");
        //            }

        //            // 2. معالجة الـ Transforms والأسماء الممنوعة (كما فعلنا سابقاً)
        //            string[] forbiddenNames = { "ScaleTransform", "RotateTransform", "SkewTransform", "MatrixTransform" };
        //            if (forbiddenNames.Contains(el.LocalName))
        //            {
        //                el.RemoveAttribute("Name");
        //                el.RemoveAttribute("x:Name");
        //            }

        //            // 3. حذف الـ Events
        //            for (int i = el.Attributes.Count - 1; i >= 0; i--)
        //            {
        //                if (IsEventAttribute(el.Attributes[i].Name))
        //                    el.RemoveAttribute(el.Attributes[i].Name);
        //            }
        //        }

        //        return doc.OuterXml;
        //    }
        //    catch { return xaml; }
        //}

        private static void FixResourcePath(XmlElement el, string attrName)
        {
            if (!el.HasAttribute(attrName)) return;

            string path = el.GetAttribute(attrName);

            // إذا كان المسار يشير لمشروع خارجي (avares://)
            if (path.StartsWith("avares://"))
            {
                // تحقق إذا كان المشروع الحالي هو صاحب المسار (استبدل 'VisualEditorApp' باسم مشروعك)
                if (!path.Contains("VisualEditorApp"))
                {
                    // الخيار الأفضل للمحرر: حذف المسار لكي لا ينهار، وسيقوم المحرك بعرض العنصر فارغاً
                    el.RemoveAttribute(attrName);

                    // أو يمكنك استبداله بصورة داخلية في مشروعك إذا أردت:
                    // el.SetAttribute(attrName, "avares://VisualEditorApp/Assets/placeholder.png");
                }
            }
        }
        



        private static bool IsEventAttribute(string name) =>
            name.Contains("Click") || name.Contains("Pointer") || name.Contains("Tapped") || name.Contains("Pressed");

        private static Control CreateErrorFallback(string message)
        {
            return new Border
            {
                Background = Avalonia.Media.Brushes.Black,
                Child = new TextBlock
                {
                    Text = $"⚠️ خطأ في المعاينة: {message}",
                    Foreground = Avalonia.Media.Brushes.Red,
                    TextWrapping = Avalonia.Media.TextWrapping.Wrap,
                    Margin = new Avalonia.Thickness(10)
                }
            };
        }
    }
}
