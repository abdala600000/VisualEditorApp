using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using System;
using System.IO;
using System.Reflection;
using System.Text;

namespace VisualEditorApp.Core
{
    public static class DesignerLoader
    {
        public static Control LoadLikeOfficialDesigner(string xamlText)
        {
            try
            {
                // 1. إخبار النواة أننا في وضع التصميم (Design Mode)
                

                var stream = new MemoryStream(Encoding.UTF8.GetBytes(xamlText));
                var localAsm = Assembly.GetExecutingAssembly();

                // 2. إنشاء مسار وهمي لمنع أخطاء الـ ResourceInclude
                Uri baseUri = new Uri("avares://VisualEditorApp/DesignerPreview.axaml");

                // 3. إعدادات المحرك المتسامحة (Designer Settings)
                var config = new RuntimeXamlLoaderConfiguration
                {
                    LocalAssembly = localAsm,
                    DesignMode = true,
                    UseCompiledBindingsByDefault = false // 👈 تمنع الكراش الناتج عن الـ Bindings غير المكتملة
                };

                var document = new RuntimeXamlLoaderDocument(baseUri, null, stream);

                // 4. استخدام الـ Reflection لاختراق المحرك الداخلي (AvaloniaXamlIlRuntimeCompiler)
                var compilerType = typeof(RuntimeXamlLoaderDocument).Assembly.GetType("Avalonia.Markup.Xaml.XamlIl.AvaloniaXamlIlRuntimeCompiler");
                if (compilerType == null)
                    throw new Exception("لم يتم العثور على محرك Avalonia الداخلي. تأكد من إصدار المكتبة.");

                var loadMethod = compilerType.GetMethod("Load", BindingFlags.Public | BindingFlags.Static);
                if (loadMethod == null)
                    throw new Exception("لم يتم العثور على دالة Load.");

                // 5. تنفيذ التحميل
                var loadedObject = loadMethod.Invoke(null, new object[] { document, config });

                // 6. الخدعة السحرية: إذا كان المحمل عبارة عن "ستايلات فقط" وليس نافذة
                if (loadedObject is Avalonia.Styling.Styles || loadedObject is Avalonia.Styling.Style)
                {
                    // نضعه في عنصر تحكم وهمي لكي يظهر في المصمم
                    return new Border
                    {
                        Background = Brushes.LightGray,
                        Child = new TextBlock
                        {
                            Text = "تم تحميل الستايل بنجاح (لا يوجد واجهة مرئية لعرضها)",
                            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                            VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center
                        }
                    };
                }

                return loadedObject as Control ?? new TextBlock { Text = "تم التحميل ولكن الكائن ليس Control" };
            }
            catch (Exception ex)
            {
                // استخراج الخطأ الحقيقي من داخل الـ Reflection
                var realError = ex.InnerException?.Message ?? ex.Message;

                // واجهة بديلة (Fallback UI) تظهر الخطأ باللون الأحمر بدلاً من إغلاق البرنامج
                return new TextBlock
                {
                    Text = $"⚠️ خطأ في تصميم الـ XAML:\n\n{realError}",
                    Foreground = Brushes.Red,
                    FontWeight = FontWeight.Bold,
                    Margin = new Thickness(10),
                    TextWrapping = TextWrapping.Wrap
                };
            }
        }
    }
}
