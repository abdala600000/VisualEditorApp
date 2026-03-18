using System;
using System.Text.RegularExpressions;
using Avalonia.Controls;

namespace VisualEditor.Core;

/// <summary>
/// Text-based XAML patcher - modifies XAML as plain text to preserve all formatting.
/// Never re-serializes the whole document; only patches the specific element/attribute.
/// </summary>
public static class XamlDOMPatcher
{
    // ─── PatchProperty ────────────────────────────────────────────────────────
    public static string PatchProperty(string xaml, Control target, string prop, string value)
    {
        if (target == null || string.IsNullOrWhiteSpace(xaml) || string.IsNullOrEmpty(target.Name))
            return xaml;

        var match = FindElementByName(xaml, target.Name);
        if (!match.Success) return xaml;

        string patchedTag = SetAttributeInTag(match.Value, prop, value);
        return xaml.Substring(0, match.Index) + patchedTag + xaml.Substring(match.Index + match.Length);
    }

    // ─── AddElement ───────────────────────────────────────────────────────────
    public static string AddElement(string xaml, string parentName, string elementType, string newName, string extraProps = "")
    {
        if (string.IsNullOrWhiteSpace(xaml)) return xaml;

        string nameAttr  = string.IsNullOrEmpty(newName)    ? "" : $" Name=\"{newName}\"";
        string propsAttr = string.IsNullOrEmpty(extraProps) ? "" : " " + extraProps;
        string newElement = $"<{elementType}{nameAttr}{propsAttr} />";

        if (string.IsNullOrEmpty(parentName))
            return InsertIntoRoot(xaml, newElement);
        else
            return InsertIntoNamedParent(xaml, parentName, newElement);
    }

    // ─── RemoveAttribute ─────────────────────────────────────────────────────
    /// <summary>يحذف attribute محدد من عنصر بالاسم</summary>
    public static string RemoveAttribute(string xaml, string elementName, string attrName)
    {
        if (string.IsNullOrEmpty(elementName) || string.IsNullOrWhiteSpace(xaml)) return xaml;
        var match = FindElementByName(xaml, elementName);
        if (!match.Success) return xaml;

        string tag = match.Value;
        string cleaned = Regex.Replace(tag, $@"\s*\b{Regex.Escape(attrName)}=""[^""]*""", "");
        if (cleaned == tag) return xaml;
        return xaml.Substring(0, match.Index) + cleaned + xaml.Substring(match.Index + match.Length);
    }

    // ─── RemoveElement ────────────────────────────────────────────────────────
    public static string RemoveElement(string xaml, string name)
    {
        if (string.IsNullOrEmpty(name) || string.IsNullOrWhiteSpace(xaml)) return xaml;

        string escaped = Regex.Escape(name);

        // self-closing: <Tag ... Name="x" ... />
        string selfClosing = $@"[ \t]*<\w[\w:.]*[^>]*(?:x:Name|Name)=""{escaped}""[^>]*/>\r?\n?";
        var result = Regex.Replace(xaml, selfClosing, "", RegexOptions.Singleline);
        if (result != xaml) return result;

        // opening+closing: <Tag ...>...</Tag>
        string openClose = $@"[ \t]*<(\w[\w:.]*)([^>]*(?:x:Name|Name)=""{escaped}"")[^>]*>.*?</\1>\r?\n?";
        return Regex.Replace(xaml, openClose, "", RegexOptions.Singleline);
    }

    // ─── PatchRenderTransform ─────────────────────────────────────────────────
    public static string PatchRenderTransform(string xaml, Control target, double angle, double skewX, double skewY)
    {
        if (target == null || string.IsNullOrWhiteSpace(xaml) || string.IsNullOrEmpty(target.Name))
            return xaml;

        // إذا كانت كل القيم صفر، احذف RenderTransform بالكامل
        if (angle == 0 && skewX == 0 && skewY == 0)
            return RemoveRenderTransformElement(xaml, target.Name);

        // بناء محتوى RenderTransform
        string transformContent;
        if (skewX == 0 && skewY == 0)
            transformContent = $"<RotateTransform Angle=\"{angle}\" />";
        else if (angle == 0)
            transformContent = $"<SkewTransform AngleX=\"{skewX}\" AngleY=\"{skewY}\" />";
        else
            transformContent = $"<TransformGroup><SkewTransform AngleX=\"{skewX}\" AngleY=\"{skewY}\" /><RotateTransform Angle=\"{angle}\" /></TransformGroup>";

        // إيجاد العنصر
        var match = FindElementByName(xaml, target.Name);
        if (!match.Success) return xaml;

        // استخراج اسم النوع من الـ tag
        var typeMatch = Regex.Match(match.Value, @"^<(\w[\w:]*)");
        if (!typeMatch.Success) return xaml;
        string typeName = typeMatch.Groups[1].Value;

        string propElementOpen  = $"<{typeName}.RenderTransform>";
        string propElementClose = $"</{typeName}.RenderTransform>";

        // هل يوجد property element موجود بالفعل؟
        int searchFrom = match.Index + match.Length;
        // نبحث بعد الـ opening tag مباشرة
        // لكن أولاً نتأكد أن الـ tag ليس self-closing
        bool isSelfClosing = match.Value.TrimEnd().EndsWith("/>");

        if (isSelfClosing)
        {
            // نحوّل self-closing إلى open/close ونضيف property element
            string openTag = Regex.Replace(match.Value, @"\s*/>(\s*)$", ">");
            string indent = GetIndentOf(xaml, match.Index) + "    ";
            string closeTag = $"\n</{typeName}>";
            string newBlock = openTag + $"\n{indent}{propElementOpen}{transformContent}{propElementClose}" + closeTag;
            return xaml.Substring(0, match.Index) + newBlock + xaml.Substring(match.Index + match.Length);
        }
        else
        {
            // ابحث عن property element موجود واستبدله
            string existingPropPattern = Regex.Escape(propElementOpen) + @".*?" + Regex.Escape(propElementClose);
            string afterTag = xaml.Substring(searchFrom);
            var existingMatch = Regex.Match(afterTag, existingPropPattern, RegexOptions.Singleline);
            if (existingMatch.Success)
            {
                string replacement = propElementOpen + transformContent + propElementClose;
                return xaml.Substring(0, searchFrom) + afterTag.Substring(0, existingMatch.Index)
                    + replacement + afterTag.Substring(existingMatch.Index + existingMatch.Length);
            }
            else
            {
                // أضف property element جديد بعد الـ opening tag مباشرة
                string indent = GetIndentOf(xaml, match.Index) + "    ";
                string insertion = $"\n{indent}{propElementOpen}{transformContent}{propElementClose}";
                return xaml.Substring(0, searchFrom) + insertion + xaml.Substring(searchFrom);
            }
        }
    }

    private static string RemoveRenderTransformElement(string xaml, string elementName)
    {
        var match = FindElementByName(xaml, elementName);
        if (!match.Success) return xaml;

        var typeMatch = Regex.Match(match.Value, @"^<(\w[\w:]*)");
        if (!typeMatch.Success) return xaml;
        string typeName = typeMatch.Groups[1].Value;

        string propElementOpen  = Regex.Escape($"<{typeName}.RenderTransform>");
        string propElementClose = Regex.Escape($"</{typeName}.RenderTransform>");
        string pattern = $@"\s*{propElementOpen}.*?{propElementClose}";

        int searchFrom = match.Index + match.Length;
        string afterTag = xaml.Substring(searchFrom);
        string cleaned = Regex.Replace(afterTag, pattern, "", RegexOptions.Singleline);
        return xaml.Substring(0, searchFrom) + cleaned;
    }

    // ─── Private Helpers ──────────────────────────────────────────────────────

    /// <summary>
    /// يجد أول tag (self-closing أو opening) يحتوي على Name="name" أو x:Name="name"
    /// مع تجاهل التعليقات والـ Design.DataContext
    /// </summary>
    private static Match FindElementByName(string xaml, string name)
    {
        string escaped = Regex.Escape(name);
        // يطابق self-closing أو opening tag يحتوي على الاسم
        string pattern = $@"<\w[\w:.]*\s[^>]*(?:x:Name|Name)=""{escaped}""[^>]*(?:/>|>)";
        return Regex.Match(xaml, pattern, RegexOptions.Singleline);
    }

    /// <summary>
    /// يضيف أو يعدل attribute داخل نص tag
    /// </summary>
    private static string SetAttributeInTag(string tag, string attrName, string attrValue)
    {
        // هل الـ attribute موجود؟
        string attrPattern = $@"\b{Regex.Escape(attrName)}=""[^""]*""";
        if (Regex.IsMatch(tag, attrPattern))
            return Regex.Replace(tag, attrPattern, $"{attrName}=\"{attrValue}\"");

        // أضفه قبل /> أو >
        if (Regex.IsMatch(tag, @"/>\s*$"))
            return Regex.Replace(tag, @"\s*/>(\s*)$", $" {attrName}=\"{attrValue}\" />$1");
        else
            return Regex.Replace(tag, @">\s*$", $" {attrName}=\"{attrValue}\">");
    }

    /// <summary>
    /// يدرج العنصر الجديد مباشرة قبل آخر closing tag في الملف (الجذر)
    /// مع تجاهل Design.DataContext وأي property elements
    /// </summary>
    private static string InsertIntoRoot(string xaml, string newElement)
    {
        // نجد الـ root tag name من أول سطر
        var rootTagMatch = Regex.Match(xaml, @"<(\w[\w:.]*)[\s>]");
        if (!rootTagMatch.Success) return xaml;
        string rootTag = rootTagMatch.Groups[1].Value;

        // نجد closing tag الجذر: </Window> أو </UserControl> إلخ
        string closeTag = $"</{rootTag}>";
        int closeIndex = xaml.LastIndexOf(closeTag, StringComparison.Ordinal);
        if (closeIndex < 0) return xaml;

        // indentation: نفس indent الجذر + 4 مسافات
        string rootIndent = GetIndentOf(xaml, rootTagMatch.Index);
        string childIndent = rootIndent + "    ";

        string insertion = childIndent + newElement + "\n";
        return xaml.Substring(0, closeIndex) + insertion + xaml.Substring(closeIndex);
    }

    /// <summary>
    /// يدرج العنصر الجديد داخل parent محدد بالاسم
    /// </summary>
    private static string InsertIntoNamedParent(string xaml, string parentName, string newElement)
    {
        string escaped = Regex.Escape(parentName);

        // نجد opening tag للـ parent
        string parentPattern = $@"<(\w[\w:.]*)([^>]*(?:x:Name|Name)=""{escaped}"")[^>]*>";
        var parentMatch = Regex.Match(xaml, parentPattern, RegexOptions.Singleline);
        if (!parentMatch.Success) return xaml;

        string tagName = parentMatch.Groups[1].Value;
        int searchFrom = parentMatch.Index + parentMatch.Length;

        // نجد closing tag المقابل
        string closeTag = $"</{tagName}>";
        int closeIndex = xaml.IndexOf(closeTag, searchFrom, StringComparison.Ordinal);
        if (closeIndex < 0) return xaml;

        string indent = GetIndentOf(xaml, parentMatch.Index) + "    ";
        string insertion = "\n" + indent + newElement;

        return xaml.Substring(0, closeIndex)
             + insertion + "\n"
             + GetIndentOf(xaml, closeIndex)
             + xaml.Substring(closeIndex);
    }

    /// <summary>
    /// يرجع الـ whitespace في بداية السطر الذي يحتوي على position
    /// </summary>
    private static string GetIndentOf(string text, int position)
    {
        int lineStart = text.LastIndexOf('\n', Math.Max(0, position - 1));
        lineStart = lineStart < 0 ? 0 : lineStart + 1;
        int i = lineStart;
        while (i < text.Length && (text[i] == ' ' || text[i] == '\t')) i++;
        return text.Substring(lineStart, i - lineStart);
    }
}
