using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;
using VisualEditorApp.Models;

namespace VisualEditorApp.Views.Tools
{
    public sealed class SolutionItemToForegroundConverter : IValueConverter
    {
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

            if (isStartup && kind == SolutionItemKind.Project)
                return Brush.Parse("#4CAF50"); // Green for Startup

            if (kind == SolutionItemKind.Document && name != null)
            {
                if (name.EndsWith(".cs", StringComparison.OrdinalIgnoreCase))
                    return Brush.Parse("#9B4F96"); // Purple for C#
                
                if (name.EndsWith(".axaml", StringComparison.OrdinalIgnoreCase) || name.EndsWith(".xaml", StringComparison.OrdinalIgnoreCase))
                    return Brush.Parse("#3E78B3"); // Blue for XAML/AXAML
            }

            return kind switch
            {
                SolutionItemKind.Solution => Brush.Parse("#995ED9"), // Purple
                SolutionItemKind.Project => Brush.Parse("#2270CF"),  // Blue
                SolutionItemKind.Folder => Brush.Parse("#F0C94B"),   // Yellow/Folder
                SolutionItemKind.Dependency => Brush.Parse("#2270CF"), // Blue for Project ref
                SolutionItemKind.NuGet => Brush.Parse("#007ACC"),      // Blue-ish/NuGet
                _ => Brush.Parse("#DCDCDC") // Default Muted
            };
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
