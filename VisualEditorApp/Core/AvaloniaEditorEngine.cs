using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using System;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;

namespace VisualEditorApp.Core
{
    public static class AvaloniaEditorEngine
    {
        /// <summary>
        /// النسخة المعدلة لتحميل XAML في وضع "المحرر" بدون انهيارات.
        /// </summary>
        public static Control LoadPreview(string xamlText)
        {
            try
            {
                // 1. الفلترة المسبقة (Pre-processing)
                // حذف x:Class وأي مراجع لمشاريع خارجية تسبب FileNotFoundException
                xamlText = Regex.Replace(xamlText, @"x:Class=""[^""]*""", "");
                xamlText = Regex.Replace(xamlText, @"avares://(?!YourAppName)[^""'\s<>]*", "");

                // تحويل Window إلى UserControl لضمان نجاح الـ Cast داخل المحرر
                xamlText = xamlText.Replace("<Window", "<UserControl").Replace("</Window>", "</UserControl>");

                // 2. إعداد المحمل المعدل (Custom Loader Configuration)
                using (var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(xamlText)))
                {
                    // استخدام وضع التصميم (DesignMode = true) هو السر في تخطي أخطاء الموارد المفقودة
                    return (Control)AvaloniaRuntimeXamlLoader.Load(new RuntimeXamlLoaderDocument(stream), new RuntimeXamlLoaderConfiguration
                    {
                        DesignMode = true,
                        LocalAssembly = Assembly.GetExecutingAssembly()
                    });
                }
            }
            catch (Exception ex)
            {
                // 3. عرض الخطأ بشكل مرئي بدلاً من إغلاق البرنامج
                return new TextBlock
                {
                    Text = $"⚠️ خطأ في المعالجة: {ex.Message}",
                    Foreground = Avalonia.Media.Brushes.Red,
                    TextWrapping = Avalonia.Media.TextWrapping.Wrap
                };
            }
        }
    }
}
