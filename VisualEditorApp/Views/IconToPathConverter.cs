using Avalonia.Data.Converters;
using Avalonia.Media;
using System;

namespace VisualEditorApp;

public class IconToPathConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, System.Globalization.CultureInfo culture)
    {
        string ext = value?.ToString()?.ToLower() ?? "";

        // ãÓÇÑÇÊ ÇáÃíÞæäÇÊ (SVG Paths)
        return ext switch
        {
            "project" => StreamGeometry.Parse("M12,2L4,5V11C4,16.55 7.84,21.74 13,23C18.16,21.74 22,16.55 22,11V5L14,2"),
            "folder" => StreamGeometry.Parse("M10,4H4C2.89,4 2,4.89 2,6V18A2,2 0 0,0 4,20H20A2,2 0 0,0 22,18V8C22,6.89 21.1,6 20,6H12L10,4Z"),
            ".cs" => StreamGeometry.Parse("M12,2L4,5V11C4,16.55 7.84,21.74 13,23C18.16,21.74 22,16.55 22,11V5L14,2"), // ÃíÞæäÉ C#
            ".axaml" or ".xaml" => StreamGeometry.Parse("M14,12L10,8V11H2V13H10V16L14,12M22,12L18,8V11H15V13H18V16L22,12Z"), // ÃíÞæäÉ Avalonia
            ".json" => StreamGeometry.Parse("M5,3H19A2,2 0 0,1 21,5V19A2,2 0 0,1 19,21H5A2,2 0 0,1 3,19V5A2,2 0 0,1 5,3M15,15V17H17V15H15M11,15V17H13V15H11M7,15V17H9V15H7Z"),
            _ => StreamGeometry.Parse("M13,9V3.5L18.5,9M6,2C4.89,2 4,2.89 4,4V20A2,2 0 0,0 6,22H18A2,2 0 0,0 20,20V8L14,2H6Z"),
        };
    }
    public object ConvertBack(object? value, Type targetType, object? parameter, System.Globalization.CultureInfo culture) => null;
}
