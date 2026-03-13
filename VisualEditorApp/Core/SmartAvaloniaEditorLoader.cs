using Avalonia.Markup.Xaml;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml;

namespace VisualEditorApp.Core
{
    public static class SmartAvaloniaEditorLoader
    {
        /// <summary>
        /// المهمة: تحميل الـ XAML وربطه برمجياً مع ضمان عدم الانهيار
        /// </summary>
        /// <param name="target">الكائن (مثل Window أو UserControl) المراد ربطه</param>
        /// <param name="xamlText">نص الـ XAML من المحرر</param>
        public static bool TryLoadAndBind(object target, string xamlText)
        {
            try
            {
                // 1. التطهير الذكي (Sanitization)
                string safeXaml = SanitizeForEditor(xamlText);

                // 2. إعداد المحمل (Runtime Loader)
                using var stream = new MemoryStream(Encoding.UTF8.GetBytes(safeXaml));

                // الربط المباشر مع الـ Code-behind (target)
                // هذا يسمح لـ x:Name و الـ Events بالعمل إذا كانت معرفة في ملف الـ C#
                AvaloniaRuntimeXamlLoader.Load(stream, null, target);

                return true;
            }
            catch (Exception ex)
            {
                // سجل الخطأ هنا ليعرف المستخدم أين المشكلة في الكود
                System.Diagnostics.Debug.WriteLine($"Editor Error: {ex.Message}");
                return false;
            }
        }

        private static string SanitizeForEditor(string xaml)
        {
            try
            {
                var doc = new XmlDocument();
                doc.LoadXml(xaml);
                var nsmgr = new XmlNamespaceManager(doc.NameTable);
                nsmgr.AddNamespace("x", "http://schemas.microsoft.com");

                // أ- إزالة x:Class مؤقتاً لتجنب تعارضات الأنواع غير المجمعة (Uncompiled Types)
                if (doc.DocumentElement.HasAttribute("x:Class"))
                    doc.DocumentElement.RemoveAttribute("x:Class");

                // ب- معالجة العناصر الحساسة (الصور والموارد)
                var elements = doc.SelectNodes("//*");
                foreach (XmlElement el in elements)
                {
                    // 1. إصلاح مسارات الصور المكسورة
                    if (el.LocalName == "Image" || el.LocalName == "ImageBrush")
                    {
                        if (el.HasAttribute("Source"))
                        {
                            string src = el.GetAttribute("Source");
                            if (!IsResourceValid(src))
                            {
                                // استبدال بمسار وهمي أو إزالته لمنع الـ Crash
                                el.SetAttribute("Source", "avares://YourProject/Assets/placeholder.png");
                            }
                        }
                    }

                    // 2. منع الـ Events المكسورة (إذا كان المستخدم يكتب اسم Function غير موجودة)
                    // في وضع المحرر، يفضل أحياناً مسح الـ Click handlers أثناء السحب والإفلات
                }

                return doc.OuterXml;
            }
            catch { return xaml; } // إذا فشل الـ XML، نترك المحمل الأصلي يخرج الخطأ
        }

        private static bool IsResourceValid(string path)
        {
            if (string.IsNullOrEmpty(path)) return false;
            if (path.StartsWith("avares://")) return true;
            if (Path.IsPathRooted(path)) return File.Exists(path);
            return true;
        }
    }
}
