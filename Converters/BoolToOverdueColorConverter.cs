using System.Globalization;

namespace THWTicketApp.Converters;

public class BoolToOverdueColorConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool isActive && isActive)
            return Color.FromArgb("#E74C3C"); // Red when active
        return Color.FromArgb("#9E9E9E"); // Gray when inactive
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotImplementedException();
}
