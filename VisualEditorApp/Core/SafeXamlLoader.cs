using Avalonia.Markup.Xaml;
using Avalonia.Markup.Xaml.XamlIl;
using Avalonia.Platform;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.NetworkInformation;
using System.Text;

namespace VisualEditorApp.Core
{
    public static class SafeXamlLoader
    {
        public static object LoadSafe(string xamlText, Uri baseUri = null)
        {
            try
            {
                // 1. تنظيف الـ XAML من المسارات المكسورة قبل التحميل (اختياري)
                // يمكنك استخدام Regex لإزالة الصور التي لا تبدأ بـ resm أو avares

                byte[] byteArray = Encoding.UTF8.GetBytes(xamlText);
                using (MemoryStream stream = new MemoryStream(byteArray))
                {
                    // استخدام المحمل الرسمي داخل بلوك Try-Catch
                    return AvaloniaRuntimeXamlLoader.Load(stream, null, null, baseUri);
                }
            }
            catch (Exception ex)
            {
                // 2. في حالة الخطأ، أرجع واجهة "خطأ" بدلاً من إغلاق البرنامج
                return CreateErrorView(ex.Message);
            }
        }

        private static object CreateErrorView(string message)
        {
            // بناء واجهة بسيطة تعرض الخطأ للمستخدم داخل المحرر
            string errorXaml = $@"
            <StackPanel xmlns='https://github.com' Spacing='10' Margin='20'>
                <TextBlock Text='⚠️ خطأ في تحميل الصفحة' Foreground='Red' FontWeight='Bold'/>
                <TextBlock Text='{message.Replace("'", "&apos;")}' TextWrapping='Wrap' FontSize='12'/>
            </StackPanel>";

            return AvaloniaRuntimeXamlLoader.Parse<object>(errorXaml);
        }
    }
}
