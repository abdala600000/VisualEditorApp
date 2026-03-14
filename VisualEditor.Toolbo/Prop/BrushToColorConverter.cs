using Avalonia.Data.Converters;
using Avalonia.Media;
using System.Globalization;


namespace VisualEditor.Toolbox.Prop;

public class BrushToColorConverter : IValueConverter
{
    // ⁄„· ‰”Œ… Static ⁄‘«‰ ‰Ê’· ·Â« »”ÂÊ·… „‰ «·‹ XAML
    public static readonly BrushToColorConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        // ·Ê «·ﬁÌ„… Ã«Ì… Brush (“Ì Background) »‰ÕÊ·Â« ·‹ Color ⁄‘«‰ «·‹ ColorPicker Ì›Â„Â«
        if (value is ISolidColorBrush solidBrush)
            return solidBrush.Color;

        if (value is Color color)
            return color;

        return value;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        // ·„« «·„” Œœ„ ÌŒ «— ·Ê‰ „‰ «·‹ ColorPicker »‰—Ã⁄Â ﬂ‹ SolidColorBrush ··ﬂ‰ —Ê·
        if (value is Color color)
            return new SolidColorBrush(color);

        return null;
    }
}
