using Avalonia.Markup.Xaml;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace VisualEditorApp.Core
{
    public static class ModifiedXamlLoader
    {
        /// <summary>
        /// تحميل XAML من نص مباشر مع دعم تحديد الـ BaseUri لضمان تحميل الصور والمصادر.
        /// </summary>
        public static T LoadFromXaml<T>(string xaml, Uri baseUri = null) where T : class
        {
            // 1. تحويل النص إلى Stream لضمان التوافق العالي
            byte[] byteArray = Encoding.UTF8.GetBytes(xaml);
            using MemoryStream stream = new MemoryStream(byteArray);

            // 2. استخدام المحمل الأصلي مع إعدادات مخصصة
            // ملاحظة: baseUri ضروري إذا كان الـ XAML يحتوي على مسارات مثل "avares://..."
            return (T)AvaloniaRuntimeXamlLoader.Load(stream, null, null, baseUri);
        }

        /// <summary>
        /// نسخة تسمح بتحميل XAML وتعديل محتواه ديناميكياً قبل العرض (مثل نظام القوالب)
        /// </summary>
        public static T LoadWithReplacements<T>(string xaml, Dictionary<string, string> replacements) where T : class
        {
            foreach (var item in replacements)
            {
                xaml = xaml.Replace($"{{{{{item.Key}}}}}", item.Value);
            }
            return LoadFromXaml<T>(xaml);
        }
    }
}
