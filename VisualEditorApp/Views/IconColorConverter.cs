using Avalonia.Data.Converters;
using Avalonia.Media;
using System;

namespace VisualEditorApp;

public class IconColorConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, System.Globalization.CultureInfo culture)
    {
        string ext = value?.ToString()?.ToLower() ?? "";
        return ext switch
        {
            "project" => Brushes.RoyalBlue,
            "folder" => Brushes.Goldenrod,
            ".cs" => Brushes.MediumPurple,
            ".axaml" or ".xaml" => Brushes.DeepSkyBlue,
            ".json" => Brushes.Orange,
            _ => Brushes.Gray
        };
    }
    public object ConvertBack(object? value, Type targetType, object? parameter, System.Globalization.CultureInfo culture) => null;
}
