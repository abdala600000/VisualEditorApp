using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Markup.Xaml;
using Avalonia.Markup.Xaml.XamlIl;
using Avalonia.Media;
using Avalonia.Platform;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.NetworkInformation;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;

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
