using Avalonia.Data.Converters;
using System.Globalization;


namespace VisualEditor.Toolbox.Prop;

public class PropertyTypeToVisibleConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is Type type && parameter is string target)
        {
            // ÝÍÕ ÏÞíÞ ááÃäæÇÚ
            bool isBool = type == typeof(bool);
            // áæ ÇáäæÚ åæ Color Ãæ ÈíÍÊæí Úáì ßáãÉ Brush (Òí SolidColorBrush)
            bool isColor = type.Name.Contains("Color") || type.Name.Contains("Brush") || typeof(Avalonia.Media.IBrush).IsAssignableFrom(type);

            switch (target.ToLower())
            {
                case "bool": return isBool;
                case "color": return isColor;
                case "text": return !isBool && !isColor;
            }
        }
        return false;
    }
    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => null;
}
