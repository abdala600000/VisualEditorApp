using System.Text.RegularExpressions;

namespace VisualEditor.Core
{
    public static class XamlSanitizer
    {
        public static string Sanitize(string originalXaml)
        {
            if (string.IsNullOrWhiteSpace(originalXaml)) return originalXaml;

            string clean = originalXaml;

            // 1. مسح الـ x:Class
            clean = Regex.Replace(clean, @"\s+x:Class=""[^""]*""", "");

            // 2. تحويل CompiledBinding
            clean = Regex.Replace(clean, @"\{CompiledBinding\b", "{Binding");

            // 3. مسح الأحداث (Events)
            clean = Regex.Replace(clean, @"\s+[A-Za-z]*(?:Click|Pressed|Released|Enter|Leave|Move|Wheel|Down|Up|Changed|Loaded|Unloaded|Opened|Closed|Tapped|TextInput|Focus|Checked|Unchecked)=""[^""]*""", "");

            // 4. حماية الصور
            clean = Regex.Replace(clean, @"\s+Source=""(?!(http|https)://)[^""]*""", "");
            clean = Regex.Replace(clean, @"<ImageBrush\s+ImageSource=""(?!(http|https)://)[^""]*""", "<ImageBrush ");

            return clean;
        }
    }
}