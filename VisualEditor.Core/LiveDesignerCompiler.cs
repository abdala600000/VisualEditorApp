using Avalonia.Controls;
using Avalonia.Media;
using System.Text.RegularExpressions;

namespace VisualEditor.Core
{
    public static class LiveDesignerCompiler
    {
        public static Control RenderLiveXaml(string xamlText)
        {


            try
            {
                // ========================================================
                // 💡 التريكة الذهبية: تنظيف الـ XAML من الكلاسات والأحداث
                // ========================================================

                // 1. مسح الكلاس المرتبط (Code-Behind)
                xamlText = Regex.Replace(xamlText, @"x:Class=""[^""]*""", "");

                // 2. مسح أحداث الضغط (Events)
                xamlText = Regex.Replace(xamlText, @"\b(Click|Tapped|SelectionChanged|TextChanged)=""[^""\{]*""", "");

                // 3. 👈 السطر الجديد (الصور): مسح مسارات الصور الخارجية لمنع الكراش
                xamlText = Regex.Replace(xamlText, @"Source=""avares://[^""]*""", "");

                // 4. 👈 السطر الجديد (الخطوط): استبدال الخطوط الخارجية بخط افتراضي آمن
                xamlText = Regex.Replace(xamlText, @"avares://[^<""]*", "Arial");

                // (اختياري) تحويل Window إلى UserControl
                xamlText = xamlText.Replace("<Window", "<UserControl").Replace("</Window>", "</UserControl>");
                // لتشاهد اسم دالة الـ Load في الإصدار الذي قمت بتثبيته (قد تكون RuntimeLoader أو XamlLoader)
                var loadedObject = XamlToCSharpGenerator.Runtime.AvaloniaSourceGeneratedXamlLoader.Load(xamlText, designMode: true);

                if (loadedObject is Control control)
                {
                    return control;
                }

                // إذا كان المحمل عبارة عن ستايل (Style) وليس واجهة مرئية
                return new Border
                {
                    Background = Brushes.LightGray,
                    Child = new TextBlock { Text = "تم تحميل الستايل بنجاح (AXSG Engine)" }
                };
            }
            catch (Exception ex)
            {
                // 3. Fallback UI لعرض الأخطاء في المصمم
                return new Border
                {
                    BorderBrush = Brushes.Red,
                    BorderThickness = new Avalonia.Thickness(2),
                    Background = Brushes.LightYellow,
                    Padding = new Avalonia.Thickness(10),
                    Child = new TextBlock
                    {
                        Text = $"⚠️ خطأ في محرك AXSG:\n{ex.Message}",
                        Foreground = Brushes.DarkRed,
                        FontWeight = FontWeight.Bold,
                        TextWrapping = TextWrapping.Wrap
                    }
                };
            }
        }
    }
}