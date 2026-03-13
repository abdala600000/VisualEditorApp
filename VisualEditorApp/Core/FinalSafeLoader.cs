using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using System;
using System.IO;
using System.Reflection;

namespace VisualEditorApp.Core
{
    public static class FinalSafeLoader
    {
        public static Control LoadFromText(string xamlText)
        {
            try
            {
                // 1. معالجة النصوص المكسورة أولاً (Regex) لمنع الانهيار المبكر
                xamlText = System.Text.RegularExpressions.Regex.Replace(xamlText, "x:Class=\"[^\"]*\"", "");
                xamlText = System.Text.RegularExpressions.Regex.Replace(xamlText, "avares://[^\"'\\s<>]*", "");

                // 2. استخدام المحمل مع Assembly فارغة لمنع البحث عن كلاسات مشروعك
                var assembly = Assembly.GetExecutingAssembly();

                // 3. التحميل باستخدام Parse<T> مع وضع التصميم (Design Mode)
                // هذا الخيار يخبر Avalonia بتجاهل أخطاء الـ Static Resources
                using (var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(xamlText)))
                {
                    return (Control)AvaloniaRuntimeXamlLoader.Load(stream, assembly, null, null, true);
                }
            }
            catch (Exception ex)
            {
                // في حالة الفشل، نقوم ببناء واجهة XML يدوية بسيطة جداً لضمان عدم توقف المحرر
                string fallback = "<UserControl xmlns='https://github.com/avaloniaui'><TextBlock Text='خطأ في عرض الصفحة'/></UserControl>";
                return AvaloniaRuntimeXamlLoader.Parse<UserControl>(fallback);
            }
        }
    }
}
