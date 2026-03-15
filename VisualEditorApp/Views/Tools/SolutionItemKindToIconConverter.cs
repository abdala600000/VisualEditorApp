using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;
using VisualEditorApp.Models;

namespace VisualEditorApp.Views.Tools
{
    public sealed class SolutionItemKindToIconConverter : IValueConverter
    {
        private static readonly Geometry s_solutionIcon = Geometry.Parse("M14.5,3H1.5V13H14.5V3 M13.5,12H2.5V4H13.5V12 M12,5H4V6H12V5 M12,7H4V8H12V7 M12,9H4V10H12V9 M12,11H4V12H12V11");
        private static readonly Geometry s_projectIcon = Geometry.Parse("M15,1V15H1V1H15 M14,14V2H2V14H14 M4,4V5H9V4H4 M4,6V7H12V6H4 M4,8V9H12V8H4 M4,10V11H12V10H4 M10,4V5H12V4H10");
        private static readonly Geometry s_folderIcon = Geometry.Parse("M14,4.5H8L6,2.5H2C1.2,2.5 0.5,3.2 0.5,4V12C0.5,12.8 1.2,13.5 2,13.5H14C14.8,13.5 15.5,12.8 15.5,12V6C15.5,5.2 14.8,4.5 14,4.5Z M14.5,12.5H1.5V6H14.5V12.5Z");
        private static readonly Geometry s_documentIcon = Geometry.Parse("M11.5,1H3.5C2.7,1 2,1.7 2,2.5V13.5C2,14.3 2.7,15 3.5,15H12.5C13.3,15 14,14.3 14,13.5V4.5L10.5,1H11.5 Z M10,2V5H13L10,2 Z M13,14H3V2H9V6H13V14 Z");
        private static readonly Geometry s_dependencyIcon = Geometry.Parse("M14.5,1.5L9.5,6.5L14.5,11.5L13,13L8,8L3,13L1.5,11.5L6.5,6.5L1.5,1.5L3,0L8,5L13,0L14.5,1.5 Z"); // Placeholder for Project Ref
        private static readonly Geometry s_axamlIcon = Geometry.Parse("M8,1L15,14H1L8,1 Z M8,4.5L12,11.5H4L8,4.5 Z M8,7.5L10,11H6L8,7.5 Z"); // Enhanced Avalonia Shield
        private static readonly Geometry s_csharpIcon = Geometry.Parse("M14,2H2V14H14V2 M13,13H3V3H13V13 M6,5V11H4V5H6 M10,5V11H8V5H10 M5,7V9H9V7H5"); // C# Boxy Icon
        private static readonly Geometry s_runIcon = Geometry.Parse("M4,2V14L14,8L4,2 Z"); 
        private static readonly Geometry s_nugetIcon = Geometry.Parse("M8,1.5L1.5,4.5V11.5L8,14.5L14.5,11.5V4.5L8,1.5 Z M8,3.5L12.5,5.5L8,7.5L3.5,5.5L8,3.5 Z M2.5,6V10.5L7.5,12.5V8L2.5,6 Z M13.5,6L8.5,8V12.5L13.5,10.5V6 Z"); // 3D Box for NuGet

        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            SolutionItemKind kind = SolutionItemKind.Document;
            string? name = null;
            bool isStartup = false;

            if (value is SolutionItemViewModel item)
            {
                kind = item.Kind;
                name = item.Name;
                isStartup = item.IsStartupProject;
            }
            else if (value is SolutionItemKind k)
            {
                kind = k;
            }

            // 🎯 أيقونة التشغيل للمشروع المختار
            if (isStartup && kind == SolutionItemKind.Project)
                return s_runIcon;

            // 🎯 أيقونات ملفات مخصصة
            if (kind == SolutionItemKind.Document && name != null)
            {
                if (name.EndsWith(".axaml", StringComparison.OrdinalIgnoreCase))
                    return s_axamlIcon;
                
                if (name.EndsWith(".cs", StringComparison.OrdinalIgnoreCase))
                    return s_csharpIcon;
            }

            return kind switch
            {
                SolutionItemKind.Solution => s_solutionIcon,
                SolutionItemKind.Project => s_projectIcon,
                SolutionItemKind.Folder => s_folderIcon,
                SolutionItemKind.Document => s_documentIcon,
                SolutionItemKind.Dependency => s_dependencyIcon,
                SolutionItemKind.NuGet => s_nugetIcon,
                _ => s_documentIcon
            };
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
